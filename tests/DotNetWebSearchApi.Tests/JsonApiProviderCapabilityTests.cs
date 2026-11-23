using System.Net;
using System.Text;
using DotNetWebSearchApi.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class JsonApiProviderCapabilityTests
{
    [Theory]
    [InlineData("openverse", "images")]
    [InlineData("wikimedia_commons", "images")]
    [InlineData("wikinews", "news")]
    [InlineData("bilibili", "videos")]
    [InlineData("dailymotion", "videos")]
    [InlineData("peertube", "videos")]
    [InlineData("deezer", "music")]
    [InlineData("musicbrainz", "music")]
    [InlineData("radiobrowser", "music")]
    public void Capabilities_RestrictVerticalProviderToItsDeclaredCategory(
        string providerName,
        string category)
    {
        using HttpClient client = new();
        JsonApiSearchProvider provider = new(
            client,
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == providerName));

        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 10)));
        Assert.True(provider.Capabilities.Supports(
            new SearchQuery("test", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { category }
            }));
    }

    [Fact]
    public async Task SearchDetailedAsync_SkipsOpenverseForGeneralSearch()
    {
        int requestCount = 0;
        using HttpClient client = CreateClient(() => requestCount++);
        JsonApiSearchProvider provider = new(
            client,
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "openverse"));
        SearchAggregator aggregator = new(
            new[] { provider },
            NullLogger<SearchAggregator>.Instance);

        SearchExecutionResult result = await aggregator.SearchDetailedAsync(
            new SearchQuery("landscape", 10),
            CancellationToken.None);

        Assert.Empty(result.Results);
        Assert.Equal("openverse", Assert.Single(result.UnsupportedProviders));
        Assert.Equal(0, requestCount);
    }

    [Fact]
    public async Task SearchDetailedAsync_UsesOpenverseForImageSearch()
    {
        int requestCount = 0;
        using HttpClient client = CreateClient(() => requestCount++);
        JsonApiSearchProvider provider = new(
            client,
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "openverse"));
        SearchAggregator aggregator = new(
            new[] { provider },
            NullLogger<SearchAggregator>.Instance);

        SearchExecutionResult result = await aggregator.SearchDetailedAsync(
            new SearchQuery("landscape", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "images" }
            },
            CancellationToken.None);

        Assert.Equal("openverse", Assert.Single(result.Results).Provider);
        Assert.Empty(result.UnsupportedProviders);
        Assert.Equal(1, requestCount);
    }


    [Fact]
    public async Task SearchDetailedAsync_SkipsGeneralProviderForImageSearch()
    {
        int requestCount = 0;
        using HttpClient client = CreateClient(() => requestCount++);
        JsonApiSearchProvider provider = new(
            client,
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "crossref"));
        SearchAggregator aggregator = new(
            new[] { provider },
            NullLogger<SearchAggregator>.Instance);

        SearchExecutionResult result = await aggregator.SearchDetailedAsync(
            new SearchQuery("landscape", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "images" }
            },
            CancellationToken.None);

        Assert.Empty(result.Results);
        Assert.Equal("crossref", Assert.Single(result.UnsupportedProviders));
        Assert.Equal(0, requestCount);
    }

    private static HttpClient CreateClient(Action onRequest)
    {
        return new HttpClient(new StubHttpMessageHandler(_ =>
        {
            onRequest();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"results\":[{\"title\":\"Landscape image\",\"foreign_landing_url\":\"https://example.org/image\",\"attribution\":\"CC BY\"}]}",
                    Encoding.UTF8,
                    "application/json")
            };
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
