namespace DotNetWebSearchApi.Search;

public sealed class SeznamSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://search.seznam.cz/";
    private readonly HttpClient _httpClient;

    public SeznamSearchProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "seznam";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        string homeHtml = await ReadHomePage(cancellationToken);
        IReadOnlyDictionary<string, string> hiddenFields = SeznamSearchResultParser.ReadHiddenFields(homeHtml);
        string requestUrl = BuildRequestUrl(query.Text, hiddenFields);

        using HttpResponseMessage response = await _httpClient.GetAsync(requestUrl, cancellationToken);
        string html = await response.Content.ReadAsStringAsync(cancellationToken);
        if (SeznamSearchResultParser.IsVerificationPage(response.RequestMessage?.RequestUri))
        {
            throw new HttpRequestException("Seznam redirected the search to its verification page.");
        }

        response.EnsureSuccessStatusCode();
        return SeznamSearchResultParser.Parse(html);
    }

    private async Task<string> ReadHomePage(CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(SearchEndpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
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
