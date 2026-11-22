namespace DotNetWebSearchApi.Search;

public sealed class BingSearchProvider : ISearchProvider, ISearchProviderCapabilities
{
    private const string SearchEndpoint = "https://www.bing.com/search";
    private static readonly IReadOnlyDictionary<string, string> SafeSearchValues =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["0"] = "off",
            ["1"] = "moderate",
            ["2"] = "strict"
        };
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public BingSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "bing";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general", "web" },
        false,
        1,
        false,
        true,
        true);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUrl(query));
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        response.EnsureSuccessStatusCode(Name);
        return BingSearchResultParser.Parse(response.Content);
    }

    private static string BuildRequestUrl(SearchQuery query)
    {
        List<string> parameters = new()
        {
            $"q={Uri.EscapeDataString(query.Text)}",
            $"adlt={SafeSearchValues[query.SafeSearch.ToString()]}"
        };
        string? market = EngineLocaleRegistry.GetRegion("bing", query.Language);
        if (!string.IsNullOrWhiteSpace(market) && !market.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            string[] parts = market.Split('-', 2);
            parameters.Add($"setlang={Uri.EscapeDataString(parts[0])}");
            if (parts.Length == 2 && parts[1] is not "US" and not "CN" and not "RU")
            {
                parameters.Add($"cc={Uri.EscapeDataString(parts[1].ToLowerInvariant())}");
            }
        }

        return $"{SearchEndpoint}?{string.Join("&", parameters)}";
    }
}
