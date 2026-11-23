using DotNetWebSearchApi.Search;
using Xunit;
using Xunit.Abstractions;

namespace DotNetWebSearchApi.Tests;

public sealed class LiveProviderIntegrationTests(ITestOutputHelper output)
{
    private const string OptInVariable = "DOTNETWEBSEARCHAPI_LIVE_TESTS";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);

    [LiveProviderFact]
    public async Task Wikipedia_ReturnsResultsFromTheLiveSearchApi()
    {
        using HttpClient client = CreateClient();
        WikipediaSearchProvider provider = new(client, new SearchHttpTransport(CreateTransportOptions()));

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery("Albert Einstein", 3),
            CancellationToken.None);

        AssertLiveResults(provider.Name, results);
    }

    [LiveProviderFact]
    public async Task Crossref_ReturnsResultsFromTheLiveWorksApi()
    {
        IReadOnlyList<SearchResult> results = await SearchJsonProviderAsync(
            "crossref",
            "quantum mechanics");

        AssertLiveResults("crossref", results);
    }

    [LiveProviderFact]
    public async Task GitHub_ReturnsResultsFromTheLiveRepositorySearchApi()
    {
        IReadOnlyList<SearchResult> results = await SearchJsonProviderAsync(
            "github",
            "dotnet");

        AssertLiveResults("github", results);
    }

    private async Task<IReadOnlyList<SearchResult>> SearchJsonProviderAsync(
        string providerName,
        string queryText)
    {
        JsonApiSearchDefinition definition = Assert.Single(
            JsonApiProviderCatalog.Definitions.Where(item => item.Name == providerName));
        using HttpClient client = CreateClient();
        JsonApiSearchProvider provider = new(
            client,
            definition,
            new SearchHttpTransport(CreateTransportOptions()));

        IReadOnlyList<SearchResult> results = await provider.SearchAsync(
            new SearchQuery(queryText, 3),
            CancellationToken.None);

        return results;
    }

    private HttpClient CreateClient()
    {
        HttpClient client = new()
        {
            Timeout = RequestTimeout
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "DotNetWebSearchApi-live-tests/1.0 (+https://github.com/fwjlh/DotNetWebSearchApi)");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/html;q=0.9");
        return client;
    }

    private static SearchHttpTransportOptions CreateTransportOptions()
    {
        return new SearchHttpTransportOptions
        {
            MaximumResponseBytes = 1024 * 1024,
            MaximumConcurrentRequestsPerHost = 1
        };
    }

    private void AssertLiveResults(string providerName, IReadOnlyList<SearchResult> results)
    {
        output.WriteLine(
            "Live provider {0}: {1} results. {2}",
            providerName,
            results.Count,
            string.Join(" | ", results.Select(result => result.Title)));

        Assert.NotEmpty(results);
        Assert.All(results, result =>
        {
            Assert.False(string.IsNullOrWhiteSpace(result.Title));
            Assert.True(Uri.TryCreate(result.Url, UriKind.Absolute, out Uri? uri));
            Assert.True(uri!.Scheme is Uri.UriSchemeHttp or Uri.UriSchemeHttps);
        });
    }
}

public sealed class LiveProviderFactAttribute : FactAttribute
{
    public LiveProviderFactAttribute()
    {
        if (!string.Equals(
            Environment.GetEnvironmentVariable("DOTNETWEBSEARCHAPI_LIVE_TESTS"),
            "1",
            StringComparison.Ordinal))
        {
            Skip = "Set DOTNETWEBSEARCHAPI_LIVE_TESTS=1 to opt in to real upstream HTTP requests.";
        }
    }
}
