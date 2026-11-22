using System.Net;

namespace DotNetWebSearchApi.Search;

public sealed class DuckDuckGoSearchProvider : ISearchProvider, ISearchProviderCapabilities
{
    private const string SearchEndpoint = "https://html.duckduckgo.com/html/";
    private const int MaximumQueryLength = 499;
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public DuckDuckGoSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "duckduckgo";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general", "web" },
        false,
        1,
        true,
        false,
        true);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Text.Length > MaximumQueryLength)
        {
            return Array.Empty<SearchResult>();
        }

        string region = EngineLocaleRegistry.GetRegion("duckduckgo", query.Language) ?? "wt-wt";
        string? timeRange = GetTimeRangeCode(query.TimeRange);
        using FormUrlEncodedContent content = BuildFormContent(query.Text, region, timeRange);
        using HttpRequestMessage request = new(HttpMethod.Post, SearchEndpoint) { Content = content };
        AddPreferenceCookies(request, region, timeRange);
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);

        if (DuckDuckGoSearchResultParser.IsCaptchaPage(response.Content))
        {
            throw new ProviderFailureException(
                ProviderFailureKind.Challenge,
                "DuckDuckGo returned an interactive CAPTCHA page.",
                challengeKind: ProviderChallengeKind.InteractiveCaptcha);
        }

        if (response.StatusCode == HttpStatusCode.SeeOther)
        {
            return Array.Empty<SearchResult>();
        }

        response.EnsureSuccessStatusCode(Name);
        return DuckDuckGoSearchResultParser.Parse(response.Content);
    }

    private static FormUrlEncodedContent BuildFormContent(string query, string region, string? timeRange)
    {
        List<KeyValuePair<string, string>> fields = new()
        {
            new("q", query),
            new("b", string.Empty),
            new("kl", region)
        };
        if (timeRange is not null)
        {
            fields.Add(new KeyValuePair<string, string>("df", timeRange));
        }

        return new FormUrlEncodedContent(fields);
    }

    private static void AddPreferenceCookies(HttpRequestMessage request, string region, string? timeRange)
    {
        List<string> cookies = new();
        if (!region.Equals("wt-wt", StringComparison.OrdinalIgnoreCase))
        {
            cookies.Add($"kl={region}");
        }

        if (timeRange is not null)
        {
            cookies.Add($"df={timeRange}");
        }

        if (cookies.Count > 0)
        {
            request.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies));
        }
    }

    private static string? GetTimeRangeCode(SearchTimeRange? timeRange)
    {
        return timeRange switch
        {
            SearchTimeRange.Day => "d",
            SearchTimeRange.Week => "w",
            SearchTimeRange.Month => "m",
            SearchTimeRange.Year => "y",
            _ => null
        };
    }
}
