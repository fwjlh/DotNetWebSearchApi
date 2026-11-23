using System.Net;
using System.Text;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class MusicResultMetadataTests
{
    [Fact]
    public async Task SearchAsync_MapsDeezerPreviewAndTrackMetadata()
    {
        const string fixture = """
            {
              "data": [
                {
                  "title": "Example track",
                  "link": "https://www.deezer.com/track/123",
                  "preview": "https://cdns-preview.example.org/track.mp3",
                  "duration": 213,
                  "artist": { "name": "Example artist" },
                  "album": { "title": "Example album", "cover_medium": "https://cdn.example.org/cover.jpg" }
                }
              ]
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client, "deezer");

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("example track", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "music" }
            },
            CancellationToken.None));
        MusicResultMetadata music = Assert.IsType<MusicResultMetadata>(result.MusicMetadata);
        SearxngSearchResponse response = SearxngSearchResponseMapper.Map(
            "example track",
            new SearchExecutionResult(new[] { result }, Array.Empty<string>(), Array.Empty<string>()));
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(response, options));
        JsonElement mapped = Assert.Single(document.RootElement.GetProperty("results").EnumerateArray());

        Assert.Equal("music", result.Category);
        Assert.Equal("Example artist", music.Artist);
        Assert.Equal("Example album", music.Album);
        Assert.Equal(213d, music.DurationSeconds);
        Assert.Equal("https://cdn.example.org/cover.jpg", result.ThumbnailUrl);
        Assert.Equal("https://cdns-preview.example.org/track.mp3", mapped.GetProperty("audio_src").GetString());
        Assert.Equal("default.html", mapped.GetProperty("template").GetString());
    }

    [Fact]
    public async Task SearchAsync_MapsRadioBrowserStreamAndStationMetadata()
    {
        const string fixture = """
            [
              {
                "name": "Example jazz radio",
                "url": "https://stream.example.org/jazz",
                "url_resolved": "https://stream.example.org/jazz.mp3",
                "favicon": "https://radio.example.org/favicon.ico",
                "country": "United Kingdom",
                "tags": "jazz,instrumental",
                "codec": "MP3",
                "bitrate": 128
              }
            ]
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client, "radiobrowser");

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("jazz", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "music" }
            },
            CancellationToken.None));
        MusicResultMetadata music = Assert.IsType<MusicResultMetadata>(result.MusicMetadata);

        Assert.Equal("https://stream.example.org/jazz.mp3", music.AudioUrl);
        Assert.Equal("https://radio.example.org/favicon.ico", result.ThumbnailUrl);
        Assert.Equal("United Kingdom", music.Country);
        Assert.Equal("jazz,instrumental", music.Tags);
        Assert.Equal("MP3", music.Codec);
        Assert.Equal(128, music.Bitrate);
    }

    [Fact]
    public async Task SearchAsync_RejectsUnsafeMusicStreamAndThumbnailUrls()
    {
        const string fixture = """
            {
              "data": [
                {
                  "title": "Unsafe track",
                  "link": "https://www.deezer.com/track/123",
                  "preview": "javascript:alert(1)",
                  "artist": { "name": "Example artist" },
                  "album": { "cover_medium": "data:image/jpeg;base64,AAAA" }
                }
              ]
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client, "deezer");

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("unsafe track", 10),
            CancellationToken.None));
        MusicResultMetadata music = Assert.IsType<MusicResultMetadata>(result.MusicMetadata);

        Assert.Equal(string.Empty, music.AudioUrl);
        Assert.Equal(string.Empty, music.ThumbnailUrl);
        Assert.Null(result.ThumbnailUrl);
    }

    private static JsonApiSearchProvider CreateProvider(HttpClient client, string name)
    {
        JsonApiSearchDefinition definition = JsonApiProviderCatalog.Definitions.Single(
            item => item.Name == name);
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
