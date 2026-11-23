using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class ImageResultMetadataTests
{
    [Fact]
    public void Map_UsesDefaultTemplateForOrdinaryResults()
    {
        SearchResult result = new(
            "Ordinary result",
            "https://example.org/result",
            "Description",
            "fixture",
            1.0);

        SearxngSearchResponse response = SearxngSearchResponseMapper.Map(
            "ordinary",
            new SearchExecutionResult(new[] { result }, Array.Empty<string>(), Array.Empty<string>()));

        Assert.Equal("default.html", Assert.Single(response.Results).Template);
    }

    [Theory]
    [InlineData("images")]
    [InlineData("videos")]
    public void Map_UsesDefaultTemplateWhenVerticalMetadataIsMissing(string category)
    {
        SearchResult result = CreateResult("Vertical result", "https://example.org/result") with
        {
            Category = category
        };

        SearxngSearchResponse response = Map(result);

        Assert.Equal("default.html", Assert.Single(response.Results).Template);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("/relative/image.jpg")]
    public void Map_UsesDefaultTemplateWhenImageSourceIsNotAbsoluteHttp(string imageUrl)
    {
        SearchResult result = CreateResult("Image result", "https://example.org/result") with
        {
            Category = "images",
            ImageMetadata = new ImageResultMetadata { ImageUrl = imageUrl }
        };

        SearxngSearchResponse response = Map(result);

        Assert.Equal("default.html", Assert.Single(response.Results).Template);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("//evil.example/embed")]
    public void Map_UsesDefaultTemplateWhenVideoEmbedIsNotAbsoluteHttp(string embedUrl)
    {
        SearchResult result = CreateResult("Video result", "https://example.org/result") with
        {
            Category = "videos",
            VideoMetadata = new VideoResultMetadata { EmbedUrl = embedUrl }
        };

        SearxngSearchResponse response = Map(result);

        Assert.Equal("default.html", Assert.Single(response.Results).Template);
    }

    [Fact]
    public void Map_PreservesTypedImageMetadataInSearxngFields()
    {
        ImageResultMetadata image = new()
        {
            ImageUrl = "https://images.example.org/full.jpg",
            ThumbnailUrl = "https://images.example.org/thumb.jpg",
            Resolution = "1920 x 1080",
            Format = "JPG",
            Source = "Example gallery",
            FileSize = "1.4 MB",
            FileSizeBytes = 1468006,
            License = "CC BY 4.0",
            LicenseUrl = "https://creativecommons.org/licenses/by/4.0/",
            Creator = "Example photographer",
            Formats = new[]
            {
                new ImageFormatReference(
                    "https://images.example.org/full.png",
                    "png",
                    "PNG")
            }
        };
        SearchResult result = new(
            "Example image",
            "https://example.org/image",
            "An image result",
            "openverse",
            1.0)
        {
            Category = "images",
            ImageMetadata = image
        };

        SearxngSearchResponse response = SearxngSearchResponseMapper.Map(
            "landscape",
            new SearchExecutionResult(new[] { result }, Array.Empty<string>(), Array.Empty<string>()));
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(response, options));
        JsonElement mapped = Assert.Single(document.RootElement.GetProperty("results").EnumerateArray());

        Assert.Equal("images", mapped.GetProperty("category").GetString());
        Assert.Equal("images.html", mapped.GetProperty("template").GetString());
        Assert.Equal(image.ImageUrl, mapped.GetProperty("img_src").GetString());
        Assert.Equal(image.ThumbnailUrl, mapped.GetProperty("thumbnail_src").GetString());
        Assert.Equal(image.Resolution, mapped.GetProperty("resolution").GetString());
        Assert.Equal(image.Format, mapped.GetProperty("img_format").GetString());
        Assert.Equal(image.Source, mapped.GetProperty("source").GetString());
        Assert.Equal(image.FileSize, mapped.GetProperty("filesize").GetString());
        Assert.Equal("png", mapped.GetProperty("formats")[0].GetProperty("subtype").GetString());
        Assert.Equal("image", mapped.GetProperty("formats")[0].GetProperty("mtype").GetString());
        Assert.Equal(image.License, mapped.GetProperty("image_metadata").GetProperty("license").GetString());
        Assert.Equal(image.Creator, mapped.GetProperty("image_metadata").GetProperty("creator").GetString());
    }
    private static SearchResult CreateResult(string title, string url)
    {
        return new SearchResult(title, url, title, "fixture", 1.0);
    }

    private static SearxngSearchResponse Map(SearchResult result)
    {
        return SearxngSearchResponseMapper.Map(
            "test",
            new SearchExecutionResult(new[] { result }, Array.Empty<string>(), Array.Empty<string>()));
    }

}
