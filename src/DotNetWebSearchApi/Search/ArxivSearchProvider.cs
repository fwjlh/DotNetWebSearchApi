using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace DotNetWebSearchApi.Search;

public sealed class ArxivSearchProvider : ISearchProvider, ISearchProviderCapabilities
{
    private static readonly XNamespace AtomNamespace = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace ArxivNamespace = "http://arxiv.org/schemas/atom";
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public ArxivSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "arxiv";

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "science" },
        false,
        1,
        false,
        false,
        false);

    public TimeSpan Timeout => TimeSpan.FromSeconds(20);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUrl(query));
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        response.EnsureSuccessStatusCode(Name);

        using StringReader textReader = new(response.Content);
        XmlReaderSettings settings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };
        using XmlReader reader = XmlReader.Create(textReader, settings);
        XDocument document = XDocument.Load(reader, LoadOptions.None);
        return ReadResults(document);
    }

    private static string BuildRequestUrl(SearchQuery query)
    {
        string search = Uri.EscapeDataString("all:" + query.Text);
        int limit = Math.Clamp(query.Limit, 1, 50);
        return $"https://export.arxiv.org/api/query?search_query={search}&start=0&max_results={limit}";
    }

    private static IReadOnlyList<SearchResult> ReadResults(XDocument document)
    {
        List<SearchResult> results = new();
        IEnumerable<XElement> entries = document.Root?.Elements(AtomNamespace + "entry")
            ?? Enumerable.Empty<XElement>();

        foreach (XElement entry in entries)
        {
            string title = ReadElement(entry, "title");
            string url = ReadElement(entry, "id");
            if (string.IsNullOrWhiteSpace(title)
                || !Uri.TryCreate(url, UriKind.Absolute, out Uri? resultUri)
                || resultUri.Scheme != Uri.UriSchemeHttps)
            {
                continue;
            }

            PaperResultMetadata paper = ReadPaperMetadata(entry, resultUri);
            results.Add(new SearchResult(
                title.Replace('\n', ' ').Trim(),
                resultUri.AbsoluteUri,
                ReadElement(entry, "summary").Replace('\n', ' ').Trim(),
                "arxiv",
                1.0 / (results.Count + 1))
            {
                Category = "science",
                PublishedDate = paper.DateOfPublication,
                PaperMetadata = paper
            });
        }

        return results;
    }

    private static PaperResultMetadata ReadPaperMetadata(XElement entry, Uri resultUri)
    {
        string summary = ReadElement(entry, "summary").Replace('\n', ' ').Trim();
        string[] authors = entry.Elements(AtomNamespace + "author")
            .Select(author => author.Element(AtomNamespace + "name")?.Value.Trim() ?? string.Empty)
            .Where(author => author.Length > 0)
            .ToArray();
        string pdfUrl = entry.Elements(AtomNamespace + "link")
            .Where(link => string.Equals((string?)link.Attribute("title"), "pdf", StringComparison.OrdinalIgnoreCase))
            .Select(link => (string?)link.Attribute("href") ?? string.Empty)
            .FirstOrDefault(IsHttpsUrl) ?? string.Empty;
        DateTimeOffset? publishedDate = DateTimeOffset.TryParse(
            ReadElement(entry, "published"),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset parsedDate) ? parsedDate : null;
        string[] tags = entry.Elements(AtomNamespace + "category")
            .Select(category => (string?)category.Attribute("term") ?? string.Empty)
            .Where(tag => tag.Length > 0)
            .ToArray();

        return new PaperResultMetadata
        {
            DateOfPublication = publishedDate,
            Content = summary,
            Comments = entry.Element(ArxivNamespace + "comment")?.Value.Trim() ?? string.Empty,
            Tags = tags,
            Type = "preprint",
            Author = authors.FirstOrDefault() ?? string.Empty,
            Authors = authors,
            Journal = entry.Element(ArxivNamespace + "journal_ref")?.Value.Trim() ?? string.Empty,
            Doi = entry.Element(ArxivNamespace + "doi")?.Value.Trim() ?? string.Empty,
            PdfUrl = pdfUrl,
            HtmlUrl = resultUri.AbsoluteUri
        };
    }

    private static bool IsHttpsUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
            && uri.Scheme == Uri.UriSchemeHttps;
    }

    private static string ReadElement(XElement entry, string name)
    {
        return entry.Element(AtomNamespace + name)?.Value ?? string.Empty;
    }
}
