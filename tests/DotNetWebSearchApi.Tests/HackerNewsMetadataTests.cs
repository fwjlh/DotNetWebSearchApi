using System.Net;
using System.Text;
using DotNetWebSearchApi.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class HackerNewsMetadataTests
{
    [Fact]
    public async Task SearchAsync_UsesRequestedCategoryAndPreservesPublicationDate()
    {
        const string fixture = """
            {
              "hits": [
                {
                  "title": "Example Hacker News story",
                  "url": "https://example.org/story",
                  "story_text": "A story summary",
                  "created_at": "2026-11-23T10:00:00.000Z"
                }
              ]
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client);

        SearchResult generalResult = Assert.Single(await provider.SearchAsync(
            new SearchQuery("story", 10),
            CancellationToken.None));
        SearchResult newsResult = Assert.Single(await provider.SearchAsync(
            new SearchQuery("story", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "news" }
            },
            CancellationToken.None));

        Assert.Equal("general", generalResult.Category);
        Assert.Equal("news", newsResult.Category);
        Assert.Equal(
            new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero),
            newsResult.PublishedDate);
    }

    [Fact]
    public async Task SearchDetailedAsync_SelectsHackerNewsForNewsVertical()
    {
        const string fixture = """
            {
              "hits": [
                {
                  "title": "Example Hacker News story",
                  "url": "https://example.org/story",
                  "story_text": "A story summary",
                  "created_at": "2026-11-23T10:00:00.000Z"
                }
              ]
            }
            """;
        using HttpClient client = CreateClient(fixture);
        SearchAggregator aggregator = new(new[] { CreateProvider(client) }, NullLogger<SearchAggregator>.Instance);

        SearchExecutionResult result = await aggregator.SearchDetailedAsync(
            new SearchQuery("story", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "news" }
            },
            CancellationToken.None);

        Assert.Equal("news", Assert.Single(result.Results).Category);
        Assert.Empty(result.UnsupportedProviders);
    }

    private static JsonApiSearchProvider CreateProvider(HttpClient client)
    {
        JsonApiSearchDefinition definition = JsonApiProviderCatalog.Definitions.Single(
            item => item.Name == "hackernews");
        return new JsonApiSearchProvider(client, definition);
    }

    private static HttpClient CreateClient(string fixture)
    {
        return new HttpClient(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(fixture, Encoding.UTF8, "application/json")
            }));
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
