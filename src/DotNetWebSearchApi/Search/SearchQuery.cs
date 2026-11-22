namespace DotNetWebSearchApi.Search;

public sealed record SearchQuery(string Text, int Limit, IReadOnlySet<string>? ProviderNames = null)
{
    public IReadOnlySet<string>? Categories { get; init; }

    public string Language { get; init; } = "auto";

    public int SafeSearch { get; init; }

    public int Page { get; init; } = 1;

    public SearchTimeRange? TimeRange { get; init; }
}

public enum SearchTimeRange
{
    Day,
    Week,
    Month,
    Year
}
