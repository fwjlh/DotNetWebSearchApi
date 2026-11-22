using System.Net;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class QwantSearchProviderTests
{
    [Fact]
    public void Parse_ReadsWebResultsAndIgnoresOtherResultTypes()
    {
        using JsonDocument document = JsonDocument.Parse(SearchResponse);
        SearchResult result = Assert.Single(QwantSearchResultParser.Parse(document.RootElement));
        Assert.Equal("First Qwant result", result.Title);
        Assert.Equal("https://example.com/first", result.Url);
        Assert.Equal("First description", result.Snippet);
        Assert.Equal("qwant", result.Provider);
    }

    [Fact]
    public void Parse_SkipsInvalidResultUrls()
    {
        using JsonDocument document = JsonDocument.Parse("""
            {"status":"success","data":{"result":{"items":{"mainline":[{"type":"web","items":[
              {"title":"Local","url":"file:///tmp/result"},
              {"title":"Valid","url":"https://example.com"}
            ]}]}}}}
            """);
        Assert.Equal("Valid", Assert.Single(QwantSearchResultParser.Parse(document.RootElement)).Title);
    }

    [Fact]
    public void Parse_ReturnsEmptyWhenMainlineIsMissing()
    {
        using JsonDocument document = JsonDocument.Parse("""{"status":"success","data":{}}""");
        Assert.Empty(QwantSearchResultParser.Parse(document.RootElement));
    }

    [Fact]
    public void Parse_ThrowsWhenApiReportsFailure()
    {
        using JsonDocument document = JsonDocument.Parse("""{"status":"error","data":{"error_code":24}}""");
        Assert.Throws<InvalidOperationException>(() => QwantSearchResultParser.Parse(document.RootElement));
    }

    [Fact]
    public async Task SearchAsync_EncodesQueryAndUsesQwantWebEndpoint()
    {
        Uri? requestedUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return JsonResponse(SearchResponse);
        }));
        QwantSearchProvider provider = new(client);
        IReadOnlyList<SearchResult> results = await provider.SearchAsync(new SearchQuery("cats & dogs", 10), CancellationToken.None);
        Assert.Equal("https://api.qwant.com/v3/search/web?t=web&q=cats%20%26%20dogs&count=10&locale=en_US&offset=0&tgp=1&device=desktop&safesearch=0", requestedUri?.AbsoluteUri);
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_MapsLocalePageAndSafeSearch()
    {
        Uri? requestedUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return JsonResponse(SearchResponse);
        }));
        QwantSearchProvider provider = new(client);

        await provider.SearchAsync(new SearchQuery("bonjour", 10)
        {
            Language = "fr-FR",
            Page = 3,
            SafeSearch = 2
        }, CancellationToken.None);

        Assert.Contains("locale=fr_FR", requestedUri?.Query);
        Assert.Contains("offset=20", requestedUri?.Query);
        Assert.Contains("safesearch=2", requestedUri?.Query);
    }

    [Fact]
    public void Capabilities_DeclareOnlyImplementedQwantFeatures()
    {
        QwantSearchProvider provider = new(new HttpClient());
        Assert.True(provider.Capabilities.SupportsPagination);
        Assert.Equal(5, provider.Capabilities.MaximumPage);
        Assert.True(provider.Capabilities.SupportsSafeSearch);
        Assert.True(provider.Capabilities.SupportsLocale);
        Assert.False(provider.Capabilities.SupportsTimeRange);
    }

    [Fact]
    public async Task SearchAsync_ClassifiesApiRateLimitCode()
    {
        const string error = """{"status":"error","data":{"error_code":24}}""";
        using HttpClient client = new(new StubHttpMessageHandler(_ => JsonResponse(error)));
        QwantSearchProvider provider = new(client);

        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(
            () => provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));

        Assert.Equal(ProviderFailureKind.RateLimited, exception.FailureKind);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenQwantDeniesRequest()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)));
        QwantSearchProvider provider = new(client);
        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(
            () => provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));
        Assert.Equal(ProviderFailureKind.AccessDenied, exception.FailureKind);
    }

    [Fact]
    public async Task SearchAsync_ClassifiesDataDomeForbiddenPageAsChallenge()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent("DataDome captcha verification")
            }));
        QwantSearchProvider provider = new(client);

        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(
            () => provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));

        Assert.Equal(ProviderFailureKind.Challenge, exception.FailureKind);
        Assert.Equal(ProviderChallengeKind.BrowserVerification, exception.ChallengeKind);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenQwantReturnsAnHttpError()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        QwantSearchProvider provider = new(client);
        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(
            () => provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));
        Assert.Equal(ProviderFailureKind.TransientHttp, exception.FailureKind);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenResponseJsonIsMalformed()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("not-json") }));
        QwantSearchProvider provider = new(client);
        await Assert.ThrowsAnyAsync<JsonException>(() => provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json) };

    private const string SearchResponse = """
        {"status":"success","data":{"result":{"items":{"mainline":[
          {"type":"web","items":[{"title":"First Qwant result","url":"https://example.com/first","desc":"First description"}]},
          {"type":"ads","items":[{"title":"Sponsored result","url":"https://ads.example.com"}]}
        ]}}}}
        """;

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }
}
