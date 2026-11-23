using System.Net;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class SearxngApiIntegrationTests
{
    [Fact]
    public void ProviderCookieContainers_AreIsolated()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        CookieContainer mojeek = factory.Services.GetRequiredKeyedService<CookieContainer>("mojeek");
        CookieContainer yahoo = factory.Services.GetRequiredKeyedService<CookieContainer>("yahoo");

        Assert.NotSame(mojeek, yahoo);
    }

    [Fact]
    public async Task GetSearch_ReturnsSearxngCompatibleJson()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("Example result", "https://example.com/page")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/search?q=dotnet&format=json");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement root = document.RootElement;
        JsonElement result = root.GetProperty("results")[0];

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("dotnet", root.GetProperty("query").GetString());
        Assert.Equal(1, root.GetProperty("number_of_results").GetInt32());
        Assert.Equal("alpha", result.GetProperty("engine").GetString());
        Assert.Equal("alpha", result.GetProperty("engines")[0].GetString());
        Assert.Equal(1, result.GetProperty("positions").GetProperty("alpha").GetInt32());
        Assert.Equal("general", result.GetProperty("category").GetString());
        Assert.Equal(6, result.GetProperty("parsed_url").GetArrayLength());
        Assert.Empty(root.GetProperty("answers").EnumerateArray());
        Assert.Empty(root.GetProperty("unresponsive_engines").EnumerateArray());
    }

    [Fact]
    public async Task Search_ExposesProviderThumbnailMetadata()
    {
        SearchResult result = Result("Naver result", "https://example.com/naver") with
        {
            ThumbnailUrl = "https://images.example.com/naver.jpg"
        };
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("naver", result));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/search?q=naver");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "https://images.example.com/naver.jpg",
            document.RootElement.GetProperty("results")[0].GetProperty("thumbnail").GetString());
    }

    [Fact]
    public async Task PostSearch_AcceptsFormEncodedRequest()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("Posted result", "https://example.com/post")));
        using HttpClient client = factory.CreateClient();
        using FormUrlEncodedContent content = new(new[]
        {
            new KeyValuePair<string, string>("q", "posted query"),
            new KeyValuePair<string, string>("format", "json")
        });

        using HttpResponseMessage response = await client.PostAsync("/search", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("posted query", document.RootElement.GetProperty("query").GetString());
    }

    [Fact]
    public async Task Search_RejectsMissingQuery()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/search?format=json");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_RejectsUnsupportedFormat()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/search?q=test&format=html");
        Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
    }

    [Fact]
    public async Task Search_ReportsProvidersThatCannotHandleRequestedPage()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/search?q=test&pageno=2");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(document.RootElement.GetProperty("results").EnumerateArray());
        Assert.Equal("alpha", document.RootElement.GetProperty("unsupported_engines")[0].GetString());
        Assert.Empty(document.RootElement.GetProperty("unresponsive_engines").EnumerateArray());
    }

    [Fact]
    public async Task Search_ParsesAllRequestContextFields()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(
            "/search?q=test&pageno=2&categories=general&language=fr-FR&safesearch=1&time_range=week");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("alpha", document.RootElement.GetProperty("unsupported_engines")[0].GetString());
    }

    [Fact]
    public async Task Search_MapsRequestContextIntoProviderQuery()
    {
        CapturingProvider provider = new();
        using WebApplicationFactory<Program> factory = CreateFactory(provider);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            "/search?q=test&pageno=2&categories=general,news&language=fr-FR&safesearch=2&time_range=week");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(provider.LastQuery);
        Assert.Equal(2, provider.LastQuery.Page);
        Assert.Equal(new[] { "general", "news" }, provider.LastQuery.Categories!.OrderBy(value => value));
        Assert.Equal("fr-FR", provider.LastQuery.Language);
        Assert.Equal(2, provider.LastQuery.SafeSearch);
        Assert.Equal(SearchTimeRange.Week, provider.LastQuery.TimeRange);
    }

    [Fact]
    public async Task Search_RejectsInvalidRequestContextValues()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/search?q=test&safesearch=3");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ApiSearch_AcceptsRequestContextAndReportsUnsupportedProviders()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(
            "/api/search?q=test&limit=5&pageno=2&language=fr-FR");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("alpha", document.RootElement.GetProperty("unsupportedProviders")[0].GetString());
        Assert.Empty(document.RootElement.GetProperty("results").EnumerateArray());
    }

    [Fact]
    public async Task Search_FiltersRequestedEngines()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("alpha", Result("Alpha", "https://example.com/a")),
            Provider("beta", Result("Beta", "https://example.com/b")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/search?q=test&engines=alpha");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(1, document.RootElement.GetProperty("number_of_results").GetInt32());
        Assert.Equal("Alpha", document.RootElement.GetProperty("results")[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task Search_ReportsFailedProvidersInCompatibilityResponse()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            new TestProvider("broken", (_, _) => throw new InvalidOperationException("failure")),
            Provider("working", Result("Available", "https://example.com/ok")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/search?q=test");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("broken", document.RootElement.GetProperty("unresponsive_engines")[0].GetString());
    }

    [Fact]
    public async Task Search_ReportsCoolingDownProvidersSeparatelyFromFailures()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            new TestProvider("challenge", (_, _) =>
                throw new ProviderFailureException(ProviderFailureKind.Challenge, "verification")));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage firstResponse = await client.GetAsync("/search?q=test");
        using JsonDocument firstDocument = await JsonDocument.ParseAsync(await firstResponse.Content.ReadAsStreamAsync());
        Assert.Equal("challenge", firstDocument.RootElement.GetProperty("unresponsive_engines")[0].GetString());

        using HttpResponseMessage secondResponse = await client.GetAsync("/search?q=test");
        using JsonDocument secondDocument = await JsonDocument.ParseAsync(await secondResponse.Content.ReadAsStreamAsync());

        Assert.Empty(secondDocument.RootElement.GetProperty("unresponsive_engines").EnumerateArray());
        Assert.Equal("challenge", secondDocument.RootElement.GetProperty("cooling_down_engines")[0].GetString());
    }

    [Fact]
    public async Task ApiSearch_ExposesTypedChallengeClassification()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            new TestProvider("captcha", (_, _) => throw new ProviderFailureException(
                ProviderFailureKind.Challenge,
                "Verification required",
                challengeKind: ProviderChallengeKind.InteractiveCaptcha)));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/search?q=test");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement outcome = document.RootElement.GetProperty("providerOutcomes")[0];

        Assert.Equal("Challenge", outcome.GetProperty("failureKind").GetString());
        Assert.Equal("InteractiveCaptcha", outcome.GetProperty("challengeKind").GetString());
    }

    [Fact]
    public async Task Search_ExposesTypedChallengeClassification()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            new TestProvider("verification", (_, _) => throw new ProviderFailureException(
                ProviderFailureKind.Challenge,
                "Browser verification required",
                challengeKind: ProviderChallengeKind.BrowserVerification)));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/search?q=test");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement outcome = document.RootElement.GetProperty("provider_outcomes")[0];

        Assert.Equal("BrowserVerification", outcome.GetProperty("challengeKind").GetString());
    }

    [Fact]
    public async Task Search_RejectsUnknownEngineNames()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/search?q=test&engines=unknown");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ProvidersEndpoint_ReturnsConfiguredProviders()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            Provider("beta", Result("B", "https://example.com/b")),
            Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/providers");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(2, document.RootElement.GetProperty("provider_count").GetInt32());
        Assert.Equal(new[] { "alpha", "beta" }, document.RootElement.GetProperty("providers").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public async Task DiagnosticsEndpoint_TracksSuccessfulSearch()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage searchResponse = await client.GetAsync("/search?q=test");
        searchResponse.EnsureSuccessStatusCode();
        using HttpResponseMessage response = await client.GetAsync("/api/diagnostics");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement provider = document.RootElement.GetProperty("providers")[0];

        Assert.Equal("ok", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("healthy", provider.GetProperty("status").GetString());
        Assert.Equal(1, provider.GetProperty("successfulSearches").GetInt64());
        Assert.Equal("Closed", document.RootElement.GetProperty("provider_health")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task DiagnosticsEndpoint_TracksProviderFailure()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(
            new TestProvider("broken", (_, _) => throw new InvalidOperationException("failure")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage searchResponse = await client.GetAsync("/search?q=test");
        searchResponse.EnsureSuccessStatusCode();
        using HttpResponseMessage response = await client.GetAsync("/api/diagnostics");
        using JsonDocument document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        JsonElement provider = document.RootElement.GetProperty("providers")[0];

        Assert.Equal("degraded", document.RootElement.GetProperty("status").GetString());
        Assert.Equal("unresponsive", provider.GetProperty("status").GetString());
        Assert.Equal(1, provider.GetProperty("failedSearches").GetInt64());
    }

    [Fact]
    public async Task RootEndpoint_ServesSearchPage()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/");
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Search across the web", html);
    }

    [Fact]
    public async Task AdminEndpoint_ServesDiagnosticsPage()
    {
        using WebApplicationFactory<Program> factory = CreateFactory(Provider("alpha", Result("A", "https://example.com/a")));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/admin");
        string html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Provider diagnostics", html);
    }

    private static WebApplicationFactory<Program> CreateFactory(params ISearchProvider[] providers)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISearchProvider>();
                services.RemoveAll<SearchAggregator>();
                foreach (ISearchProvider provider in providers)
                {
                    services.AddSingleton(provider);
                }

                services.AddSingleton<SearchAggregator>();
            }));
    }

    private static TestProvider Provider(string name, SearchResult result)
    {
        return new TestProvider(name, (_, _) => Task.FromResult<IReadOnlyList<SearchResult>>(new[] { result with { Provider = name } }));
    }

    private static SearchResult Result(string title, string url)
    {
        return new SearchResult(title, url, title + " description", "fixture", 1.0);
    }

    private sealed class CapturingProvider : ISearchProvider, ISearchProviderCapabilities
    {
        public string Name => "capture";
        public TimeSpan Timeout => TimeSpan.FromSeconds(2);
        public SearchQuery? LastQuery { get; private set; }
        public EngineCapabilities Capabilities { get; } = new(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general", "news" },
            true,
            10,
            true,
            true,
            true);

        public Task<IReadOnlyList<SearchResult>> SearchAsync(
            SearchQuery query,
            CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>());
        }
    }

    private sealed class TestProvider(
        string name,
        Func<SearchQuery, CancellationToken, Task<IReadOnlyList<SearchResult>>> search) : ISearchProvider
    {
        public string Name => name;
        public TimeSpan Timeout => TimeSpan.FromSeconds(2);
        public Task<IReadOnlyList<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
            => search(query, cancellationToken);
    }
}
