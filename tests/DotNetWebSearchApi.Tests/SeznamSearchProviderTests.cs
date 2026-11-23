using System.Net;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class SeznamSearchProviderTests
{
    [Fact]
    public void Capabilities_DeclareGeneralWebOnly()
    {
        using HttpClient client = new();
        SeznamSearchProvider provider = new(client);

        Assert.True(provider.Capabilities.Supports(new SearchQuery("test", 5)
        {
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general" }
        }));
        Assert.True(provider.Capabilities.Supports(new SearchQuery("test", 5)
        {
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "web" }
        }));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 5)
        {
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "images" }
        }));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 5) { Page = 2 }));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 5)
        {
            TimeRange = SearchTimeRange.Day
        }));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 5) { SafeSearch = 1 }));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 5) { Language = "en" }));
    }

    [Fact]
    public void Parse_ReadsResultTitleUrlAndContent()
    {
        SearchResult result = Assert.Single(SeznamSearchResultParser.Parse(ResultPage));
        Assert.Equal("Seznam result", result.Title);
        Assert.Equal("https://example.com/article", result.Url);
        Assert.Equal("Seznam description.", result.Snippet);
        Assert.Equal("seznam", result.Provider);
    }

    [Fact]
    public void Parse_ReadsHiddenHomepageFields()
    {
        IReadOnlyDictionary<string, string> fields = SeznamSearchResultParser.ReadHiddenFields(
            "<input type='hidden' name='csrf' value='token-1'><input type='hidden' name='mode' value='web'>");

        Assert.Equal("token-1", fields["csrf"]);
        Assert.Equal("web", fields["mode"]);
    }

    [Fact]
    public void Parse_DetectsVerificationRedirect()
    {
        Assert.True(SeznamSearchResultParser.IsVerificationPage(new Uri("https://search.seznam.cz/verify")));
        Assert.False(SeznamSearchResultParser.IsVerificationPage(new Uri("https://search.seznam.cz/")));
    }

    [Fact]
    public void Parse_SkipsIncompleteAndInvalidResultCards()
    {
        const string html = """
            <div id="searchpage-root">
              <div class="Layout--left">
                <div class="f2c528"><div class="c8774a">Snippet</div><h3><a href="file:///tmp/a">Local</a></h3></div>
                <div class="f2c528"><div class="c8774a">No title link</div></div>
              </div>
            </div>
            """;

        Assert.Empty(SeznamSearchResultParser.Parse(html));
    }

    [Fact]
    public void Parse_ReturnsEmptyWhenSearchCardsAreMissing()
    {
        Assert.Empty(SeznamSearchResultParser.Parse("<html><body>Empty</body></html>"));
    }

    [Fact]
    public async Task SearchAsync_BootstrapsHomepageAndIncludesHiddenFields()
    {
        int requests = 0;
        Uri? searchUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requests++;
            if (requests == 1)
            {
                return HtmlResponse("<input type='hidden' name='csrf' value='token-1'>");
            }

            searchUri = request.RequestUri;
            return HtmlResponse(ResultPage);
        }));
        SeznamSearchProvider provider = new(client);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("cats & dogs", 10),
            CancellationToken.None);

        Assert.Equal(2, requests);
        Assert.Equal("https://search.seznam.cz/?q=cats%20%26%20dogs&oq=cats%20%26%20dogs&noredirect=1&csrf=token-1", searchUri?.AbsoluteUri);
        Assert.Single(results);
    }

    [Fact]
    public async Task SearchAsync_ThrowsWhenHomepageReturnsAnHttpError()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        SeznamSearchProvider provider = new(client);

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
        <div id="searchpage-root">
          <div class="Layout--left">
            <div class="f2c528">
              <div class="c8774a">Seznam description.</div>
              <h3><a href="https://example.com/article">Seznam result</a></h3>
            </div>
          </div>
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
