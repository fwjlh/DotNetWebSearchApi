namespace DotNetWebSearchApi.Search;

public sealed record SearchResult(
    string Title,
    string Url,
    string Snippet,
    string Provider,
    double Score);
