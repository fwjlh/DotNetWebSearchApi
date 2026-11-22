namespace DotNetWebSearchApi.Search;

public sealed class NaverSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://search.naver.com/search.naver";
    private readonly HttpClient _httpClient;

    public NaverSearchProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "naver";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        string requestUrl = BuildRequestUrl(query.Text);
        using HttpResponseMessage response = await _httpClient.GetAsync(requestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        string html = await response.Content.ReadAsStringAsync(cancellationToken);
        return NaverSearchResultParser.Parse(html);
    }

    private static string BuildRequestUrl(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return $"{SearchEndpoint}?query={encodedQuery}&start=1&where=web";
    }
}
