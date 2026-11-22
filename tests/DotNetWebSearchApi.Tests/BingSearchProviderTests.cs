using System.Net;
using System.Text;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class BingSearchProviderTests
{
    [Fact]
    public void Parse_ReturnsResultTitleUrlAndCleanSnippet()
    {
        IReadOnlyList<SearchResult> results = BingSearchResultParser.Parse(ResultPage);

        SearchResult result = Assert.Single(results);
        Assert.Equal("Example & title", result.Title);
        Assert.Equal("https://example.com/article", result.Url);
        Assert.Equal("A useful result description.", result.Snippet);
        Assert.Equal("bing", result.Provider);
    }

    [Fact]
    public void Parse_UnwrapsBingTrackingUrl()
    {
        string target = "https://example.com/redirected";
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(target))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        string html = ResultPage.Replace(
            "https://example.com/article",
            $"https://www.bing.com/ck/a?u=a1{encoded}");

        IReadOnlyList<SearchResult> results = BingSearchResultParser.Parse(html);

        Assert.Equal(target, Assert.Single(results).Url);
    }

    [Fact]
    public void Parse_IgnoresIncompleteAndNonHttpResults()
    {
        const string html = """
            <ol id="b_results">
              <li class="b_algo"><h2><a href="file:///tmp/a">Local file</a></h2></li>
              <li class="b_algo"><p>Missing title link</p></li>
            </ol>
            """;

        Assert.Empty(BingSearchResultParser.Parse(html));
    }

    [Fact]
    public void Parse_ReturnsEmptyWhenBingResultContainerIsMissing()
    {
        Assert.Empty(BingSearchResultParser.Parse("<html><body>Unexpected response</body></html>"));
    }

    [Fact]
    public async Task SearchAsync_EncodesQueryAndParsesResponse()
    {
        Uri? requestedUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ResultPage)
            };
        }));
        BingSearchProvider provider = new(client);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("cats & dogs", 10),
            CancellationToken.None);

        Assert.Equal("https://www.bing.com/search?q=cats%20%26%20dogs&adlt=off", requestedUri?.AbsoluteUri);
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenBingReturnsAnHttpError()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        BingSearchProvider provider = new(client);

        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(() =>
            provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None));
        Assert.Equal(ProviderFailureKind.RateLimited, exception.FailureKind);
    }

    private const string ResultPage = """
        <html>
          <ol id="b_results">
            <li class="b_algo">
              <h2><a href="https://example.com/article">Example &amp; title</a></h2>
              <p>A useful <span class="algoSlug_icon">decorative badge</span> result description.</p>
            </li>
          </ol>
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
