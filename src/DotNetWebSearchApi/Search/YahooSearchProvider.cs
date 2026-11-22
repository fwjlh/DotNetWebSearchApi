using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetWebSearchApi.Search;

public sealed class YahooSearchProvider : ISearchProvider, ISearchProviderCapabilities
{
    private const string SearchEndpoint = "https://search.yahoo.com/search";
    private const int MaximumRedirects = 4;
    private const int MaximumPage = 20;
    private static readonly IReadOnlyDictionary<SearchTimeRange, string> TimeRangeValues =
        new Dictionary<SearchTimeRange, string>
        {
            [SearchTimeRange.Day] = "d",
            [SearchTimeRange.Week] = "w",
            [SearchTimeRange.Month] = "m"
        };
    private static readonly IReadOnlyDictionary<int, string> SafeSearchValues =
        new Dictionary<int, string>
        {
            [0] = "p",
            [1] = "i",
            [2] = "r"
        };
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookies;
    private readonly SearchHttpTransport _transport;

    public YahooSearchProvider(
        HttpClient httpClient,
        [FromKeyedServices("yahoo")] CookieContainer cookies,
        SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _cookies = cookies;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "yahoo";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general", "web" },
        true,
        MaximumPage,
        true,
        true,
        true)
    {
        SupportedTimeRanges = new HashSet<SearchTimeRange>
        {
            SearchTimeRange.Day,
            SearchTimeRange.Week,
            SearchTimeRange.Month
        }
    };

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        Uri requestUri = BuildRequestUri(query);
        AddSearchCookie(requestUri, query);

        for (int redirect = 0; redirect <= MaximumRedirects; redirect++)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
            SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
            CaptureYbvCookie(response);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return YahooSearchResultParser.Parse(response.Content);
            }

            if (!TryGetRedirect(response, requestUri, out Uri? nextUri) || nextUri is null)
            {
                response.EnsureSuccessStatusCode(Name);
                return Array.Empty<SearchResult>();
            }

            requestUri = nextUri;
        }

        throw new HttpRequestException("Yahoo exceeded the permitted redirect count.");
    }

    private void AddSearchCookie(Uri requestUri, SearchQuery query)
    {
        string safeSearch = SafeSearchValues[query.SafeSearch];
        string language = EngineLocaleRegistry.GetLanguage("yahoo", query.Language, "any") ?? "any";
        string cookieValue = $"v=1&vm={safeSearch}&fl=1&vl=lang_{language}&pn=10&rw=new&userset=1";
        _cookies.Add(requestUri, new Cookie("sB", cookieValue, "/", ".yahoo.com"));
    }

    private void CaptureYbvCookie(SearchHttpResponse response)
    {
        if (!response.RequestUri.Host.EndsWith(".yahoo.com", StringComparison.OrdinalIgnoreCase)
            || !response.TryGetHeader("Set-Cookie", out IReadOnlyList<string>? values)
            || values is null)
        {
            return;
        }

        bool hasYbvCookie = values.Any(value => value.StartsWith("YBV=", StringComparison.OrdinalIgnoreCase));
        if (!hasYbvCookie)
        {
            return;
        }

        foreach (string value in values)
        {
            _cookies.SetCookies(response.RequestUri, value);
        }

        Cookie? ybv = _cookies.GetCookies(response.RequestUri)["YBV"];
        if (ybv is null || !ybv.Value.StartsWith("v0.2", StringComparison.Ordinal))
        {
            return;
        }

        _cookies.Add(
            new Uri("https://search.yahoo.com/"),
            new Cookie("YBV", ybv.Value, "/", ".yahoo.com")
            {
                Expires = DateTime.UtcNow.AddHours(24),
                Secure = true,
                HttpOnly = true
            });
    }

    private static Uri BuildRequestUri(SearchQuery query)
    {
        string domain = GetSearchDomain(query.Language);
        List<KeyValuePair<string, string>> parameters = new()
        {
            new("p", query.Text)
        };

        if (query.TimeRange.HasValue && TimeRangeValues.TryGetValue(query.TimeRange.Value, out string? timeRange))
        {
            parameters.Add(new KeyValuePair<string, string>("btf", timeRange));
        }

        if (query.Page == 1)
        {
            parameters.Add(new KeyValuePair<string, string>("iscqry", string.Empty));
        }
        else
        {
            parameters.Add(new KeyValuePair<string, string>("b", (query.Page * 7 + 1).ToString()));
            parameters.Add(new KeyValuePair<string, string>("pz", "7"));
            parameters.Add(new KeyValuePair<string, string>("bct", "0"));
            parameters.Add(new KeyValuePair<string, string>("xargs", "0"));
        }

        string queryString = string.Join("&", parameters.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        return new Uri($"https://{domain}/search?{queryString}");
    }

    private static string GetSearchDomain(string language)
    {
        string? region = EngineLocaleRegistry.GetRegion("yahoo", language);
        return EngineLocaleRegistry.GetCustom(
            "yahoo",
            "region2domain",
            region ?? string.Empty,
            "search.yahoo.com")!;
    }

    private static bool TryGetRedirect(
        SearchHttpResponse response,
        Uri currentUri,
        out Uri? nextUri)
    {
        nextUri = null;
        if (response.StatusCode is not HttpStatusCode.Redirect and not HttpStatusCode.TemporaryRedirect
            || !response.TryGetHeader("Location", out IReadOnlyList<string>? values)
            || values is null
            || values.Count == 0
            || !Uri.TryCreate(currentUri, values[0], out nextUri))
        {
            return false;
        }

        return true;
    }
}
