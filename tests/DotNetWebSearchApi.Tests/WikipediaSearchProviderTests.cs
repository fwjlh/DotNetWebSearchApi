using System.Net;
using System.Text;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class WikipediaSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_UsesLocaleSpecificWikiHostAndLanguageHeader()
    {
        Uri? requestedUri = null;
        string? acceptLanguage = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            acceptLanguage = string.Join(",", request.Headers.AcceptLanguage.Select(value => value.ToString()));
            return JsonResponse();
        }));
        WikipediaSearchProvider provider = new(client);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("出租車", 10) { Language = "zh-TW" },
            CancellationToken.None);

        Assert.Equal("zh.wikipedia.org", requestedUri?.Host);
        Assert.Contains("zh-TW", acceptLanguage);
        Assert.Contains("zh;q=0.9", acceptLanguage);
        Assert.Equal("https://zh.wikipedia.org/?curid=52", Assert.Single(results).Url);
    }

    [Fact]
    public async Task SearchAsync_UsesEnglishHostForAutomaticLocale()
    {
        Uri? requestedUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return JsonResponse();
        }));
        WikipediaSearchProvider provider = new(client);

        await provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None);

        Assert.Equal("en.wikipedia.org", requestedUri?.Host);
    }

    [Fact]
    public void Capabilities_DeclareLocaleButNotPagingOrTimeRange()
    {
        WikipediaSearchProvider provider = new(new HttpClient());
        Assert.True(provider.Capabilities.SupportsLocale);
        Assert.False(provider.Capabilities.SupportsPagination);
        Assert.False(provider.Capabilities.SupportsTimeRange);
        Assert.False(provider.Capabilities.SupportsSafeSearch);
    }

    private static HttpResponseMessage JsonResponse()
    {
        const string json = """{"query":{"search":[{"title":"Wikipedia page","pageid":52,"snippet":"A <b>short</b> description."}]}}""";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

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
