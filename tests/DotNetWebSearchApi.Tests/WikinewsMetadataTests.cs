using System.Net;
using System.Text;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class WikinewsMetadataTests
{
    [Fact]
    public async Task SearchAsync_PreservesNewsTimestampInSearxngResponse()
    {
        const string fixture = """
            {
              "query": {
                "search": [
                  {
                    "title": "Example news story",
                    "snippet": "A news summary",
                    "timestamp": "2026-11-23T10:00:00Z"
                  }
                ]
              }
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client);

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("news story", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "news" }
            },
            CancellationToken.None));
        SearxngSearchResponse response = SearxngSearchResponseMapper.Map(
            "news story",
            new SearchExecutionResult(new[] { result }, Array.Empty<string>(), Array.Empty<string>()));
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(response, options));
        JsonElement mapped = Assert.Single(document.RootElement.GetProperty("results").EnumerateArray());

        Assert.Equal("news", result.Category);
        Assert.Equal(new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero), result.PublishedDate);
        Assert.Equal("2026-11-23T10:00:00+00:00", mapped.GetProperty("publishedDate").GetString());
        Assert.Equal("default.html", mapped.GetProperty("template").GetString());
    }

    [Fact]
    public async Task SearchAsync_LeavesMissingOrInvalidNewsTimestampsUnset()
    {
        const string fixture = """
            {
              "query": {
                "search": [
                  {
                    "title": "No timestamp",
                    "snippet": "No date supplied"
                  },
                  {
                    "title": "Invalid timestamp",
                    "snippet": "Invalid date supplied",
                    "timestamp": "not-a-date"
                  }
                ]
              }
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client);

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("news", 10),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.Null(result.PublishedDate));
    }

    private static JsonApiSearchProvider CreateProvider(HttpClient client)
    {
        JsonApiSearchDefinition definition = JsonApiProviderCatalog.Definitions.Single(
            item => item.Name == "wikinews");
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
