namespace DotNetWebSearchApi.Search;

public sealed class BingSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://www.bing.com/search";
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public BingSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "bing";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        string requestUrl = BuildRequestUrl(query.Text);
        using HttpRequestMessage request = new(HttpMethod.Get, requestUrl);
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        response.EnsureSuccessStatusCode(Name);
        return BingSearchResultParser.Parse(response.Content);
    }

    private static string BuildRequestUrl(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return $"{SearchEndpoint}?q={encodedQuery}&adlt=off";
    }
}
