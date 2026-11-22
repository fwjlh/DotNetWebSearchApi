namespace DotNetWebSearchApi.Search;

public sealed class BingSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://www.bing.com/search";
    private readonly HttpClient _httpClient;

    public BingSearchProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "bing";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        string requestUrl = BuildRequestUrl(query.Text);
        using HttpResponseMessage response = await _httpClient.GetAsync(requestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        string html = await response.Content.ReadAsStringAsync(cancellationToken);
        return BingSearchResultParser.Parse(html);
    }

    private static string BuildRequestUrl(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return $"{SearchEndpoint}?q={encodedQuery}&adlt=off";
    }
}
