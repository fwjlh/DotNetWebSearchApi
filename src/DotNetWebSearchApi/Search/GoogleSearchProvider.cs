using System.Net;
using System.Text.RegularExpressions;

namespace DotNetWebSearchApi.Search;

public sealed class GoogleSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://www.google.com/search";
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public GoogleSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "google";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUrl(query.Text));
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        string html = response.Content;

        if (IsCaptchaResponse(response, html))
        {
            throw new ProviderFailureException(ProviderFailureKind.Challenge, "Google returned a CAPTCHA or blocking page.");
        }

        response.EnsureSuccessStatusCode(Name);
        return GoogleSearchResultParser.Parse(html);
    }

    private static string BuildRequestUrl(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return $"{SearchEndpoint}?q={encodedQuery}&sca_esv=1&hl=en&lr=lang_en&ie=utf8&oe=utf8"
            + "&adtest=on&adtest-useragent=SAMSUNG-GT-S5230&adtest=preview";
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
