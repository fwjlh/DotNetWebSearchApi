using System.Net;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class DuckDuckGoSearchProviderTests
{
    [Fact]
    public void Parse_ReadsWebResultsAndIgnoresOtherCards()
    {
        IReadOnlyList<SearchResult> results = DuckDuckGoSearchResultParser.Parse(ResultPage);

        SearchResult result = Assert.Single(results);
        Assert.Equal("Example & result", result.Title);
        Assert.Equal("https://example.com/article", result.Url);
        Assert.Equal("A useful DuckDuckGo snippet.", result.Snippet);
        Assert.Equal("duckduckgo", result.Provider);
    }

    [Fact]
    public void Parse_UnwrapsDuckDuckGoRedirectUrl()
    {
        string html = ResultPage.Replace(
            "https://example.com/article",
            "https://duckduckgo.com/l/?uddg=https%3A%2F%2Fexample.com%2Fredirected&rut=abc");

        IReadOnlyList<SearchResult> results = DuckDuckGoSearchResultParser.Parse(html);

        Assert.Equal("https://example.com/redirected", Assert.Single(results).Url);
    }

    [Fact]
    public void Parse_DetectsCaptchaForm()
    {
        Assert.True(DuckDuckGoSearchResultParser.IsCaptchaPage(
            "<form id='challenge-form'><input name='captcha'></form>"));
    }

    [Fact]
    public void Parse_ReturnsFalseWhenCaptchaFormIsMissing()
    {
        Assert.False(DuckDuckGoSearchResultParser.IsCaptchaPage(ResultPage));
    }

    [Fact]
    public void Parse_IgnoresInvalidResultUrls()
    {
        const string html = """
            <div id="links">
              <div class="web-result">
                <h2><a href="file:///tmp/result">Local file</a></h2>
              </div>
            </div>
            """;

        Assert.Empty(DuckDuckGoSearchResultParser.Parse(html));
    }

    [Fact]
    public void Parse_ReturnsEmptyWhenResultContainerIsMissing()
    {
        Assert.Empty(DuckDuckGoSearchResultParser.Parse("<html><body>Empty</body></html>"));
    }

    [Fact]
    public async Task SearchAsync_PostsEncodedFirstPageForm()
    {
        Uri? requestedUri = null;
        string? formData = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            formData = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return HtmlResponse(ResultPage);
        }));
        DuckDuckGoSearchProvider provider = new(client);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("cats & dogs", 10),
            CancellationToken.None);

        Assert.Equal("https://html.duckduckgo.com/html/", requestedUri?.AbsoluteUri);
        Assert.Equal("q=cats+%26+dogs&b=&kl=wt-wt", formData);
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyWhenQueryIsTooLong()
    {
        int requestCount = 0;
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
        {
            requestCount++;
            return HtmlResponse(ResultPage);
        }));
        DuckDuckGoSearchProvider provider = new(client);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery(new string('x', 500), 10),
            CancellationToken.None);

        Assert.Empty(results);
        Assert.Equal(0, requestCount);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenDuckDuckGoReturnsCaptcha()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            HtmlResponse("<form id='challenge-form'>Verify you are human</form>")));
        DuckDuckGoSearchProvider provider = new(client);

        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(() =>
            provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));
        Assert.Equal(ProviderFailureKind.Challenge, exception.FailureKind);
        Assert.Equal(ProviderChallengeKind.InteractiveCaptcha, exception.ChallengeKind);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyForSeeOtherResponse()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.SeeOther)));
        DuckDuckGoSearchProvider provider = new(client);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenDuckDuckGoReturnsAnHttpError()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        DuckDuckGoSearchProvider provider = new(client);

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
          <div id="links">
            <div class="web-result">
              <h2><a href="https://example.com/article">Example &amp; result</a></h2>
              <a class="result__snippet">A useful DuckDuckGo snippet.</a>
            </div>
            <div class="result--ad result--ad--small">
              <h2><a href="https://advert.example.com">Advertisement</a></h2>
            </div>
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
