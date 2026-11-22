namespace DotNetWebSearchApi.Search;

public sealed record SearchExecutionResult(
    IReadOnlyList<SearchResult> Results,
    IReadOnlyList<string> UnresponsiveProviders,
    IReadOnlyList<string>? UnsupportedProviders = null);
