namespace DotNetWebSearchApi.Search;

public sealed class NaverSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://search.naver.com/search.naver";
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public NaverSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "naver";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUrl(query.Text));
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        response.EnsureSuccessStatusCode(Name);
        return NaverSearchResultParser.Parse(response.Content);
    }

    private static string BuildRequestUrl(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return $"{SearchEndpoint}?query={encodedQuery}&start=1&where=web";
    }
}
