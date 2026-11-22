using System.Net;
using System.Text;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class YandexSearchProviderTests
{
    [Fact]
    public void Parse_ReadsBase64XmlResults()
    {
        IReadOnlyList<SearchResult> results = YandexSearchResultParser.Parse(EncodeXml(ResultXml));

        SearchResult result = Assert.Single(results);
        Assert.Equal("Yandex result", result.Title);
        Assert.Equal("https://example.com/page", result.Url);
        Assert.Equal("A useful Yandex snippet.", result.Snippet);
        Assert.Equal("yandex", result.Provider);
    }

    [Fact]
    public void Parse_UsesPassagesWhenHeadlineIsMissing()
    {
        const string xml = """
            <yandexsearch><response><results><grouping><group><doc>
              <url>https://example.com/page</url>
              <title>Passage result</title>
              <passages><passage>First passage</passage><passage>Second passage</passage></passages>
            </doc></group></grouping></results></response></yandexsearch>
            """;

        SearchResult result = Assert.Single(YandexSearchResultParser.Parse(EncodeXml(xml)));

        Assert.Equal("First passage Second passage", result.Snippet);
    }

    [Fact]
    public void Parse_ReturnsEmptyForNoResultsError()
    {
        const string xml = """<yandexsearch><response><error code="15">Nothing found</error></response></yandexsearch>""";

        Assert.Empty(YandexSearchResultParser.Parse(EncodeXml(xml)));
    }

    [Fact]
    public void Parse_ThrowsForOtherXmlErrors()
    {
        const string xml = """<yandexsearch><response><error code="99">Failed</error></response></yandexsearch>""";

        Assert.Throws<InvalidOperationException>(() => YandexSearchResultParser.Parse(EncodeXml(xml)));
    }

    [Fact]
    public void Parse_SkipsInvalidUrls()
    {
        const string xml = """
            <yandexsearch><response><results><grouping><group>
              <doc><url>file:///tmp/a</url><title>Local</title></doc>
              <doc><url>https://example.com</url><title>Valid</title></doc>
            </group></grouping></results></response></yandexsearch>
            """;

        Assert.Equal("Valid", Assert.Single(YandexSearchResultParser.Parse(EncodeXml(xml))).Title);
    }

    [Fact]
    public async Task SearchAsync_SendsFolderAndApiKeyAndParsesResponse()
    {
        string? authorization = null;
        string? body = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            authorization = request.Headers.Authorization?.ToString();
            body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonResponse(EncodeXml(ResultXml));
        }));
        YandexSearchProvider provider = new(client, new YandexSearchOptions("test-key", "folder-123"));

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("cats", 10),
            CancellationToken.None);

        Assert.Equal("Api-Key test-key", authorization);
        using JsonDocument requestBody = JsonDocument.Parse(body!);
        Assert.Equal("folder-123", requestBody.RootElement.GetProperty("folderId").GetString());
        Assert.Equal("cats", requestBody.RootElement.GetProperty("query").GetProperty("queryText").GetString());
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyWhenQueryExceedsApiLimit()
    {
        int requestCount = 0;
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
        {
            requestCount++;
            return JsonResponse(EncodeXml(ResultXml));
        }));
        YandexSearchProvider provider = new(client, new YandexSearchOptions("test-key", "folder-123"));

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery(new string('x', 401), 10),
            CancellationToken.None);

        Assert.Empty(results);
        Assert.Equal(0, requestCount);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenApiReturnsUnauthorized()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        YandexSearchProvider provider = new(client, new YandexSearchOptions("test-key", "folder-123"));

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));
    }

    private static string EncodeXml(string xml)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));
    }

    private static HttpResponseMessage JsonResponse(string rawData)
    {
        string json = JsonSerializer.Serialize(new { rawData });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
    }

    private const string ResultXml = """
        <yandexsearch><response><results><grouping><group><doc>
          <url>https://example.com/page</url>
          <title>Yandex result</title>
          <headline>A useful Yandex snippet.</headline>
        </doc></group></grouping></results></response></yandexsearch>
        """;

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
