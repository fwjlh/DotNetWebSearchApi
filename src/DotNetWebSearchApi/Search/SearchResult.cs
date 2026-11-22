using System.Text.Json.Serialization;

namespace DotNetWebSearchApi.Search;

public sealed record SearchResult(
    string Title,
    string Url,
    string Snippet,
    string Provider,
    double Score)
{
    public int? Position { get; init; }

    [JsonIgnore]
    public double? ProviderScore { get; init; }

    public IReadOnlyList<string> Engines { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, int> Positions { get; init; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}
