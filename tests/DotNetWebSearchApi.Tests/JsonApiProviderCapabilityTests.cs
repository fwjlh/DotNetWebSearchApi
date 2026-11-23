using System.Net;
using System.Text;
using DotNetWebSearchApi.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class JsonApiProviderCapabilityTests
{
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
