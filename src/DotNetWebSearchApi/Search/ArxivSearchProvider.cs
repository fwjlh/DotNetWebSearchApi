using System.Xml;
using System.Xml.Linq;

namespace DotNetWebSearchApi.Search;

public sealed class ArxivSearchProvider : ISearchProvider
{
    private static readonly XNamespace AtomNamespace = "http://www.w3.org/2005/Atom";
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public ArxivSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "arxiv";

    public TimeSpan Timeout => TimeSpan.FromSeconds(10);

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

            results.Add(new SearchResult(
                title.Replace('\n', ' ').Trim(),
                resultUri.AbsoluteUri,
                ReadElement(entry, "summary").Replace('\n', ' ').Trim(),
                "arxiv",
                1.0 / (results.Count + 1)));
        }

        return results;
    }

    private static string ReadElement(XElement entry, string name)
    {
        return entry.Element(AtomNamespace + name)?.Value ?? string.Empty;
    }
}
