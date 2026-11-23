using System.Net;
using System.Text;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class DailymotionVideoMetadataTests
{
    [Fact]
    public async Task SearchAsync_MapsVideoMetadataAndSearxngTemplate()
    {
        const string fixture = """
            {
              "list": [
                {
                  "id": "x123abc",
                  "title": "Example video",
                  "url": "https://www.dailymotion.com/video/x123abc",
                  "description": "A video description",
                  "embed_url": "https://geo.dailymotion.com/player/x123.html?video=x123abc",
                  "thumbnail_360_url": "https://s2.dmcdn.net/example/x360.jpg",
                  "duration": 423,
                  "uploaded_time": 1795437600
                }
              ]
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
        Assert.Equal("https://geo.dailymotion.com/player/x123.html?video=x123abc", video.EmbedUrl);
        Assert.Equal("https://s2.dmcdn.net/example/x360.jpg", result.ThumbnailUrl);
        Assert.Equal(423d, video.DurationSeconds);
        Assert.Equal(
            new DateTimeOffset(2026, 11, 23, 12, 40, 0, TimeSpan.Zero),
            result.PublishedDate);
        Assert.Equal("videos.html", mapped.GetProperty("template").GetString());
        Assert.Equal(video.EmbedUrl, mapped.GetProperty("iframe_src").GetString());
        Assert.Equal(423d, mapped.GetProperty("length").GetDouble());
    }

    [Fact]
    public async Task SearchAsync_RejectsNonHttpVideoEmbedAndThumbnailUrls()
    {
        const string fixture = """
            {
              "list": [
                {
                  "title": "Unsafe video",
                  "url": "https://www.dailymotion.com/video/x123abc",
                  "embed_url": "javascript:alert(1)",
                  "thumbnail_360_url": "data:image/jpeg;base64,AAAA",
                  "duration": 10
                }
              ]
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client);

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("unsafe video", 10),
            CancellationToken.None));
        VideoResultMetadata video = Assert.IsType<VideoResultMetadata>(result.VideoMetadata);

        Assert.Equal(string.Empty, video.EmbedUrl);
        Assert.Equal(string.Empty, video.ThumbnailUrl);
        Assert.Null(result.ThumbnailUrl);
    }

    private static JsonApiSearchProvider CreateProvider(HttpClient client)
    {
        JsonApiSearchDefinition definition = JsonApiProviderCatalog.Definitions.Single(
            item => item.Name == "dailymotion");
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
