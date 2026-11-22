using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class KagiSearchProviderTests
{
    [Fact]
    public async Task SearchAsync_MapsPythonRequestContextIntoKagiPostBody()
    {
        string? requestBody = null;
        HttpRequestMessage? capturedRequest = null;
        using HttpClient client = new(new StubHttpMessageHandler(async request =>
        {
            capturedRequest = request;
            requestBody = await request.Content!.ReadAsStringAsync();
            return JsonResponse("{\"data\":{\"search\":[{\"url\":\"https://example.com/result\",\"title\":\"<b>Result</b>\",\"snippet\":\"A &amp; B\"}]}}");
        }));
        KagiSearchProvider provider = CreateProvider(client);
        SearchQuery query = new("test query", 5)
        {
            Page = 3,
            SafeSearch = 2,
            Language = "en-GB",
            TimeRange = SearchTimeRange.Week
        };

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(query, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("Bearer test-api-key", capturedRequest.Headers.Authorization!.ToString());
        using JsonDocument body = JsonDocument.Parse(requestBody!);
        JsonElement root = body.RootElement;
        Assert.Equal("test query", root.GetProperty("query").GetString());
        Assert.Equal(3, root.GetProperty("page").GetInt32());
        Assert.Equal("search", root.GetProperty("workflow").GetString());
        Assert.True(root.GetProperty("safe_search").GetBoolean());
        Assert.Equal("gb", root.GetProperty("filters").GetProperty("region").GetString());
        Assert.Equal("2026-11-15", root.GetProperty("filters").GetProperty("after").GetString());
        Assert.Equal("Result", Assert.Single(results).Title);
        Assert.Equal("A & B", results[0].Snippet);
    }

    [Fact]
    public async Task SearchAsync_UsesNoRegionAndLeavesSafeSearchOffByDefault()
    {
        string? requestBody = null;
        using HttpClient client = new(new StubHttpMessageHandler(async request =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return JsonResponse("{\"data\":{\"search\":[]}}");
        }));
        KagiSearchProvider provider = CreateProvider(client);

        await provider.SearchAsync(new SearchQuery("test", 10), CancellationToken.None);

        using JsonDocument body = JsonDocument.Parse(requestBody!);
        Assert.False(body.RootElement.GetProperty("safe_search").GetBoolean());
        Assert.Equal("no_region", body.RootElement.GetProperty("filters").GetProperty("region").GetString());
        Assert.False(body.RootElement.GetProperty("filters").TryGetProperty("after", out _));
    }

    [Fact]
    public void Capabilities_DeclareOnlySupportedGeneralSearchFeatures()
    {
        KagiSearchProvider provider = CreateProvider(new HttpClient(new StubHttpMessageHandler(_ => JsonResponse("{\"data\":{\"search\":[]}}"))));

        Assert.True(provider.Capabilities.Supports(new SearchQuery("test", 10)
        {
            Page = 10,
            SafeSearch = 2,
            Language = "en-GB",
            TimeRange = SearchTimeRange.Year
        }));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 10) { Page = 11 }));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("test", 10)
        {
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "news" }
        }));
    }

    private static KagiSearchProvider CreateProvider(HttpClient client)
    {
        return new KagiSearchProvider(
            client,
            new KagiSearchOptions("test-api-key"),
            timeProvider: new FixedTimeProvider(DateTimeOffset.Parse("2026-11-22T12:00:00Z")));
    }

    private static HttpResponseMessage JsonResponse(string json)
    {
        StringContent content = new(json, Encoding.UTF8, "application/json");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return responseFactory(request);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
