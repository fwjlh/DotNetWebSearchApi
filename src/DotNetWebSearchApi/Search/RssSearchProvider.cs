using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace DotNetWebSearchApi.Search;

public sealed class RssSearchProvider : ISearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly RssSearchDefinition _definition;
    private readonly SearchHttpTransport _transport;

    public RssSearchProvider(
        HttpClient httpClient,
        RssSearchDefinition definition,
        SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _definition = definition;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => _definition.Name;

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "news" },
        false,
        1,
        false,
        false,
        false);

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUri(query));
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        response.EnsureSuccessStatusCode(Name);

        using StringReader textReader = new(response.Content);
        using XmlReader reader = XmlReader.Create(textReader, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            IgnoreComments = true
        });
        XDocument document = XDocument.Load(reader, LoadOptions.None);
        return ReadResults(document, query.Limit);
    }

    private Uri BuildRequestUri(SearchQuery query)
    {
        Dictionary<string, string> parameters = _definition.FixedParameters.ToDictionary(
            item => item.Key,
            item => item.Value,
            StringComparer.Ordinal);
        parameters[_definition.QueryParameter] = query.Text;
        string queryString = string.Join("&", parameters.Select(item =>
            Uri.EscapeDataString(item.Key) + "=" + Uri.EscapeDataString(item.Value)));
        return new Uri(_definition.Endpoint + "?" + queryString);
    }

    private IReadOnlyList<SearchResult> ReadResults(XDocument document, int limit)
    {
        XElement? root = document.Root;
        if (root is null)
        {
            return Array.Empty<SearchResult>();
        }

        XNamespace atom = "http://www.w3.org/2005/Atom";
        IEnumerable<XElement> items = root.Element("channel")?.Elements("item")
            ?? root.Elements(atom + "entry");
        List<SearchResult> results = new();
        foreach (XElement item in items)
        {
            if (!TryReadResult(item, atom, results.Count, out SearchResult? result) || result is null)
            {
                continue;
            }

            results.Add(result);
            if (results.Count >= Math.Clamp(limit, 1, 50))
            {
                break;
            }
        }

        return results;
    }

    private bool TryReadResult(XElement item, XNamespace atom, int position, out SearchResult? result)
    {
        result = null;
        string title = CleanText(item.Element("title")?.Value ?? item.Element(atom + "title")?.Value ?? string.Empty);
        string link = item.Element("link")?.Value ?? item.Elements(atom + "link")
            .FirstOrDefault(element => (string?)element.Attribute("rel") is null
                || (string?)element.Attribute("rel") == "alternate")?.Attribute("href")?.Value ?? string.Empty;
        link = ResolveArticleUrl(link);
        if (string.IsNullOrWhiteSpace(title) || !Uri.TryCreate(link, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        string snippet = CleanText(item.Element("description")?.Value
            ?? item.Element("summary")?.Value
            ?? item.Element(atom + "summary")?.Value
            ?? item.Element(atom + "content")?.Value
            ?? string.Empty);
        result = new SearchResult(title, uri.AbsoluteUri, snippet, Name, 1.0 / (position + 1))
        {
            Category = "news",
            PublishedDate = ParsePublishedDate(item, atom)
        };
        return true;
    }

    private static DateTimeOffset? ParsePublishedDate(XElement item, XNamespace atom)
    {
        string value = item.Element("pubDate")?.Value
            ?? item.Element(atom + "published")?.Value
            ?? item.Element(atom + "updated")?.Value
            ?? string.Empty;
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
            out DateTimeOffset publishedDate) ? publishedDate : null;
    }

    private static string ResolveArticleUrl(string link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out Uri? uri)
            || !uri.Host.EndsWith("bing.com", StringComparison.OrdinalIgnoreCase))
        {
            return link;
        }

        string? destination = uri.Query.TrimStart('?').Split('&')
            .Select(part => part.Split('=', 2))
            .Where(parts => parts.Length == 2 && parts[0] == "url")
            .Select(parts => Uri.UnescapeDataString(parts[1]))
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(destination) ? link : destination;
    }

    private static string CleanText(string value)
    {
        return WebUtility.HtmlDecode(Regex.Replace(value, "<[^>]+>", string.Empty)).Trim();
    }
}

public sealed record RssSearchDefinition
{
    public required string Name { get; init; }
    public required string Endpoint { get; init; }
    public required string QueryParameter { get; init; }
    public IReadOnlyDictionary<string, string> FixedParameters { get; init; } =
        new Dictionary<string, string>();
}

public static class RssProviderCatalog
{
    private static readonly IReadOnlyList<RssSearchDefinition> Definitions = new[]
    {
        new RssSearchDefinition
        {
            Name = "google_news",
            Endpoint = "https://news.google.com/rss/search",
            QueryParameter = "q",
            FixedParameters = new Dictionary<string, string>
            {
                ["hl"] = "en-GB",
                ["gl"] = "GB",
                ["ceid"] = "GB:en"
            }
        },
        new RssSearchDefinition
        {
            Name = "bing_news",
            Endpoint = "https://www.bing.com/news/search",
            QueryParameter = "q",
            FixedParameters = new Dictionary<string, string>
            {
                ["format"] = "rss",
                ["setmkt"] = "en-GB"
            }
        }
    };

    public static void Register(IServiceCollection services)
    {
        foreach (RssSearchDefinition definition in Definitions)
        {
            services.AddHttpClient(definition.Name, client =>
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)");
                client.DefaultRequestHeaders.Accept.ParseAdd("application/rss+xml, application/atom+xml, application/xml");
            });
            services.AddSingleton<ISearchProvider>(serviceProvider =>
                new RssSearchProvider(
                    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(definition.Name),
                    definition,
                    serviceProvider.GetRequiredService<SearchHttpTransport>()));
        }
    }
}
