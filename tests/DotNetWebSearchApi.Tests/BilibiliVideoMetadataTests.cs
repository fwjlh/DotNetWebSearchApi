using System.Net;
using System.Text;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class BilibiliVideoMetadataTests
{
    [Fact]
    public async Task SearchAsync_MapsBilibiliVideoMetadata()
    {
        const string fixture = """
            {
              "data": {
                "result": [
                  {
                    "title": "Example Bilibili video",
                    "arcurl": "https://www.bilibili.com/video/BV123",
                    "description": "A video description",
                    "pic": "https://i0.hdslb.com/bfs/archive/example.jpg",
                    "author": "Example creator",
                    "play": 12000,
                    "pubdate": 1795437600
                  }
                ]
              }
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client);

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("example video", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "videos" }
            },
            CancellationToken.None));
        VideoResultMetadata video = Assert.IsType<VideoResultMetadata>(result.VideoMetadata);
        SearxngSearchResponse response = SearxngSearchResponseMapper.Map(
            "example video",
            new SearchExecutionResult(new[] { result }, Array.Empty<string>(), Array.Empty<string>()));
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(response, options));
        JsonElement mapped = Assert.Single(document.RootElement.GetProperty("results").EnumerateArray());

        Assert.Equal("videos", result.Category);
        Assert.Equal("https://i0.hdslb.com/bfs/archive/example.jpg", result.ThumbnailUrl);
        Assert.Equal("Example creator", video.Author);
        Assert.Equal("12000", video.Views);
        Assert.Equal(new DateTimeOffset(2026, 11, 23, 12, 40, 0, TimeSpan.Zero), result.PublishedDate);
        Assert.Equal("default.html", mapped.GetProperty("template").GetString());
        Assert.Equal("Example creator", mapped.GetProperty("author").GetString());
        Assert.Equal("12000", mapped.GetProperty("views").GetString());
    }

    [Fact]
    public async Task SearchAsync_RejectsNonHttpBilibiliThumbnail()
    {
        const string fixture = """
            {
              "data": {
                "result": [
                  {
                    "title": "Unsafe thumbnail",
                    "arcurl": "https://www.bilibili.com/video/BV123",
                    "pic": "data:image/jpeg;base64,AAAA",
                    "author": "Example creator"
                  }
                ]
              }
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client);

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("unsafe video", 10),
            CancellationToken.None));
        VideoResultMetadata video = Assert.IsType<VideoResultMetadata>(result.VideoMetadata);

        Assert.Equal(string.Empty, video.ThumbnailUrl);
        Assert.Null(result.ThumbnailUrl);
    }

    private static JsonApiSearchProvider CreateProvider(HttpClient client)
    {
        JsonApiSearchDefinition definition = JsonApiProviderCatalog.Definitions.Single(
            item => item.Name == "bilibili");
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
