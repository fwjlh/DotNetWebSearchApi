using System.Net;

namespace DotNetWebSearchApi.Search;

public sealed class GoogleSearchProvider : ISearchProvider, ISearchProviderCapabilities
{
    private const string SearchEndpoint = "https://www.google.com/search";
    private static readonly IReadOnlyDictionary<SearchTimeRange, string> TimeRangeValues =
        new Dictionary<SearchTimeRange, string>
        {
            [SearchTimeRange.Day] = "d",
            [SearchTimeRange.Week] = "w",
            [SearchTimeRange.Month] = "m",
            [SearchTimeRange.Year] = "y"
        };
    private static readonly IReadOnlyDictionary<int, string> SafeSearchValues =
        new Dictionary<int, string>
        {
            [1] = "medium",
            [2] = "high"
        };
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public GoogleSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "google";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general", "web" },
        true,
        50,
        true,
        true,
        true);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUrl(query));
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        string html = response.Content;

        if (IsCaptchaResponse(response, html))
        {
            throw new ProviderFailureException(
                ProviderFailureKind.Challenge,
                "Google returned a CAPTCHA or human-verification page.",
                challengeKind: ProviderChallengeKind.InteractiveCaptcha);
        }

        response.EnsureSuccessStatusCode(Name);
        return GoogleSearchResultParser.Parse(html);
    }

    private static string BuildRequestUrl(SearchQuery query)
    {
        string language = EngineLocaleRegistry.GetLanguage("google", query.Language, "lang_en") ?? "lang_en";
        string languageCode = language.Split('_').Last();
        string? country = EngineLocaleRegistry.GetRegion("google", query.Language);
        List<KeyValuePair<string, string>> parameters = new()
        {
            new("q", query.Text),
            new("sca_esv", "1"),
            new("hl", languageCode),
            new("lr", query.Language.Equals("auto", StringComparison.OrdinalIgnoreCase) ? string.Empty : language),
            new("ie", "utf8"),
            new("oe", "utf8")
        };

        if (query.Language.Contains("-", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(country)
            && !country.Equals("ZZ", StringComparison.OrdinalIgnoreCase))
        {
            parameters.Add(new KeyValuePair<string, string>("cr", $"country{country}"));
        }

        if (query.Page > 1)
        {
            parameters.Add(new KeyValuePair<string, string>("start", ((query.Page - 1) * 10).ToString()));
        }

        if (query.TimeRange.HasValue && TimeRangeValues.TryGetValue(query.TimeRange.Value, out string? timeRange))
        {
            parameters.Add(new KeyValuePair<string, string>("tbs", $"qdr:{timeRange}"));
        }

        if (SafeSearchValues.TryGetValue(query.SafeSearch, out string? safeSearch))
        {
            parameters.Add(new KeyValuePair<string, string>("safe", safeSearch));
        }

        parameters.Add(new KeyValuePair<string, string>("adtest", "on"));
        parameters.Add(new KeyValuePair<string, string>("adtest-useragent", "SAMSUNG-GT-S5230"));
        parameters.Add(new KeyValuePair<string, string>("adtest", "preview"));

        string queryString = string.Join("&", parameters.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        return $"{SearchEndpoint}?{queryString}";
    }

    private static bool IsCaptchaResponse(SearchHttpResponse response, string html)
    {
        Uri responseUri = response.RequestUri;
        bool redirectedToCaptcha = responseUri.Host.Equals("sorry.google.com", StringComparison.OrdinalIgnoreCase)
            || responseUri.AbsolutePath.StartsWith("/sorry", StringComparison.OrdinalIgnoreCase);

        return redirectedToCaptcha
            || response.StatusCode == HttpStatusCode.Redirect
            || (html.Length < 2000 && html.Contains("/sorry/", StringComparison.OrdinalIgnoreCase));
    }
}
