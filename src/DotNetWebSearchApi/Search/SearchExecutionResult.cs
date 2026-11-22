namespace DotNetWebSearchApi.Search;

public sealed record SearchExecutionResult(
    IReadOnlyList<SearchResult> Results,
    IReadOnlyList<string> UnresponsiveProviders,
    IReadOnlyList<string> UnsupportedProviders)
{
    public IReadOnlyList<ProviderOutcome> ProviderOutcomes { get; init; } = Array.Empty<ProviderOutcome>();
}
