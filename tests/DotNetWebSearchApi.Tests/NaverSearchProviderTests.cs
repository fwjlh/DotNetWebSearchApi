using System.Net;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class NaverSearchProviderTests
{
    [Fact]
    public void Parse_ReadsGeneralWebResult()
    {
        SearchResult result = Assert.Single(NaverSearchResultParser.Parse(ResultPage));
        Assert.Equal("Naver result & title", result.Title);
        Assert.Equal("https://example.com/article", result.Url);
        Assert.Equal("Naver result description.", result.Snippet);
        Assert.Equal("naver", result.Provider);
    }

    [Fact]
    public void Parse_SkipsInvalidAndKeepNaverLinks()
    {
        const string html = """
            <div class="fds-web-normal-doc-root">
              <span class="sds-comps-text-type-headline1">Keep Naver</span>
              <a href="https://keep.naver.com/keep">Keep</a>
            </div>
            <div class="fds-web-normal-doc-root">
              <span class="sds-comps-text-type-headline1">Local file</span>
              <a href="file:///tmp/a">Local</a>
            </div>
            """;

        Assert.Empty(NaverSearchResultParser.Parse(html));
    }

    [Fact]
    public void Parse_ReturnsEmptyWhenNoResultCardsExist()
    {
        Assert.Empty(NaverSearchResultParser.Parse("<html><body>Nothing found</body></html>"));
    }

    [Fact]
    public async Task SearchAsync_EncodesQueryAndUsesGeneralWebMode()
    {
        Uri? requestedUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return HtmlResponse(ResultPage);
        }));
        NaverSearchProvider provider = new(client);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("cats & dogs", 10),
            CancellationToken.None);

        Assert.Equal(
            "https://search.naver.com/search.naver?query=cats%20%26%20dogs&start=1&where=web",
            requestedUri?.AbsoluteUri);
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_MapsPageAndTimeRange()
    {
        Uri? requestedUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return HtmlResponse(ResultPage);
        }));
        NaverSearchProvider provider = new(client);

        await provider.SearchAsync(new SearchQuery("cats", 10)
        {
            Page = 3,
            TimeRange = SearchTimeRange.Month
        }, CancellationToken.None);

        Assert.Contains("start=31", requestedUri?.Query);
        Assert.Contains("where=web", requestedUri?.Query);
        Assert.Contains("nso=p%3A1m", requestedUri?.Query);
    }

    [Fact]
    public void Capabilities_DeclareNaverPagingAndTimeRangeOnly()
    {
        NaverSearchProvider provider = new(new HttpClient());
        Assert.True(provider.Capabilities.SupportsPagination);
        Assert.True(provider.Capabilities.SupportsTimeRange);
        Assert.False(provider.Capabilities.SupportsSafeSearch);
        Assert.False(provider.Capabilities.SupportsLocale);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenNaverReturnsAnHttpError()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        NaverSearchProvider provider = new(client);

        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(() =>
            provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));
        Assert.Equal(ProviderFailureKind.TransientHttp, exception.FailureKind);
    }

    private static HttpResponseMessage HtmlResponse(string html)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(html)
        };
    }

    private const string ResultPage = """
        <div class="fds-web-normal-doc-root">
          <span class="sds-comps-text-type-headline1">Naver result &amp; title</span>
          <a href="https://example.com/article">Open result</a>
          <span class="sds-comps-text-type-body1">Naver result description.</span>
        </div>
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
