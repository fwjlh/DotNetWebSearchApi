using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DotNetWebSearchApi.Search;

public sealed class YandexSearchProvider : ISearchProvider
{
    private const string SearchEndpoint = "https://searchapi.api.cloud.yandex.net/v2/web/search";
    private const int MaximumQueryLength = 400;
    private readonly HttpClient _httpClient;
    private readonly YandexSearchOptions _options;
    private readonly SearchHttpTransport _transport;

    public YandexSearchProvider(
        HttpClient httpClient,
        YandexSearchOptions options,
        SearchHttpTransport? transport = null)
    {
        _httpClient = httpClient;
        _options = options;
        _transport = transport ?? new SearchHttpTransport();
    }

    public string Name => "yandex";

    public TimeSpan Timeout => TimeSpan.FromSeconds(8);

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Text.Length > MaximumQueryLength)
        {
            return Array.Empty<SearchResult>();
        }

        using HttpRequestMessage request = BuildRequest(query);
        SearchHttpResponse response = await _transport.SendAsync(_httpClient, request, cancellationToken);
        if (response.TryGetHeader("x-yandex-captcha", out IReadOnlyList<string>? challengeHeaders)
            && challengeHeaders?.Any(value => value.Equals("captcha", StringComparison.OrdinalIgnoreCase)) == true)
        {
            throw new ProviderFailureException(
                ProviderFailureKind.Challenge,
                "Yandex requires interactive CAPTCHA verification.",
                challengeKind: ProviderChallengeKind.InteractiveCaptcha);
        }

        response.EnsureSuccessStatusCode(Name);
        using JsonDocument document = JsonDocument.Parse(response.Content);

        string rawData = ReadRawData(document.RootElement);
        return YandexSearchResultParser.Parse(rawData);
    }

    private HttpRequestMessage BuildRequest(SearchQuery query)
    {
        int pageSize = Math.Clamp(query.Limit, 1, 100);
        object body = new
        {
            query = new
            {
                searchType = "SEARCH_TYPE_COM",
                queryText = query.Text,
                familyMode = "FAMILY_MODE_MODERATE",
                page = "0"
            },
            groupSpec = new
            {
                groupMode = "GROUP_MODE_FLAT",
                groupsOnPage = pageSize.ToString(),
                docsInGroup = "1"
            },
            l10n = "LOCALIZATION_EN",
            folderId = _options.FolderId,
            responseFormat = "FORMAT_XML"
        };

        HttpRequestMessage request = new(HttpMethod.Post, SearchEndpoint)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Api-Key", _options.ApiKey);
        return request;
    }

    private static string ReadRawData(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("rawData", out JsonElement rawData)
            || rawData.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(rawData.GetString()))
        {
            throw new InvalidOperationException("Yandex Search API response did not contain rawData.");
        }

        return rawData.GetString()!;
    }
}
