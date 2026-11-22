namespace DotNetWebSearchApi.Search;

public static class SearxngSearchResponseMapper
{
    public static SearxngSearchResponse Map(string query, SearchExecutionResult execution)
    {
        SearxngSearchResult[] results = execution.Results.Select(MapResult).ToArray();
        return new SearxngSearchResponse(
            query, results.Length, results, Array.Empty<string>(), Array.Empty<string>(),
            Array.Empty<object>(), Array.Empty<string>(), execution.UnresponsiveProviders);
    }

    private static SearxngSearchResult MapResult(SearchResult result)
    {
        Uri uri = new(result.Url);
        int position = result.Score > 0 ? Math.Max(1, (int)Math.Round(1.0 / result.Score)) : 1;
        Dictionary<string, int> positions = new(StringComparer.OrdinalIgnoreCase) { [result.Provider] = position };
        string[] parsedUrl =
        {
            uri.Scheme, uri.Authority, uri.AbsolutePath, string.Empty,
            uri.Query.TrimStart('?'), uri.Fragment.TrimStart('#')
        };
        return new SearxngSearchResult(
            result.Url, result.Title, result.Snippet, result.Provider, new[] { result.Provider },
            positions, result.Score, "general", "default.html", parsedUrl, null);
    }
}
