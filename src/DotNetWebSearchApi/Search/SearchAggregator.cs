using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DotNetWebSearchApi.Search;

public sealed class SearchAggregator
{
    private readonly IReadOnlyList<ISearchProvider> _providers;
    private readonly ILogger<SearchAggregator> _logger;
    private readonly SearchDiagnostics _diagnostics;
    private readonly ProviderHealthRegistry _health;

    public SearchAggregator(
        IEnumerable<ISearchProvider> providers,
        ILogger<SearchAggregator> logger,
        SearchDiagnostics? diagnostics = null,
        ProviderHealthRegistry? health = null)
    {
        _providers = providers.ToArray();
        _logger = logger;
        _diagnostics = diagnostics ?? new SearchDiagnostics();
        _health = health ?? new ProviderHealthRegistry();
    }

    public IReadOnlyList<string> ProviderNames => _providers.Select(provider => provider.Name).ToArray();

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        SearchExecutionResult execution = await SearchDetailedAsync(query, cancellationToken);
        return execution.Results;
    }

    public async Task<SearchExecutionResult> SearchDetailedAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Text) || query.Limit < 1)
            return new SearchExecutionResult(Array.Empty<SearchResult>(), Array.Empty<string>(), Array.Empty<string>());

        ISearchProvider[] selectedProviders = query.ProviderNames is null
            ? _providers.ToArray()
            : _providers.Where(provider => query.ProviderNames.Contains(provider.Name)).ToArray();
        ISearchProvider[] eligibleProviders = selectedProviders
            .Where(provider => GetCapabilities(provider).Supports(query))
            .ToArray();
        string[] unsupportedProviders = selectedProviders
            .Except(eligibleProviders)
            .Select(provider => provider.Name)
            .ToArray();
        List<ProviderOutcome> skippedOutcomes = new();
        List<ISearchProvider> readyProviders = new();
        foreach (ISearchProvider provider in eligibleProviders)
        {
            ProviderHealthAdmission admission = _health.TryAcquire(provider.Name);
            if (admission.CanExecute)
            {
                readyProviders.Add(provider);
                continue;
            }

            skippedOutcomes.Add(new ProviderOutcome(
                provider.Name,
                ProviderOutcomeStatus.Skipped,
                admission.Snapshot.LastFailureKind,
                admission.RetryAfter));
        }

        ProviderExecutionResult[] providerResults = await Task.WhenAll(
            readyProviders.Select(provider => ExecuteProviderAsync(provider, query, cancellationToken)));

        IReadOnlyList<SearchResult> results = MergeResults(providerResults.Select(item => item.Results), query.Limit);
        ProviderOutcome[] outcomes = skippedOutcomes
            .Concat(providerResults.Select(item => item.Outcome))
            .ToArray();
        string[] unresponsiveProviders = outcomes
            .Where(item => item.Status == ProviderOutcomeStatus.Failed)
            .Select(item => item.ProviderName)
            .ToArray();
        string[] coolingDownProviders = outcomes
            .Where(item => item.Status == ProviderOutcomeStatus.Skipped)
            .Select(item => item.ProviderName)
            .ToArray();
        return new SearchExecutionResult(results, unresponsiveProviders, unsupportedProviders)
        {
            ProviderOutcomes = outcomes,
            CoolingDownProviders = coolingDownProviders
        };
    }

    private static EngineCapabilities GetCapabilities(ISearchProvider provider)
    {
        return provider is ISearchProviderCapabilities declared
            ? declared.Capabilities
            : EngineCapabilities.BasicWeb;
    }

    private async Task<ProviderExecutionResult> ExecuteProviderAsync(
        ISearchProvider provider,
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(provider.Timeout);
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            IReadOnlyList<SearchResult> results = await provider.SearchAsync(query, timeoutSource.Token);
            _health.RecordSuccess(provider.Name);
            _diagnostics.RecordSuccess(provider.Name, stopwatch.Elapsed, results.Count);
            ProviderOutcome outcome = new(provider.Name, ProviderOutcomeStatus.Succeeded);
            return new ProviderExecutionResult(results, outcome);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _health.ReleaseProbe(provider.Name);
            throw;
        }
        catch (Exception exception)
        {
            ProviderOutcome outcome = ProviderFailureClassifier.CreateFailure(
                provider.Name,
                exception,
                timeoutSource.IsCancellationRequested);
            _health.RecordFailure(
                provider.Name,
                outcome.FailureKind ?? ProviderFailureKind.Unknown,
                outcome.RetryAfter);
            _logger.LogWarning(
                exception,
                "Search provider {Provider} failed with {FailureKind}.",
                provider.Name,
                outcome.FailureKind);
            _diagnostics.RecordFailure(provider.Name, stopwatch.Elapsed, exception.GetType().Name);
            return new ProviderExecutionResult(Array.Empty<SearchResult>(), outcome);
        }
    }

    private static IReadOnlyList<SearchResult> MergeResults(IEnumerable<IReadOnlyList<SearchResult>> providerResults, int limit)
    {
        Dictionary<string, SearchResult> uniqueResults = new(StringComparer.OrdinalIgnoreCase);
        foreach (SearchResult result in providerResults.SelectMany(results => results))
        {
            if (!TryGetCanonicalUrl(result.Url, out string canonicalUrl)) continue;
            if (!uniqueResults.TryGetValue(canonicalUrl, out SearchResult? existing) || result.Score > existing.Score)
                uniqueResults[canonicalUrl] = result;
        }

        return uniqueResults.Values
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Title, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();
    }

    private static bool TryGetCanonicalUrl(string value, out string canonicalUrl)
    {
        canonicalUrl = string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return false;
        UriBuilder builder = new(uri) { Fragment = string.Empty };
        string path = builder.Path.TrimEnd('/');
        builder.Path = path.Length == 0 ? "/" : path;
        canonicalUrl = builder.Uri.AbsoluteUri;
        return true;
    }

    private sealed record ProviderExecutionResult(IReadOnlyList<SearchResult> Results, ProviderOutcome Outcome);
}
