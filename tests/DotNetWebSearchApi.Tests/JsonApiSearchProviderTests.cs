using System.Net;
using System.Text;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed partial class JsonApiSearchProviderTests
{
    [Theory]
    [MemberData(nameof(ProviderCases))]
    public async Task SearchAsync_UsesProviderEndpointAndMapsItsJson(
        JsonApiSearchDefinition definition,
        string fixture,
        string expectedTitle,
        string expectedUrl)
    {
        Uri? requestUri = null;
        string? requestBody = null;
        HttpMethod? requestMethod = null;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestUri = request.RequestUri;
            requestMethod = request.Method;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fixture, Encoding.UTF8, "application/json")
            };
        }));
        JsonApiSearchProvider provider = new(client, definition);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("C# & search", 12),
            CancellationToken.None);

        SearchResult result = Assert.Single(results);
        Assert.Equal(definition.Name, result.Provider);
        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal(expectedUrl, result.Url);
        if (string.Equals(definition.RequestMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Equal(HttpMethod.Post, requestMethod);
            using JsonDocument body = JsonDocument.Parse(requestBody ?? "{}");
            Assert.Equal("C# & search", body.RootElement.GetProperty("query").GetString());
            Assert.True(body.RootElement.TryGetProperty(definition.BodyLimitParameter, out _));
        }
        else
        {
            Assert.Equal(HttpMethod.Get, requestMethod);
            Assert.Contains("C%23%20%26%20search", requestUri!.Query);
            if (!string.IsNullOrWhiteSpace(definition.LimitParameter))
            {
                Assert.Contains(definition.LimitParameter + "=", Uri.UnescapeDataString(requestUri.Query));
            }
        }
        if (definition.Name == "wiby")
        {
            Assert.Contains("https://wiby.me", result.Snippet);
        }
    }

    [Fact]
    public async Task OpenAireSearch_UsesV4SelectableFieldsAndMapsV4Metadata()
    {
        JsonApiSearchDefinition definition = JsonApiProviderCatalog.Definitions
            .Single(item => item.Name == "openaire");
        Uri? requestUri = null;
        const string fixture = """
            {
              "results": [
                {
                  "id": "dedup_wf_001",
                  "mainTitle": "OpenAIRE record",
                  "descriptions": ["Open research metadata"],
                  "authors": [{ "fullName": "A. Author" }],
                  "publicationDate": "2024-01-01",
                  "publisher": "Example Publisher",
                  "container": { "name": "Research Policy" },
                  "type": "publication",
                  "instances": [{ "urls": ["https://example.org/full-text"] }]
                }
              ]
            }
            """;
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            requestUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fixture, Encoding.UTF8, "application/json")
            };
        }));
        JsonApiSearchProvider provider = new(client, definition);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("research", 5),
            CancellationToken.None);

        SearchResult result = Assert.Single(results);
        string requestQuery = Uri.UnescapeDataString(requestUri!.Query);
        Assert.Contains("select=id,mainTitle,descriptions,authors.fullName,publicationDate,publisher,container.name,pids,instances.urls,type", requestQuery);
        Assert.DoesNotContain("journal,", requestQuery);
        Assert.DoesNotContain("webLinks", requestQuery);
        Assert.Equal("Research Policy", result.PaperMetadata?.Journal);
        Assert.Equal("Example Publisher", result.PaperMetadata?.Publisher);
        Assert.Equal("A. Author", Assert.Single(result.PaperMetadata!.Authors));
        Assert.Equal("https://example.org/full-text", result.PaperMetadata.HtmlUrl);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyForUnexpectedJsonShape()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"unexpected\":true}")
            }));
        JsonApiSearchProvider provider = new(client, JsonApiProviderCatalog.Definitions[0]);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("missing", 5),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_RejectsHttpErrors()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        JsonApiSearchProvider provider = new(client, JsonApiProviderCatalog.Definitions[0]);

        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(() =>
            provider.SearchAsync(new SearchQuery("rate limited", 5), CancellationToken.None));
        Assert.Equal(ProviderFailureKind.RateLimited, exception.FailureKind);
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
