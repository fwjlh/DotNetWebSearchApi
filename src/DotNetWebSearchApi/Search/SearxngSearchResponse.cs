using System.Text.Json.Serialization;

namespace DotNetWebSearchApi.Search;

public sealed record SearxngSearchResponse(
    string Query,
    [property: JsonPropertyName("number_of_results")] int NumberOfResults,
    IReadOnlyList<SearxngSearchResult> Results,
    IReadOnlyList<string> Answers,
    IReadOnlyList<string> Corrections,
    IReadOnlyList<object> Infoboxes,
    IReadOnlyList<string> Suggestions,
    [property: JsonPropertyName("unresponsive_engines")] IReadOnlyList<string> UnresponsiveEngines,
    [property: JsonPropertyName("unsupported_engines")] IReadOnlyList<string> UnsupportedEngines,
    [property: JsonPropertyName("cooling_down_engines")] IReadOnlyList<string> CoolingDownEngines)
{
    [JsonPropertyName("provider_outcomes")]
    public IReadOnlyList<ProviderOutcome> ProviderOutcomes { get; init; } = Array.Empty<ProviderOutcome>();
}

public sealed record SearxngSearchResult(
    string Url,
    string Title,
    string Content,
    string Engine,
    IReadOnlyList<string> Engines,
    IReadOnlyDictionary<string, int> Positions,
    double Score,
    string Category,
    string Template,
    [property: JsonPropertyName("parsed_url")] IReadOnlyList<string> ParsedUrl,
    [property: JsonPropertyName("publishedDate")] DateTimeOffset? PublishedDate)
{
    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; init; }

    [JsonPropertyName("img_src")]
    public string? ImageUrl { get; init; }

    [JsonPropertyName("thumbnail_src")]
    public string? ImageThumbnailUrl { get; init; }

    [JsonPropertyName("resolution")]
    public string? ImageResolution { get; init; }

    [JsonPropertyName("img_format")]
    public string? ImageFormat { get; init; }

    [JsonPropertyName("source")]
    public string? ImageSource { get; init; }

    [JsonPropertyName("filesize")]
    public string? ImageFileSize { get; init; }

    [JsonPropertyName("formats")]
    public IReadOnlyList<ImageFormatReference> ImageFormats { get; init; } =
        Array.Empty<ImageFormatReference>();

    [JsonPropertyName("image_metadata")]
    public ImageResultMetadata? ImageMetadata { get; init; }

    [JsonPropertyName("iframe_src")]
    public string? VideoEmbedUrl { get; init; }

    [JsonPropertyName("length")]
    public double? VideoDurationSeconds { get; init; }

    [JsonPropertyName("author")]
    public string? VideoAuthor { get; init; }

    [JsonPropertyName("views")]
    public string? VideoViews { get; init; }

    [JsonPropertyName("video_metadata")]
    public VideoResultMetadata? VideoMetadata { get; init; }

    [JsonPropertyName("audio_src")]
    public string? AudioUrl { get; init; }

    [JsonPropertyName("music_metadata")]
    public MusicResultMetadata? MusicMetadata { get; init; }
}
