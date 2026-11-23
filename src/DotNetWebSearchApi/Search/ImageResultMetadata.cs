namespace DotNetWebSearchApi.Search;

public sealed record ImageFormatReference(
    string Url,
    string Subtype,
    string Label,
    string MediaType = "image");

public sealed record ImageResultMetadata
{
    public string ImageUrl { get; init; } = string.Empty;
    public string ThumbnailUrl { get; init; } = string.Empty;
    public string Resolution { get; init; } = string.Empty;
    public string Format { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public long? FileSizeBytes { get; init; }
    public string License { get; init; } = string.Empty;
    public string LicenseUrl { get; init; } = string.Empty;
    public string Creator { get; init; } = string.Empty;
    public IReadOnlyList<ImageFormatReference> Formats { get; init; } =
        Array.Empty<ImageFormatReference>();
}
