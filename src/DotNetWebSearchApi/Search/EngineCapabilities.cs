namespace DotNetWebSearchApi.Search;

public sealed record EngineCapabilities(
    IReadOnlySet<string> Categories,
    bool SupportsPagination,
    int MaximumPage,
    bool SupportsTimeRange,
    bool SupportsSafeSearch,
    bool SupportsLocale,
    double Weight = 1.0)
{
    public static EngineCapabilities BasicWeb { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general" },
        false,
        1,
        false,
        false,
        false);

    public bool Supports(SearchQuery query)
    {
        if (query.Page < 1 || query.Page > MaximumPage) return false;
        if (query.Page > 1 && !SupportsPagination) return false;
        if (query.Categories is not null && query.Categories.Any(category => !Categories.Contains(category))) return false;
        if (query.TimeRange is not null && !SupportsTimeRange) return false;
        if (query.SafeSearch > 0 && !SupportsSafeSearch) return false;
        if (!string.IsNullOrWhiteSpace(query.Language)
            && !query.Language.Equals("auto", StringComparison.OrdinalIgnoreCase)
            && !SupportsLocale) return false;
        return true;
    }
}

public interface ISearchProviderCapabilities
{
    EngineCapabilities Capabilities { get; }
}
