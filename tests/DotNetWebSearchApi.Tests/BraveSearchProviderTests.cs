using System.Net;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class BraveSearchProviderTests
{
    [Fact]
    public void Parse_ReadsWebResultsAndAssignsDescendingScores()
    {
        using JsonDocument document = JsonDocument.Parse(SearchResponse);
        IReadOnlyList<SearchResult> results = BraveSearchResultParser.Parse(document.RootElement);

        Assert.Equal(2, results.Count);
        Assert.Equal("First result", results[0].Title);
        Assert.Equal("https://example.com/first", results[0].Url);
        Assert.Equal("First description", results[0].Snippet);
        Assert.Equal("brave", results[0].Provider);
        Assert.True(results[0].Score > results[1].Score);
    }

    [Fact]
    public void Parse_SkipsMissingFieldsAndNonHttpUrls()
    {
        using JsonDocument document = JsonDocument.Parse("""
            {
              "web": {
                "results": [
                  { "title": "No URL" },
                  { "title": "Local", "url": "file:///tmp/result" },
                  { "title": "Valid", "url": "https://example.com" }
                ]
              }
            }
            """);

        IReadOnlyList<SearchResult> results = BraveSearchResultParser.Parse(document.RootElement);

        Assert.Equal("Valid", Assert.Single(results).Title);
    }

    [Fact]
    public void Parse_ReturnsEmptyWhenWebResultsAreMissing()
    {
        using JsonDocument document = JsonDocument.Parse("""{"query":{"original":"test"}}""");

        Assert.Empty(BraveSearchResultParser.Parse(document.RootElement));
    }

    [Fact]
    public void Parse_ReturnsEmptyForUnexpectedRootShape()
    {
        using JsonDocument document = JsonDocument.Parse("[]");

        Assert.Empty(BraveSearchResultParser.Parse(document.RootElement));
    }

    [Fact]
    public async Task SearchAsync_EncodesQueryAndUsesRequestedPageSize()
    {
        Uri? requestedUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return JsonResponse(SearchResponse);
        }));
        BraveSearchProvider provider = new(client);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("cats & dogs", 5),
            CancellationToken.None);

        Assert.Equal(
            "https://api.search.brave.com/res/v1/web/search?q=cats%20%26%20dogs&count=5&offset=0&text_decorations=false&safesearch=off",
            requestedUri?.AbsoluteUri);
        Assert.Equal(2, results.Count);
    }

    [Theory]
    [InlineData(0, "off")]
    [InlineData(1, "moderate")]
    [InlineData(2, "strict")]
    public async Task SearchAsync_MapsSafeSearchLevel(int safeSearch, string expectedValue)
    {
        Uri? requestedUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return JsonResponse(SearchResponse);
        }));
        BraveSearchProvider provider = new(client);

        await provider.SearchAsync(
            new SearchQuery("test", 10) { SafeSearch = safeSearch },
            CancellationToken.None);

        Assert.Contains("safesearch=" + expectedValue, requestedUri?.Query);
    }

    [Fact]
    public void Capabilities_DeclareSupportedRequestFeatures()
    {
        BraveSearchProvider provider = new(new HttpClient());

        Assert.True(provider.Capabilities.Supports(new SearchQuery("test", 10) { SafeSearch = 2 }));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 10) { Page = 2 }));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 10) { TimeRange = SearchTimeRange.Day }));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 10) { Language = "en-GB" }));
    }

    [Fact]
    public async Task SearchAsync_ClampsPageSizeToApiMaximum()
    {
        Uri? requestedUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return JsonResponse(SearchResponse);
        }));
        BraveSearchProvider provider = new(client);

        await provider.SearchAsync(new SearchQuery("test", 50), CancellationToken.None);

        Assert.Contains("count=20", requestedUri?.Query);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenBraveReturnsAnHttpError()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        BraveSearchProvider provider = new(client);

        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(() =>
            provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));
        Assert.Equal(ProviderFailureKind.RateLimited, exception.FailureKind);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenResponseJsonIsMalformed()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json")
            }));
        BraveSearchProvider provider = new(client);

        await Assert.ThrowsAnyAsync<JsonException>(() =>
            provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };
    }

    private const string SearchResponse = """
        {
          "web": {
            "results": [
              {
                "title": "First result",
                "url": "https://example.com/first",
                "description": "First description"
              },
              {
                "title": "Second result",
                "url": "https://example.com/second",
                "description": "Second description"
              }
            ]
          }
        }
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
