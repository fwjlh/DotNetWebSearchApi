using System.Net;
using System.Text.Json;

namespace DotNetWebSearchApi.Search;

public sealed class QwantSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://api.qwant.com/v3/search/web";
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public QwantSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "qwant";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUrl(query.Text));
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            ProviderFailureKind kind = response.Content.Contains("captcha", StringComparison.OrdinalIgnoreCase)
                || response.Content.Contains("datadome", StringComparison.OrdinalIgnoreCase)
                ? ProviderFailureKind.Challenge
                : ProviderFailureKind.AccessDenied;
            throw new ProviderFailureException(
                kind,
                "Qwant denied the request; a DataDome session or browser verification may be required.",
                response.RetryAfter);
        }

        response.EnsureSuccessStatusCode(Name);
        using JsonDocument document = JsonDocument.Parse(response.Content);
        return QwantSearchResultParser.Parse(document.RootElement);
    }

    private static string BuildRequestUrl(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return $"{SearchEndpoint}?t=web&q={encodedQuery}&count=10&locale=en_GB&offset=0&tgp=1&device=desktop&safesearch=1";
    }
}
