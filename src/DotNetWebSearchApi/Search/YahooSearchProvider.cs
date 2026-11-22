using System.Net;

namespace DotNetWebSearchApi.Search;

public sealed class YahooSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://search.yahoo.com/search";
    private const int MaximumRedirects = 4;
    private readonly HttpClient _httpClient;
    private readonly CookieContainer _cookies;

    public YahooSearchProvider(HttpClient httpClient, CookieContainer cookies)
    {
        _httpClient = httpClient;
        _cookies = cookies;
    }

    public string Name => "yahoo";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        Uri requestUri = BuildRequestUri(query.Text);
        AddSearchCookie(requestUri);

        for (int redirect = 0; redirect <= MaximumRedirects; redirect++)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                string html = await response.Content.ReadAsStringAsync(cancellationToken);
                return YahooSearchResultParser.Parse(html);
            }

            if (!TryGetRedirect(response, requestUri, out Uri? nextUri) || nextUri is null)
            {
                response.EnsureSuccessStatusCode();
                return Array.Empty<SearchResult>();
            }

            requestUri = nextUri;
        }

        throw new HttpRequestException("Yahoo exceeded the permitted redirect count.");
    }

    private void AddSearchCookie(Uri requestUri)
    {
        const string cookieValue = "v=1&vm=p&fl=1&vl=lang_en&pn=10&rw=new&userset=1";
        _cookies.Add(requestUri, new Cookie("sB", cookieValue, "/", ".yahoo.com"));
    }

    private static Uri BuildRequestUri(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return new Uri($"{SearchEndpoint}?p={encodedQuery}&iscqry=");
    }

    private static bool TryGetRedirect(
        HttpResponseMessage response,
        Uri currentUri,
        out Uri? nextUri)
    {
        nextUri = null;
        if (response.StatusCode is not HttpStatusCode.Redirect and not HttpStatusCode.TemporaryRedirect
            || response.Headers.Location is null)
        {
            return false;
        }

        nextUri = new Uri(currentUri, response.Headers.Location);
        return true;
    }
}
