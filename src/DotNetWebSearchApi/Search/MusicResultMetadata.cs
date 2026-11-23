namespace DotNetWebSearchApi.Search;

public sealed record MusicResultMetadata
{
    public string AudioUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string Album { get; init; } = string.Empty;
    public double? DurationSeconds { get; init; }
    public string Country { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public string Codec { get; init; } = string.Empty;
    public int? Bitrate { get; init; }
}
