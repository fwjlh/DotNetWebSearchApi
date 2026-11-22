using System.Net;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class YahooSearchProviderTests
{
    [Fact]
    public void Parse_ReadsResultTitleUrlAndSnippet()
    {
        SearchResult result = Assert.Single(YahooSearchResultParser.Parse(ResultPage));
        Assert.Equal("Example Yahoo result", result.Title);
        Assert.Equal("https://example.com/article", result.Url);
        Assert.Equal("Yahoo result description.", result.Snippet);
        Assert.Equal("yahoo", result.Provider);
    }

    [Fact]
    public void Parse_UnwrapsYahooTrackingUrl()
    {
        string html = ResultPage.Replace(
            "https://example.com/article",
            "https://r.search.yahoo.com/_ylt=abc/RU=https%3A%2F%2Fexample.com%2Fredirected/RK=2/RS=abc");

        SearchResult result = Assert.Single(YahooSearchResultParser.Parse(html));

        Assert.Equal("https://example.com/redirected", result.Url);
    }

    [Fact]
    public void Parse_IgnoresInvalidAndIncompleteResults()
    {
        const string html = """
            <div class="algo-sr">
              <div class="compTitle"><a href="file:///tmp/result"><h3>Local</h3></a></div>
            </div>
            <div class="algo-sr">
              <div class="compTitle"><a href="https://example.com/no-title"></a></div>
            </div>
            """;

        Assert.Empty(YahooSearchResultParser.Parse(html));
    }

    [Fact]
    public void Parse_ReturnsEmptyWhenNoResultCardsExist()
    {
        Assert.Empty(YahooSearchResultParser.Parse("<html><body>No results</body></html>"));
    }

    [Fact]
    public async Task SearchAsync_UsesYahooSearchUrlAndSearchCookie()
    {
        Uri? requestedUri = null;
        CookieContainer cookies = new();
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return HtmlResponse(ResultPage);
        }));
        YahooSearchProvider provider = new(client, cookies);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("cats & dogs", 10),
            CancellationToken.None);

        Assert.Equal("https://search.yahoo.com/search?p=cats%20%26%20dogs&iscqry=", requestedUri?.AbsoluteUri);
        Assert.Contains("sB=", cookies.GetCookieHeader(new Uri("https://search.yahoo.com/")));
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_MapsLocalePageTimeRangeAndSafeSearch()
    {
        Uri? requestedUri = null;
        CookieContainer cookies = new();
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return HtmlResponse(ResultPage);
        }));
        YahooSearchProvider provider = new(client, cookies);

        await provider.SearchAsync(new SearchQuery("cats", 10)
        {
            Language = "en-GB",
            Page = 2,
            TimeRange = SearchTimeRange.Week,
            SafeSearch = 2
        }, CancellationToken.None);

        Assert.Equal("uk.search.yahoo.com", requestedUri?.Host);
        Assert.Contains("btf=w", requestedUri?.Query);
        Assert.Contains("b=15", requestedUri?.Query);
        Assert.Contains("pz=7", requestedUri?.Query);
        string cookie = cookies.GetCookieHeader(new Uri("https://search.yahoo.com/"));
        Assert.Contains("vm=r", cookie);
        Assert.Contains("vl=lang_en", cookie);
    }

    [Fact]
    public async Task SearchAsync_CachesYbvCookieForTwentyFourHours()
    {
        int requests = 0;
        CookieContainer cookies = new();
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
        {
            requests++;
            if (requests == 1)
            {
                HttpResponseMessage redirect = new(HttpStatusCode.Redirect)
                {
                    Headers = { Location = new Uri("https://search.yahoo.com/tracking.gif") }
                };
                redirect.Headers.TryAddWithoutValidation("Set-Cookie", "YBV=v0.2-test-token; Domain=.yahoo.com; Path=/");
                return redirect;
            }

            return HtmlResponse(ResultPage);
        }));
        YahooSearchProvider provider = new(client, cookies);

        await provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None);

        Cookie? ybv = cookies.GetCookies(new Uri("https://search.yahoo.com/"))["YBV"];
        Assert.NotNull(ybv);
        Assert.StartsWith("v0.2", ybv!.Value);
        Assert.True(ybv.Expires > DateTime.UtcNow.AddHours(23));
    }

    [Fact]
    public void Capabilities_DeclareOnlyYahooSupportedTimeRanges()
    {
        YahooSearchProvider provider = new(new HttpClient(), new CookieContainer());
        Assert.True(provider.Capabilities.SupportsPagination);
        Assert.True(provider.Capabilities.SupportsSafeSearch);
        Assert.True(provider.Capabilities.SupportsLocale);
        Assert.True(provider.Capabilities.Supports(new SearchQuery("test", 10) { TimeRange = SearchTimeRange.Month }));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 10) { TimeRange = SearchTimeRange.Year }));
    }

    [Fact]
    public async Task SearchAsync_FollowsYahooTrackingRedirect()
    {
        int requests = 0;
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
        {
            requests++;
            return requests == 1
                ? new HttpResponseMessage(HttpStatusCode.Redirect)
                {
                    Headers = { Location = new Uri("https://search.yahoo.com/tracking.gif") }
                }
                : HtmlResponse(ResultPage);
        }));
        YahooSearchProvider provider = new(client, new CookieContainer());

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Equal(2, requests);
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenYahooReturnsAnHttpError()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        YahooSearchProvider provider = new(client, new CookieContainer());

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
        <div class="algo-sr">
          <div class="compTitle">
            <a href="https://example.com/article" aria-label="Example Yahoo result">
              <h3>Example Yahoo result</h3>
            </a>
          </div>
          <div class="compText">Yahoo result description.</div>
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
