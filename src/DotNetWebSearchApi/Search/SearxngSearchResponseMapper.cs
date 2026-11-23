namespace DotNetWebSearchApi.Search;

public static class SearxngSearchResponseMapper
{
    public static SearxngSearchResponse Map(string query, SearchExecutionResult execution)
    {
        SearxngSearchResult[] results = execution.Results.Select(MapResult).ToArray();
        return new SearxngSearchResponse(
            query, results.Length, results, Array.Empty<string>(), Array.Empty<string>(),
            Array.Empty<object>(), Array.Empty<string>(), execution.UnresponsiveProviders,
            execution.UnsupportedProviders, execution.CoolingDownProviders)
        {
            ProviderOutcomes = execution.ProviderOutcomes
        };
    }

    private static SearxngSearchResult MapResult(SearchResult result)
    {
        Uri uri = new(result.Url);
        string[] engines = result.Engines.Count > 0 ? result.Engines.ToArray() : new[] { result.Provider };
        string[] parsedUrl =
        {
            uri.Scheme, uri.Authority, uri.AbsolutePath, string.Empty,
            uri.Query.TrimStart('?'), uri.Fragment.TrimStart('#')
        };
        return new SearxngSearchResult(
            result.Url, result.Title, result.Snippet, result.Provider, engines,
            result.Positions, result.Score, result.Category, "default.html", parsedUrl, null)
        {
            Thumbnail = result.ThumbnailUrl
        };
    }
}
