namespace DotNetWebSearchApi.Search;

public sealed class NaverSearchProvider : ISearchProvider, ISearchProviderCapabilities
{
    private const string SearchEndpoint = "https://search.naver.com/search.naver";
    private const int ResultsPerPage = 15;
    private static readonly IReadOnlyDictionary<SearchTimeRange, string> TimeRangeValues =
        new Dictionary<SearchTimeRange, string>
        {
            [SearchTimeRange.Day] = "1d",
            [SearchTimeRange.Week] = "1w",
            [SearchTimeRange.Month] = "1m",
            [SearchTimeRange.Year] = "1y"
        };
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public NaverSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "naver";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general", "web" },
        true,
        20,
        true,
        false,
        false);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUrl(query));
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        response.EnsureSuccessStatusCode(Name);
        return NaverSearchResultParser.Parse(response.Content);
    }

    private static string BuildRequestUrl(SearchQuery query)
    {
        List<KeyValuePair<string, string>> parameters = new()
        {
            new("query", query.Text),
            new("start", ((query.Page - 1) * ResultsPerPage + 1).ToString()),
            new("where", "web")
        };
        if (query.TimeRange.HasValue && TimeRangeValues.TryGetValue(query.TimeRange.Value, out string? timeRange))
        {
            parameters.Add(new KeyValuePair<string, string>("nso", $"p:{timeRange}"));
        }

        string queryString = string.Join("&", parameters.Select(parameter =>
            $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        return $"{SearchEndpoint}?{queryString}";
    }
}
