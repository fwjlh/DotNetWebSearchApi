using System.Text.Json;

namespace DotNetWebSearchApi.Search;

public sealed class BraveSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://api.search.brave.com/res/v1/web/search";
    private const int MaximumPageSize = 20;
    private readonly HttpClient _httpClient;

    public BraveSearchProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "brave";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        string requestUrl = BuildRequestUrl(query);
        using HttpResponseMessage response = await _httpClient.GetAsync(requestUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(
            responseStream,
            cancellationToken: cancellationToken);

        return BraveSearchResultParser.Parse(document.RootElement);
    }

    private static string BuildRequestUrl(SearchQuery query)
    {
        int count = Math.Clamp(query.Limit, 1, MaximumPageSize);
        string encodedQuery = Uri.EscapeDataString(query.Text);
        return $"{SearchEndpoint}?q={encodedQuery}&count={count}&offset=0&text_decorations=false&safesearch=moderate";
    }
}
