using Microsoft.Extensions.Logging;

namespace DotNetWebSearchApi.Search;

public sealed class SearchAggregator
{
    private readonly IReadOnlyList<ISearchProvider> _providers;
    private readonly ILogger<SearchAggregator> _logger;

    public SearchAggregator(
        IEnumerable<ISearchProvider> providers,
        ILogger<SearchAggregator> logger)
    {
        _providers = providers.ToArray();
        _logger = logger;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Text) || query.Limit < 1)
        {
            return Array.Empty<SearchResult>();
        }

        Task<IReadOnlyList<SearchResult>>[] tasks = _providers
            .Select(provider => ExecuteProviderAsync(provider, query, cancellationToken))
            .ToArray();

        IReadOnlyList<SearchResult>[] providerResults = await Task.WhenAll(tasks);
        return MergeResults(providerResults, query.Limit);
    }

    private async Task<IReadOnlyList<SearchResult>> ExecuteProviderAsync(
        ISearchProvider provider,
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(provider.Timeout);

        try
        {
            return await provider.SearchAsync(query, timeoutSource.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Search provider {Provider} failed.", provider.Name);
            return Array.Empty<SearchResult>();
        }
    }

    private static IReadOnlyList<SearchResult> MergeResults(
        IEnumerable<IReadOnlyList<SearchResult>> providerResults,
        int limit)
    {
        Dictionary<string, SearchResult> uniqueResults = new(StringComparer.OrdinalIgnoreCase);

        foreach (SearchResult result in providerResults.SelectMany(results => results))
        {
            if (!TryGetCanonicalUrl(result.Url, out string canonicalUrl))
            {
                continue;
            }

            if (!uniqueResults.TryGetValue(canonicalUrl, out SearchResult? existing)
                || result.Score > existing.Score)
            {
                uniqueResults[canonicalUrl] = result;
            }
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
        {
            return false;
        }

        UriBuilder builder = new(uri)
        {
            Fragment = string.Empty
        };

        string path = builder.Path.TrimEnd('/');
        builder.Path = path.Length == 0 ? "/" : path;
        canonicalUrl = builder.Uri.AbsoluteUri;
        return true;
    }
}
