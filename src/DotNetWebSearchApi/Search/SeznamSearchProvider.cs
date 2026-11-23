namespace DotNetWebSearchApi.Search;

public sealed class SeznamSearchProvider : ISearchProvider, ISearchProviderCapabilities
{
    private const string SearchEndpoint = "https://search.seznam.cz/";
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public SeznamSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "seznam";

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general", "web" },
        false,
        1,
        false,
        false,
        false);

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        string homeHtml = await ReadHomePage(cancellationToken);
        IReadOnlyDictionary<string, string> hiddenFields = SeznamSearchResultParser.ReadHiddenFields(homeHtml);
        string requestUrl = BuildRequestUrl(query.Text, hiddenFields);

        using HttpRequestMessage request = new(HttpMethod.Get, requestUrl);
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        if (SeznamSearchResultParser.IsVerificationPage(response.RequestUri))
        {
            throw new ProviderFailureException(
                ProviderFailureKind.Challenge,
                "Seznam redirected the search to its browser-verification page.",
                challengeKind: ProviderChallengeKind.BrowserVerification);
        }

        response.EnsureSuccessStatusCode(Name);
        return SeznamSearchResultParser.Parse(response.Content);
    }

    private async Task<string> ReadHomePage(CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, SearchEndpoint);
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        response.EnsureSuccessStatusCode(Name);
        return response.Content;
    }

    private static string BuildRequestUrl(
        string query,
        IReadOnlyDictionary<string, string> hiddenFields)
    {
        Dictionary<string, string> fields = new(StringComparer.Ordinal)
        {
            ["q"] = query,
            ["oq"] = query,
            ["noredirect"] = "1"
        };

        foreach (KeyValuePair<string, string> field in hiddenFields)
        {
            fields[field.Key] = field.Value;
        }

        string queryString = string.Join("&", fields.Select(field =>
            $"{Uri.EscapeDataString(field.Key)}={Uri.EscapeDataString(field.Value)}"));
        return $"{SearchEndpoint}?{queryString}";
    }
}
