using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DotNetWebSearchApi.Search;

public sealed partial class WikipediaSearchProvider : ISearchProvider, ISearchProviderCapabilities
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

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general" },
        false,
        1,
        false,
        false,
        true);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        string wikiHost = GetWikiHost(query.Language);
        using HttpRequestMessage request = new(HttpMethod.Get, BuildRequestUrl(query.Text, wikiHost));
        AddLanguagePreferences(request, query.Language);
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        response.EnsureSuccessStatusCode(Name);
        using JsonDocument document = JsonDocument.Parse(response.Content);

        return ReadResults(document.RootElement, wikiHost);
    }

    private static string GetWikiHost(string language)
    {
        string engineLanguage = EngineLocaleRegistry.GetRegion("wikipedia", language)
            ?? EngineLocaleRegistry.GetLanguage("wikipedia", language, "en")
            ?? "en";
        return EngineLocaleRegistry.GetCustom(
            "wikipedia",
            "wiki_netloc",
            engineLanguage,
            "en.wikipedia.org")!;
    }

    private static string BuildRequestUrl(string query, string wikiHost)
    {
        string encodedQuery = Uri.EscapeDataString(query);
        return $"https://{wikiHost}/w/api.php?action=query&list=search&format=json&utf8=1&srsearch={encodedQuery}";
    }

    private static void AddLanguagePreferences(HttpRequestMessage request, string language)
    {
        if (string.IsNullOrWhiteSpace(language)
            || language.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string languageOnly = language.Split('-', 2)[0];
        request.Headers.AcceptLanguage.ParseAdd(language);
        if (!languageOnly.Equals(language, StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.AcceptLanguage.ParseAdd($"{languageOnly};q=0.9");
        }
    }

    private static IReadOnlyList<SearchResult> ReadResults(JsonElement root, string wikiHost)
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
            if (!TryReadResult(item, position, wikiHost, out SearchResult? result) || result is null)
            {
                continue;
            }

            results.Add(result);
            position++;
        }

        return results;
    }

    private static bool TryReadResult(
        JsonElement item,
        int position,
        string wikiHost,
        out SearchResult? result)
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
            $"https://{wikiHost}/?curid={pageId}",
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
