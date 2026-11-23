using System.Text.Json;

namespace DotNetWebSearchApi.Search;

public sealed class BraveSearchProvider : ISearchProvider, ISearchProviderCapabilities
{
    private const string SearchEndpoint = "https://api.search.brave.com/res/v1/web/search";
    private const int MaximumPageSize = 20;
    private static readonly IReadOnlyDictionary<int, string> SafeSearchValues =
        new Dictionary<int, string>
        {
            [0] = "off",
            [1] = "moderate",
            [2] = "strict"
        };
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public BraveSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "brave";

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general", "web" },
        false,
        1,
        false,
        true,
        false);

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
        return $"{SearchEndpoint}?q={encodedQuery}&count={count}&offset=0&text_decorations=false&safesearch={SafeSearchValues[query.SafeSearch]}";
    }
}
