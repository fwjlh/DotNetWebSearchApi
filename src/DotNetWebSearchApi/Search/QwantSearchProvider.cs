using System.Net;
using System.Text.Json;

namespace DotNetWebSearchApi.Search;

public sealed class QwantSearchProvider : ISearchProvider, ISearchProviderCapabilities
{
    private const string SearchEndpoint = "https://api.qwant.com/v3/search/web";
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public QwantSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "qwant";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general", "web" },
        true,
        5,
        false,
        true,
        true);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUrl(query));
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            ProviderFailureKind kind = response.Content.Contains("captcha", StringComparison.OrdinalIgnoreCase)
                || response.Content.Contains("datadome", StringComparison.OrdinalIgnoreCase)
                ? ProviderFailureKind.Challenge
                : ProviderFailureKind.AccessDenied;
            throw new ProviderFailureException(
                kind,
                "Qwant denied the request; a DataDome session or browser verification may be required.",
                response.RetryAfter,
                challengeKind: kind == ProviderFailureKind.Challenge ? ProviderChallengeKind.BrowserVerification : null);
        }

        response.EnsureSuccessStatusCode(Name);
        using JsonDocument document = JsonDocument.Parse(response.Content);
        ThrowForApiError(document.RootElement);
        return QwantSearchResultParser.Parse(document.RootElement);
    }

    private static string BuildRequestUrl(SearchQuery query)
    {
        string locale = EngineLocaleRegistry.GetRegion("qwant", query.Language, "en_US") ?? "en_US";
        List<KeyValuePair<string, string>> parameters = new()
        {
            new("t", "web"),
            new("q", query.Text),
            new("count", "10"),
            new("locale", locale),
            new("offset", ((query.Page - 1) * 10).ToString()),
            new("tgp", "1"),
            new("device", "desktop"),
            new("safesearch", query.SafeSearch.ToString())
        };
        string queryString = string.Join("&", parameters.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        return $"{SearchEndpoint}?{queryString}";
    }

    private static void ThrowForApiError(JsonElement root)
    {
        if (!root.TryGetProperty("status", out JsonElement statusElement)
            || statusElement.ValueKind != JsonValueKind.String
            || statusElement.GetString() == "success")
        {
            return;
        }

        if (root.TryGetProperty("data", out JsonElement data)
            && data.TryGetProperty("error_code", out JsonElement codeElement)
            && codeElement.TryGetInt32(out int code)
            && code == 24)
        {
            throw new ProviderFailureException(ProviderFailureKind.RateLimited, "Qwant rate limited the search request.");
        }

        if (root.TryGetProperty("url", out JsonElement urlElement)
            && urlElement.ValueKind == JsonValueKind.String)
        {
            throw new ProviderFailureException(
                ProviderFailureKind.Challenge,
                "Qwant redirected the request to a verification URL.",
                challengeKind: ProviderChallengeKind.BrowserVerification);
        }

        throw new ProviderFailureException(ProviderFailureKind.InvalidResponse, "Qwant returned an API error response.");
    }
}
