using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNetWebSearchApi.Search;

public sealed partial class WikipediaSearchProvider : ISearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly SearchHttpTransport _transport;

    public WikipediaSearchProvider(HttpClient httpClient, SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "wikipedia";

    public TimeSpan Timeout => TimeSpan.FromSeconds(5);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUrl(query.Text));
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        response.EnsureSuccessStatusCode(Name);
        using JsonDocument document = JsonDocument.Parse(response.Content);

        return ReadResults(document.RootElement);
    }

    private static string BuildRequestUrl(string query)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return "https://en.wikipedia.org/w/api.php?action=query&list=search&format=json&utf8=1&srsearch="
            + encodedQuery;
    }

    private static IReadOnlyList<SearchResult> ReadResults(JsonElement root)
    {
        if (!root.TryGetProperty("query", out JsonElement queryElement)
            || !queryElement.TryGetProperty("search", out JsonElement searchElement)
            || searchElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<SearchResult>();
        }

        List<SearchResult> results = new();
        int position = 0;
        foreach (JsonElement item in searchElement.EnumerateArray())
        {
            if (!TryReadResult(item, position, out SearchResult? result) || result is null)
            {
                continue;
            }

            results.Add(result);
            position++;
        }

        return results;
    }

    private static bool TryReadResult(JsonElement item, int position, out SearchResult? result)
    {
        result = null;
        if (!item.TryGetProperty("title", out JsonElement titleElement)
            || !item.TryGetProperty("pageid", out JsonElement pageIdElement)
            || titleElement.ValueKind != JsonValueKind.String
            || !pageIdElement.TryGetInt64(out long pageId))
        {
            return false;
        }

        string title = titleElement.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            return false;
        }

        string snippet = item.TryGetProperty("snippet", out JsonElement snippetElement)
            && snippetElement.ValueKind == JsonValueKind.String
                ? CleanSnippet(snippetElement.GetString() ?? string.Empty)
                : string.Empty;

        result = new SearchResult(
            title,
            $"https://en.wikipedia.org/?curid={pageId}",
            snippet,
            "wikipedia",
            1.0 / (position + 1));

        return true;
    }

    private static string CleanSnippet(string value)
    {
        string withoutTags = HtmlTagRegex().Replace(value, string.Empty);
        return WebUtility.HtmlDecode(withoutTags).Trim();
    }

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();
}
