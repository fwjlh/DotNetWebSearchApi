using System.Net;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class GoogleSearchProviderTests
{
    [Fact]
    public void Parse_ReadsResultTitleUrlAndSnippet()
    {
        IReadOnlyList<SearchResult> results = GoogleSearchResultParser.Parse(ResultPage);

        SearchResult result = Assert.Single(results);
        Assert.Equal("Example & title", result.Title);
        Assert.Equal("https://example.com/article", result.Url);
        Assert.Equal("A useful Google snippet.", result.Snippet);
        Assert.Equal("google", result.Provider);
    }

    [Fact]
    public void Parse_UnwrapsGoogleRedirectUrl()
    {
        string html = ResultPage.Replace(
            "https://example.com/article",
            "https://www.google.com/url?q=https%3A%2F%2Fexample.com%2Fredirected&sa=U");

        IReadOnlyList<SearchResult> results = GoogleSearchResultParser.Parse(html);

        Assert.Equal("https://example.com/redirected", Assert.Single(results).Url);
    }

    [Fact]
    public void Parse_IgnoresIncompleteAndNonHttpResults()
    {
        const string html = """
            <div class="zMzFAb">
              <a class="fuLhoc" href="file:///tmp/result"><span class="CVA68e">Local</span></a>
            </div>
            <div class="zMzFAb">
              <a class="fuLhoc" href="https://example.com/no-title"> </a>
            </div>
            """;

        Assert.Empty(GoogleSearchResultParser.Parse(html));
    }

    [Fact]
    public void Parse_ReturnsEmptyWhenGoogleResultCardsAreMissing()
    {
        Assert.Empty(GoogleSearchResultParser.Parse("<html><body>Nothing found</body></html>"));
    }

    [Fact]
    public async Task SearchAsync_EncodesQueryAndParsesResponse()
    {
        Uri? requestedUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return HtmlResponse(ResultPage);
        }));
        GoogleSearchProvider provider = new(client);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("cats & dogs", 10),
            CancellationToken.None);

        Assert.Contains("q=cats%20%26%20dogs", requestedUri?.Query);
        Assert.Contains("adtest=on", requestedUri?.Query);
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_MapsLocaleSafeSearchTimeRangeAndPage()
    {
        Uri? requestedUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return HtmlResponse(ResultPage);
        }));
        GoogleSearchProvider provider = new(client);

        await provider.SearchAsync(new SearchQuery("bonjour", 10)
        {
            Language = "fr-FR",
            Page = 3,
            SafeSearch = 2,
            TimeRange = SearchTimeRange.Week
        }, CancellationToken.None);

        Assert.Contains("hl=fr", requestedUri?.Query);
        Assert.Contains("lr=lang_fr", requestedUri?.Query);
        Assert.Contains("cr=countryFR", requestedUri?.Query);
        Assert.Contains("start=20", requestedUri?.Query);
        Assert.Contains("tbs=qdr%3Aw", requestedUri?.Query);
        Assert.Contains("safe=high", requestedUri?.Query);
    }

    [Fact]
    public void Capabilities_DeclareImplementedGoogleRequestFeatures()
    {
        GoogleSearchProvider provider = new(new HttpClient());
        Assert.True(provider.Capabilities.SupportsPagination);
        Assert.Equal(50, provider.Capabilities.MaximumPage);
        Assert.True(provider.Capabilities.SupportsTimeRange);
        Assert.True(provider.Capabilities.SupportsSafeSearch);
        Assert.True(provider.Capabilities.SupportsLocale);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenGoogleReturnsCaptchaPage()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            HtmlResponse("<html><body>/sorry/ automated traffic detected</body></html>")));
        GoogleSearchProvider provider = new(client);

        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(() =>
            provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));
        Assert.Equal(ProviderFailureKind.Challenge, exception.FailureKind);
        Assert.Equal(ProviderChallengeKind.InteractiveCaptcha, exception.ChallengeKind);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenGoogleReturnsAnHttpError()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        GoogleSearchProvider provider = new(client);

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
        <html>
          <div class="zMzFAb">
            <a class="fuLhoc" href="https://example.com/article">
              <span class="CVA68e">Example &amp; title</span>
            </a>
            <span class="FrIlee"><span class="fYyStc">A useful Google snippet.</span></span>
          </div>
        </html>
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
