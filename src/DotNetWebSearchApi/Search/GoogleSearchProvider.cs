using System.Net;
using System.Text.RegularExpressions;

namespace DotNetWebSearchApi.Search;

public sealed class GoogleSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://www.google.com/search";
    private readonly HttpClient _httpClient;

    public GoogleSearchProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "google";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(
            BuildRequestUrl(query.Text),
            cancellationToken);
        string html = await response.Content.ReadAsStringAsync(cancellationToken);

        if (IsCaptchaResponse(response, html))
        {
            throw new HttpRequestException("Google returned a CAPTCHA or blocking page.");
        }

        response.EnsureSuccessStatusCode();
        return GoogleSearchResultParser.Parse(html);
    }

    private static string BuildRequestUrl(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return $"{SearchEndpoint}?q={encodedQuery}&sca_esv=1&hl=en&lr=lang_en&ie=utf8&oe=utf8"
            + "&adtest=on&adtest-useragent=SAMSUNG-GT-S5230&adtest=preview";
    }

    private static bool IsCaptchaResponse(HttpResponseMessage response, string html)
    {
        Uri? responseUri = response.RequestMessage?.RequestUri;
        bool redirectedToCaptcha = responseUri is not null
            && (responseUri.Host.Equals("sorry.google.com", StringComparison.OrdinalIgnoreCase)
                || responseUri.AbsolutePath.StartsWith("/sorry", StringComparison.OrdinalIgnoreCase));

        return redirectedToCaptcha
            || response.StatusCode == HttpStatusCode.Redirect
            || (html.Length < 2000 && html.Contains("/sorry/", StringComparison.OrdinalIgnoreCase));
    }
}
