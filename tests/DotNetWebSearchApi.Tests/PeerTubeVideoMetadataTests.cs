using System.Net;
using System.Text;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class PeerTubeVideoMetadataTests
{
    [Fact]
    public async Task SearchAsync_MapsSameOriginEmbedAndVideoMetadata()
    {
        const string fixture = """
            {
              "data": [
                {
                  "name": "PeerTube example",
                  "url": "https://peertube.cpy.re/videos/watch/123",
                  "description": "A federated video",
                  "embedPath": "/videos/embed/123",
                  "duration": 1419,
                  "publishedAt": "2026-11-23T10:00:00Z",
                  "views": 1337,
                  "account": { "displayName": "PeerTube creator" },
                  "thumbnails": [
                    { "fileUrl": "https://peertube.cpy.re/static/thumbnails/123.jpg" }
                  ]
                }
              ]
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client);

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("PeerTube example", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "videos" }
            },
            CancellationToken.None));
        VideoResultMetadata video = Assert.IsType<VideoResultMetadata>(result.VideoMetadata);
        SearxngSearchResponse response = SearxngSearchResponseMapper.Map(
            "PeerTube example",
            new SearchExecutionResult(new[] { result }, Array.Empty<string>(), Array.Empty<string>()));
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(response, options));
        JsonElement mapped = Assert.Single(document.RootElement.GetProperty("results").EnumerateArray());

        Assert.Equal("https://peertube.cpy.re/videos/embed/123", video.EmbedUrl);
        Assert.Equal("https://peertube.cpy.re/static/thumbnails/123.jpg", result.ThumbnailUrl);
        Assert.Equal(1419d, video.DurationSeconds);
        Assert.Equal("PeerTube creator", video.Author);
        Assert.Equal("1337", video.Views);
        Assert.Equal(new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero), result.PublishedDate);
        Assert.Equal(video.EmbedUrl, mapped.GetProperty("iframe_src").GetString());
        Assert.Equal("videos.html", mapped.GetProperty("template").GetString());
    }

    [Fact]
    public async Task SearchAsync_RejectsCrossOriginRelativeEmbedAndUnsafeThumbnail()
    {
        const string fixture = """
            {
              "data": [
                {
                  "name": "Unsafe PeerTube video",
                  "url": "https://peertube.cpy.re/videos/watch/123",
                  "embedPath": "//evil.example/videos/embed/123",
                  "thumbnails": [
                    { "fileUrl": "javascript:alert(1)" }
                  ],
                  "duration": 10
                }
              ]
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client);

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("unsafe PeerTube", 10),
            CancellationToken.None));
        VideoResultMetadata video = Assert.IsType<VideoResultMetadata>(result.VideoMetadata);

        Assert.Equal(string.Empty, video.EmbedUrl);
        Assert.Equal(string.Empty, video.ThumbnailUrl);
        Assert.Null(result.ThumbnailUrl);
    }

    private static JsonApiSearchProvider CreateProvider(HttpClient client)
    {
        JsonApiSearchDefinition definition = JsonApiProviderCatalog.Definitions.Single(
            item => item.Name == "peertube");
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
