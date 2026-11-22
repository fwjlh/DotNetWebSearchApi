using System.Net;

namespace DotNetWebSearchApi.Search;

public sealed class DuckDuckGoSearchProvider : ISearchProvider
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

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Text.Length > MaximumQueryLength)
        {
            return Array.Empty<SearchResult>();
        }

        using FormUrlEncodedContent content = BuildFormContent(query.Text);
        using HttpRequestMessage request = new(HttpMethod.Post, SearchEndpoint) { Content = content };
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);

        if (DuckDuckGoSearchResultParser.IsCaptchaPage(response.Content))
        {
            throw new ProviderFailureException(ProviderFailureKind.Challenge, "DuckDuckGo returned a CAPTCHA page.");
        }

        if (response.StatusCode == HttpStatusCode.SeeOther)
        {
            return Array.Empty<SearchResult>();
        }

        response.EnsureSuccessStatusCode(Name);
        return DuckDuckGoSearchResultParser.Parse(response.Content);
    }

    private static FormUrlEncodedContent BuildFormContent(string query)
    {
        KeyValuePair<string, string>[] fields =
        {
            new("q", query),
            new("b", string.Empty),
            new("kl", "wt-wt")
        };

        return new FormUrlEncodedContent(fields);
    }
}
