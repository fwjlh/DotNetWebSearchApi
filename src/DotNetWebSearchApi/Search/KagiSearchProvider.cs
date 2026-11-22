using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net;

namespace DotNetWebSearchApi.Search;

public sealed record KagiSearchOptions(string ApiKey);

public sealed class KagiSearchProvider : ISearchProvider, ISearchProviderCapabilities
{
    private const string SearchEndpoint = "https://kagi.com/api/v1/search";
    private readonly HttpClient _httpClient;
    private readonly KagiSearchOptions _options;
    private readonly SearchHttpTransport _transport;
    private readonly TimeProvider _timeProvider;

    public KagiSearchProvider(
        HttpClient httpClient,
        KagiSearchOptions options,
        SearchHttpTransport? transport = null,
        TimeProvider? timeProvider = null)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException("A Kagi API key is required.", nameof(options));
        }

        _httpClient = httpClient;
        _options = options;
        _transport = transport ?? new SearchHttpTransport();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public string Name => "kagi";

    public TimeSpan Timeout => TimeSpan.FromSeconds(10);

    public EngineCapabilities Capabilities { get; } = new(
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general" },
        true,
        10,
        true,
        true,
        true);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = BuildRequest(query);
        SearchHttpResponse response = await _transport.SendAsync(
            _httpClient,
            request,
            cancellationToken,
            Name);
        response.EnsureSuccessStatusCode(Name);
        using JsonDocument document = JsonDocument.Parse(response.Content);
        return ParseResults(document.RootElement, query.Limit);
    }

    private HttpRequestMessage BuildRequest(SearchQuery query)
    {
        HttpRequestMessage request = new(HttpMethod.Post, SearchEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            query = query.Text,
            page = query.Page,
            workflow = "search",
            safe_search = query.SafeSearch == 0 ? false : true,
            filters = BuildFilters(query)
        });
        return request;
    }

    private Dictionary<string, string> BuildFilters(SearchQuery query)
    {
        Dictionary<string, string> filters = new(StringComparer.Ordinal)
        {
            ["region"] = GetRegion(query.Language)
        };
        if (query.TimeRange is { } timeRange)
        {
            int days = GetTimeRangeDays(timeRange);
            DateTimeOffset earliestDate = _timeProvider.GetUtcNow().AddDays(-days);
            filters["after"] = earliestDate.ToString("yyyy-MM-dd");
        }

        return filters;
    }

    private static string GetRegion(string language)
    {
        string[] parts = language.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 1 ? parts[^1].ToLowerInvariant() : "no_region";
    }

    private static int GetTimeRangeDays(SearchTimeRange timeRange)
    {
        return timeRange switch
        {
            SearchTimeRange.Day => 1,
            SearchTimeRange.Week => 7,
            SearchTimeRange.Month => 30,
            SearchTimeRange.Year => 365,
            _ => throw new ArgumentOutOfRangeException(nameof(timeRange))
        };
    }

    private IReadOnlyList<SearchResult> ParseResults(JsonElement root, int limit)
    {
        if (!root.TryGetProperty("data", out JsonElement data)
            || !data.TryGetProperty("search", out JsonElement results)
            || results.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<SearchResult>();
        }

        List<SearchResult> parsed = new();
        foreach (JsonElement item in results.EnumerateArray())
        {
            if (!TryReadResult(item, parsed.Count, out SearchResult? result) || result is null)
            {
                continue;
            }

            parsed.Add(result);
            if (parsed.Count >= limit)
            {
                break;
            }
        }

        return parsed;
    }

    private bool TryReadResult(JsonElement item, int position, out SearchResult? result)
    {
        result = null;
        if (!TryReadString(item, "url", out string url)
            || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        TryReadString(item, "title", out string title);
        TryReadString(item, "snippet", out string snippet);
        title = string.IsNullOrWhiteSpace(title) ? "No title available" : CleanText(title);
        result = new SearchResult(title, uri.AbsoluteUri, CleanText(snippet), Name, 1.0 / (position + 1));
        return true;
    }

    private static bool TryReadString(JsonElement item, string propertyName, out string value)
    {
        value = string.Empty;
        if (!item.TryGetProperty(propertyName, out JsonElement property)
            || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string CleanText(string value)
    {
        return WebUtility.HtmlDecode(Regex.Replace(value, "<[^>]+>", string.Empty)).Trim();
    }
}
