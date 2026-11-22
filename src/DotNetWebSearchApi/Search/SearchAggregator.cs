using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace DotNetWebSearchApi.Search;

public sealed class SearchAggregator
{
    private readonly IReadOnlyList<ISearchProvider> _providers;
    private readonly ILogger<SearchAggregator> _logger;
    private readonly SearchDiagnostics _diagnostics;

    public SearchAggregator(
        IEnumerable<ISearchProvider> providers,
        ILogger<SearchAggregator> logger,
        SearchDiagnostics? diagnostics = null)
    {
        _providers = providers.ToArray();
        _logger = logger;
        _diagnostics = diagnostics ?? new SearchDiagnostics();
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
            return new SearchExecutionResult(Array.Empty<SearchResult>(), Array.Empty<string>());

        ISearchProvider[] selectedProviders = query.ProviderNames is null
            ? _providers.ToArray()
            : _providers.Where(provider => query.ProviderNames.Contains(provider.Name)).ToArray();
        ProviderExecutionResult[] providerResults = await Task.WhenAll(
            selectedProviders.Select(provider => ExecuteProviderAsync(provider, query, cancellationToken)));

        IReadOnlyList<SearchResult> results = MergeResults(providerResults.Select(item => item.Results), query.Limit);
        string[] unresponsiveProviders = providerResults
            .Where(item => item.Failed)
            .Select(item => item.ProviderName)
            .ToArray();
        return new SearchExecutionResult(results, unresponsiveProviders);
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
            _diagnostics.RecordSuccess(provider.Name, stopwatch.Elapsed, results.Count);
            return new ProviderExecutionResult(provider.Name, results, false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Search provider {Provider} failed.", provider.Name);
            _diagnostics.RecordFailure(provider.Name, stopwatch.Elapsed, exception.GetType().Name);
            return new ProviderExecutionResult(provider.Name, Array.Empty<SearchResult>(), true);
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

    private sealed record ProviderExecutionResult(string ProviderName, IReadOnlyList<SearchResult> Results, bool Failed);
}
