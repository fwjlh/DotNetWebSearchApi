using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class ArxivSearchProviderTests
{
    [Fact]
    public void Timeout_AllowsTwentySecondsForThePublicApi()
    {
        using HttpClient client = new();
        ArxivSearchProvider provider = new(client);

        Assert.Equal(TimeSpan.FromSeconds(20), provider.Timeout);
    }

    [Fact]
    public async Task SearchAsync_ParsesAtomEntriesAndEncodesQuery()
    {
        Uri? requestUri = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(AtomFixture, Encoding.UTF8, "application/atom+xml")
            };
        }));
        ArxivSearchProvider provider = new(client);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("machine learning & graphs", 8),
            CancellationToken.None);

        SearchResult result = Assert.Single(results);
        Assert.Equal("A research paper", result.Title);
        Assert.Equal("https://arxiv.org/abs/1234.5678", result.Url);
        Assert.Equal("A research summary.", result.Snippet);
        Assert.Equal("science", result.Category);
        PaperResultMetadata paper = Assert.IsType<PaperResultMetadata>(result.PaperMetadata);
        Assert.Equal("Ada Lovelace", paper.Author);
        Assert.Equal(new[] { "Ada Lovelace", "Grace Hopper" }, paper.Authors);
        Assert.Equal("https://arxiv.org/pdf/1234.5678", paper.PdfUrl);
        Assert.Equal("10.48550/arXiv.1234.5678", paper.Doi);
        Assert.Equal(new DateTimeOffset(2026, 11, 20, 0, 0, 0, TimeSpan.Zero), paper.DateOfPublication);
        SearxngSearchResponse mappedResponse = SearxngSearchResponseMapper.Map(
            "machine learning",
            new SearchExecutionResult(new[] { result }, Array.Empty<string>(), Array.Empty<string>()));
        Assert.Equal("paper.html", Assert.Single(mappedResponse.Results).Template);
        Assert.Contains("all%3Amachine%20learning%20%26%20graphs", requestUri!.Query);
        Assert.Contains("max_results=8", requestUri.Query);
    }

    [Fact]
    public async Task SearchDetailedAsync_SelectsArxivOnlyForScienceCategory()
    {
        int requestCount = 0;
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
        {
            requestCount++;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(AtomFixture, Encoding.UTF8, "application/atom+xml")
            };
        }));
        ArxivSearchProvider provider = new(client);
        SearchAggregator aggregator = new(
            new[] { provider },
            NullLogger<SearchAggregator>.Instance);

        SearchExecutionResult generalResult = await aggregator.SearchDetailedAsync(
            new SearchQuery("machine learning", 5),
            CancellationToken.None);

        Assert.Empty(generalResult.Results);
        Assert.Equal("arxiv", Assert.Single(generalResult.UnsupportedProviders));
        Assert.Equal(0, requestCount);

        SearchExecutionResult scienceResult = await aggregator.SearchDetailedAsync(
            new SearchQuery("machine learning", 5)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "science" }
            },
            CancellationToken.None);

        Assert.Equal("arxiv", Assert.Single(scienceResult.Results).Provider);
        Assert.Empty(scienceResult.UnsupportedProviders);
        Assert.Equal(1, requestCount);
    }

    [Fact]
    public async Task SearchAsync_RejectsHttpErrors()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
        ArxivSearchProvider provider = new(client);

        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(() =>
            provider.SearchAsync(new SearchQuery("test", 5), CancellationToken.None));
        Assert.Equal(ProviderFailureKind.TransientHttp, exception.FailureKind);
    }

    [Fact]
    public async Task SearchAsync_RejectsUnsafePdfUrl()
    {
        string fixture = AtomFixture.Replace(
            "https://arxiv.org/pdf/1234.5678",
            "javascript:alert(1)",
            StringComparison.Ordinal);
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fixture, Encoding.UTF8, "application/atom+xml")
            }));
        ArxivSearchProvider provider = new(client);

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("machine learning", 5) { Categories = new HashSet<string> { "science" } },
            CancellationToken.None));

        Assert.Equal(string.Empty, Assert.IsType<PaperResultMetadata>(result.PaperMetadata).PdfUrl);
    }

    private const string AtomFixture = """
        <feed xmlns="http://www.w3.org/2005/Atom"
              xmlns:arxiv="http://arxiv.org/schemas/atom">
          <entry>
            <id>https://arxiv.org/abs/1234.5678</id>
            <title>A research paper</title>
            <summary>A research summary.</summary>
            <published>2026-11-20T00:00:00Z</published>
            <author><name>Ada Lovelace</name></author>
            <author><name>Grace Hopper</name></author>
            <link rel="related" type="application/pdf" title="pdf" href="https://arxiv.org/pdf/1234.5678"/>
            <arxiv:journal_ref>Journal of Example Research</arxiv:journal_ref>
            <arxiv:doi>10.48550/arXiv.1234.5678</arxiv:doi>
            <category term="cs.LG"/>
            <category term="cs.AI"/>
          </entry>
        </feed>
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
