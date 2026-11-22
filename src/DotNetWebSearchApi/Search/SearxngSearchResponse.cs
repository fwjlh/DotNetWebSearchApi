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
    [property: JsonPropertyName("unresponsive_engines")] IReadOnlyList<string> UnresponsiveEngines);

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
    [property: JsonPropertyName("publishedDate")] DateTimeOffset? PublishedDate);
