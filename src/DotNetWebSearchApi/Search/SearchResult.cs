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

    public string? ThumbnailUrl { get; init; }

    public string Category { get; init; } = "general";

    public DateTimeOffset? PublishedDate { get; init; }

    public ImageResultMetadata? ImageMetadata { get; init; }

    public VideoResultMetadata? VideoMetadata { get; init; }

    public MusicResultMetadata? MusicMetadata { get; init; }

    public PaperResultMetadata? PaperMetadata { get; init; }

    [JsonIgnore]
    public double? ProviderScore { get; init; }

    public IReadOnlyList<string> Engines { get; init; } = Array.Empty<string>();

    public IReadOnlyDictionary<string, int> Positions { get; init; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
}
