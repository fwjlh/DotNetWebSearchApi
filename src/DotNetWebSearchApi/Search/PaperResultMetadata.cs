namespace DotNetWebSearchApi.Search;

public sealed record PaperResultMetadata
{
    public DateTimeOffset? DateOfPublication { get; init; }
    public string PublicationYear { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Comments { get; init; } = string.Empty;
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public string Type { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public IReadOnlyList<string> Authors { get; init; } = Array.Empty<string>();
    public string Editor { get; init; } = string.Empty;
    public string Publisher { get; init; } = string.Empty;
    public string Journal { get; init; } = string.Empty;
    public string Volume { get; init; } = string.Empty;
    public string Pages { get; init; } = string.Empty;
    public string Number { get; init; } = string.Empty;
    public string Doi { get; init; } = string.Empty;
    public IReadOnlyList<string> Issn { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Isbn { get; init; } = Array.Empty<string>();
    public string PdfUrl { get; init; } = string.Empty;
    public string HtmlUrl { get; init; } = string.Empty;
}
