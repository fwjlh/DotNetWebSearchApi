using System.Text.Json.Serialization;

namespace DotNetWebSearchApi.Search;

public sealed record ImageFormatReference(
    string Url,
    string Subtype,
    string Label,
    [property: JsonPropertyName("mtype")] string MediaType = "image");

public sealed record ImageResultMetadata
{
    public string ImageUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string Resolution { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public long? FileSizeBytes { get; init; }
    public string FileSize { get; init; } = string.Empty;
    public string License { get; init; } = string.Empty;
    public string LicenseUrl { get; init; } = string.Empty;
    public string Creator { get; init; } = string.Empty;
    public string CreatorUrl { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string LicenseVersion { get; init; } = string.Empty;
    public string Attribution { get; init; } = string.Empty;
    public string MediaCategory { get; init; } = string.Empty;
    public IReadOnlyList<ImageFormatReference> Formats { get; init; } =
        Array.Empty<ImageFormatReference>();
}
