using System.Net;
using System.Text.Json;

namespace DotNetWebSearchApi.Search;

public sealed class QwantSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://api.qwant.com/v3/search/web";
    private readonly HttpClient _httpClient;

    public QwantSearchProvider(HttpClient httpClient) { _httpClient = httpClient; }
    public string Name => "qwant";
    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(BuildRequestUrl(query.Text), cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new HttpRequestException("Qwant denied the request; it may require a valid DataDome session.");
        response.EnsureSuccessStatusCode();
        await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        return QwantSearchResultParser.Parse(document.RootElement);
    }

    private static string BuildRequestUrl(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return $"{SearchEndpoint}?t=web&q={encodedQuery}&count=10&locale=en_GB&offset=0&tgp=1&device=desktop&safesearch=1";
    }
}
