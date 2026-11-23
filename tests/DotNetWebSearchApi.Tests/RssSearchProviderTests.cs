using System.Net;
using System.Text;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class RssSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_ParsesGoogleNewsRss()
    {
        const string fixture = "<rss><channel><item><title>News headline</title><link>https://news.google.com/rss/articles/123</link><description>A news summary</description></item></channel></rss>";
        Uri? requestUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestUri = request.RequestUri;
            return Response(fixture);
        }));
        RssSearchProvider provider = new(client, Definition("google_news"));

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("AI policy", 10),
            CancellationToken.None);

        SearchResult result = Assert.Single(results);
        Assert.Equal("News headline", result.Title);
        Assert.Equal("https://news.google.com/rss/articles/123", result.Url);
        Assert.Contains("AI%20policy", requestUri!.Query);
    }

    [Fact]
    public async Task SearchAsync_ResolvesBingNewsRedirectToPublisher()
    {
        const string fixture = "<rss><channel><item><title>Bing headline</title><link>https://www.bing.com/news/apiclick.aspx?ref=1&amp;url=https%3A%2F%2Fpublisher.example%2Fstory%2F1</link><description>Publisher summary</description></item></channel></rss>";
        using HttpClient client = new(new StubHttpMessageHandler(_ => Response(fixture)));
        RssSearchProvider provider = new(client, Definition("bing_news"));

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("search engines", 10),
            CancellationToken.None);

        SearchResult result = Assert.Single(results);
        Assert.Equal("https://publisher.example/story/1", result.Url);
    }

    [Fact]
    public async Task SearchAsync_ParsesAtomFeed()
    {
        const string fixture = """
            <feed xmlns="http://www.w3.org/2005/Atom">
              <entry>
                <title>Atom headline</title>
                <link rel="alternate" href="https://publisher.example/atom-story" />
                <summary>An Atom summary</summary>
              </entry>
            </feed>
            """;
        using HttpClient client = new(new StubHttpMessageHandler(_ => Response(fixture)));
        RssSearchProvider provider = new(client, Definition("google_news"));

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("Atom", 10),
            CancellationToken.None));

        Assert.Equal("Atom headline", result.Title);
        Assert.Equal("https://publisher.example/atom-story", result.Url);
        Assert.Equal("An Atom summary", result.Snippet);
    }

    [Fact]
    public async Task SearchAsync_RejectsExternalDtd()
    {
        const string fixture = """
            <!DOCTYPE rss SYSTEM "https://example.invalid/external.dtd">
            <rss><channel><item><title>Blocked DTD</title><link>https://example.com</link></item></channel></rss>
            """;
        using HttpClient client = new(new StubHttpMessageHandler(_ => Response(fixture)));
        RssSearchProvider provider = new(client, Definition("google_news"));

        await Assert.ThrowsAsync<System.Xml.XmlException>(() => provider.SearchAsync(
            new SearchQuery("external entity", 10),
            CancellationToken.None));
    }

    private static RssSearchDefinition Definition(string name)
    {
        return name == "google_news"
            ? new RssSearchDefinition
            {
                Name = "google_news",
                Endpoint = "https://news.google.com/rss/search",
                QueryParameter = "q",
                FixedParameters = new Dictionary<string, string>
                {
                    ["hl"] = "en-GB",
                    ["gl"] = "GB",
                    ["ceid"] = "GB:en"
                }
            }
            : new RssSearchDefinition
            {
                Name = "bing_news",
                Endpoint = "https://www.bing.com/news/search",
                QueryParameter = "q",
                FixedParameters = new Dictionary<string, string>
                {
                    ["format"] = "rss",
                    ["setmkt"] = "en-GB"
                }
            };
    }

    private static HttpResponseMessage Response(string content)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/rss+xml")
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
