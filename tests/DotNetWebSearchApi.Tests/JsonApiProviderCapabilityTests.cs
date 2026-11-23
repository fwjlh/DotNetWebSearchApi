using System.Net;
using System.Text;
using DotNetWebSearchApi.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class JsonApiProviderCapabilityTests
{
    [Fact]
    public void Definitions_RemainUniqueAndCompleteAfterFileSplit()
    {
        string[] names = JsonApiProviderCatalog.Definitions
            .Select(definition => definition.Name)
            .ToArray();

        Assert.Equal(52, names.Length);
        Assert.Equal(names.Length, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.True(Array.IndexOf(names, "openverse") < Array.IndexOf(names, "mwmbl"));
    }

    [Theory]
    [InlineData("openverse", "images")]
    [InlineData("wikinews", "news")]
    [InlineData("bilibili", "videos")]
    [InlineData("dailymotion", "videos")]
    [InlineData("peertube", "videos")]
    [InlineData("deezer", "music")]
    [InlineData("musicbrainz", "music")]
    [InlineData("radiobrowser", "music")]
    [InlineData("crossref", "science")]
    [InlineData("openalex", "science")]
    [InlineData("europepmc", "science")]
    [InlineData("semanticscholar", "science")]
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
    public void Register_RegistersAllDefinitionsAndAppliesProviderHeaders()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EXA_API_KEY"] = "test-api-key"
            })
            .Build();
        ServiceCollection services = new();
        services.AddHttpClient();
        services.AddSingleton<SearchHttpTransport>();
        JsonApiProviderCatalog.Register(services, configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        ISearchProvider[] registered = provider.GetServices<ISearchProvider>().ToArray();
        HttpClient bilibiliClient = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient("bilibili");

        Assert.Equal(52, registered.Length);
        Assert.Contains(registered, item => item.Name == "exa");
        Assert.Contains(
            bilibiliClient.DefaultRequestHeaders.GetValues("Referer"),
            value => value == "https://search.bilibili.com");
    }

    [Fact]
    public void BilibiliDefinition_UsesRequiredRefererAndNoUnsupportedLimitParameter()
    {
        JsonApiSearchDefinition definition = JsonApiProviderCatalog.Definitions.Single(
            item => item.Name == "bilibili");

        Assert.Equal(string.Empty, definition.LimitParameter);
        Assert.Equal("https://search.bilibili.com", definition.Headers["Referer"]);
    }

    [Fact]
    public void Capabilities_KeepWikimediaCommonsGeneralUntilDirectImageUrlsExist()
    {
        using HttpClient client = new();
        JsonApiSearchProvider provider = new(
            client,
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wikimedia_commons"));

        Assert.True(provider.Capabilities.Supports(new SearchQuery("file", 10)));
        Assert.False(provider.Capabilities.Supports(new SearchQuery("file", 10)
        {
            Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "images" }
        }));
    }

    [Fact]
    public async Task SearchDetailedAsync_SkipsWikimediaCommonsForImageSearch()
    {
        int requestCount = 0;
        using HttpClient client = CreateClient(() => requestCount++);
        JsonApiSearchProvider provider = new(
            client,
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "wikimedia_commons"));
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
        Assert.Equal("wikimedia_commons", Assert.Single(result.UnsupportedProviders));
        Assert.Equal(0, requestCount);
    }

    [Fact]
    public async Task SearchAsync_PreservesSpecialistCategoryThroughSearxngMapping()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"data\":{\"result\":[{\"title\":\"Video result\",\"arcurl\":\"https://example.org/video\",\"description\":\"A video\"}]}}",
                    Encoding.UTF8,
                    "application/json")
            }));
        JsonApiSearchProvider provider = new(
            client,
            JsonApiProviderCatalog.Definitions.Single(item => item.Name == "bilibili"));

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("video", 10)
            {
                Categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "videos" }
            },
            CancellationToken.None);
        SearxngSearchResponse response = SearxngSearchResponseMapper.Map(
            "video",
            new SearchExecutionResult(results, Array.Empty<string>(), Array.Empty<string>()));

        Assert.Equal("videos", Assert.Single(results).Category);
        Assert.Equal("videos", Assert.Single(response.Results).Category);
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
