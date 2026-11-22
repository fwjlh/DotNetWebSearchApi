using System.Text.Json;

namespace DotNetWebSearchApi.Search;

public sealed class BraveSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://api.search.brave.com/res/v1/web/search";
    private const int MaximumPageSize = 20;
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public BraveSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "brave";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUrl(query));
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        response.EnsureSuccessStatusCode(Name);
        using JsonDocument document = JsonDocument.Parse(response.Content);

        return BraveSearchResultParser.Parse(document.RootElement);
    }

    private static string BuildRequestUrl(SearchQuery query)
    {
        int count = Math.Clamp(query.Limit, 1, MaximumPageSize);
        string encodedQuery = Uri.EscapeDataString(query.Text);
        return $"{SearchEndpoint}?q={encodedQuery}&count={count}&offset=0&text_decorations=false&safesearch=moderate";
    }
}
