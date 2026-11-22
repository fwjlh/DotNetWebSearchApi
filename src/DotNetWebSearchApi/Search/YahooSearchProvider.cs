using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetWebSearchApi.Search;

public sealed class YahooSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://search.yahoo.com/search";
    private const int MaximumRedirects = 4;
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

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        Uri requestUri = BuildRequestUri(query.Text);
        AddSearchCookie(requestUri);

        for (int redirect = 0; redirect <= MaximumRedirects; redirect++)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
            SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);

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
