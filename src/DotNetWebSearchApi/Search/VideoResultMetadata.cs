namespace DotNetWebSearchApi.Search;

public sealed record VideoResultMetadata
{
    public string EmbedUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public double? DurationSeconds { get; init; }
    public string Author { get; init; } = string.Empty;
    public string Views { get; init; } = string.Empty;
}
