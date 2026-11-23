using System.Net;
using System.Text;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class OpenverseImageMetadataTests
{
    [Fact]
    public async Task SearchAsync_MapsOpenverseImageMetadata()
    {
        const string fixture = """
            {
              "results": [
                {
                  "title": "Tree Bark Photo",
                  "foreign_landing_url": "https://stocksnap.io/photo/example",
                  "attribution": "Tree Bark Photo by Tim Sullivan is CC0 1.0.",
                  "url": "https://cdn.example.org/tree.jpg",
                  "thumbnail": "https://api.openverse.org/thumb/tree",
                  "width": 6016,
                  "height": 4016,
                  "filetype": "jpg",
                  "source": "stocksnap",
                  "filesize": 896128,
                  "license": "cc0",
                  "license_url": "https://creativecommons.org/publicdomain/zero/1.0/",
                  "creator": "Tim Sullivan",
                  "creator_url": "https://creator.example.org/tim",
                  "provider": "stocksnap",
                  "license_version": "1.0",
                  "category": "photograph"
                }
              ]
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client);

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("tree bark", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "images" }
            },
            CancellationToken.None));

        ImageResultMetadata image = Assert.IsType<ImageResultMetadata>(result.ImageMetadata);
        Assert.Equal("images", result.Category);
        Assert.Equal("https://cdn.example.org/tree.jpg", image.ImageUrl);
        Assert.Equal("https://api.openverse.org/thumb/tree", result.ThumbnailUrl);
        Assert.Equal("6016 x 4016", image.Resolution);
        Assert.Equal("jpg", image.Format);
        Assert.Equal(896128L, image.FileSizeBytes);
        Assert.Equal("875.12 KB", image.FileSize);
        Assert.Equal("cc0", image.License);
        Assert.Equal("1.0", image.LicenseVersion);
        Assert.Equal("Tim Sullivan", image.Creator);
        Assert.Equal("stocksnap", image.Provider);
        Assert.Equal("photograph", image.MediaCategory);
    }

    [Fact]
    public async Task SearchAsync_RejectsNonHttpImageMetadataUrls()
    {
        const string fixture = """
            {
              "results": [
                {
                  "title": "Unsafe image URLs",
                  "foreign_landing_url": "https://example.org/landing",
                  "url": "javascript:alert(1)",
                  "thumbnail": "data:image/png;base64,AAAA",
                  "license": "cc0",
                  "license_url": "javascript:alert(1)",
                  "creator": "Example",
                  "creator_url": "/creator/example"
                }
              ]
            }
            """;
        using HttpClient client = CreateClient(fixture);
        JsonApiSearchProvider provider = CreateProvider(client);

        SearchResult result = Assert.Single(await provider.SearchAsync(
            new SearchQuery("unsafe", 10),
            CancellationToken.None));
        ImageResultMetadata image = Assert.IsType<ImageResultMetadata>(result.ImageMetadata);

        Assert.Equal(string.Empty, image.ImageUrl);
        Assert.Equal(string.Empty, image.ThumbnailUrl);
        Assert.Equal(string.Empty, image.LicenseUrl);
        Assert.Equal(string.Empty, image.CreatorUrl);
        Assert.Null(result.ThumbnailUrl);
    }

    private static JsonApiSearchProvider CreateProvider(HttpClient client)
    {
        JsonApiSearchDefinition definition = JsonApiProviderCatalog.Definitions.Single(
            item => item.Name == "openverse");
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
