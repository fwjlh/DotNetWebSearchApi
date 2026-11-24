using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using DotNetWebSearchApi.Search;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace DotNetWebSearchApi.Tests;

public sealed class LiveProviderIntegrationTests(ITestOutputHelper output)
{
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


    [LiveProviderFact]
    public async Task AllRegisteredProviders_RespondToLiveRequests()
    {
        int offset = ReadBatchSetting("DOTNETWEBSEARCHAPI_LIVE_PROVIDER_OFFSET", 0);
        int batchSize = ReadBatchSetting("DOTNETWEBSEARCHAPI_LIVE_PROVIDER_COUNT", 20);
        if (batchSize is < 1 or > 20)
        {
            throw new InvalidOperationException("Live provider batch size must be between 1 and 20.");
        }

        using WebApplicationFactory<Program> factory = new();
        ISearchProvider[] providers = factory.Services
            .GetServices<ISearchProvider>()
            .OrderBy(provider => provider.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        ISearchProvider[] batch = providers.Skip(offset).Take(batchSize).ToArray();

        output.WriteLine(
            "Provider inventory: {0} registered; batch offset {1}; selected {2}.",
            providers.Length,
            offset,
            batch.Length);

        List<string> failures = new();
        foreach (ISearchProvider provider in batch)
        {
            await CheckProviderAsync(provider, failures);
        }

        Assert.True(
            failures.Count == 0,
            "Live provider batch failed:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
    }


    [LiveProviderFact]
    public async Task FullSearchApi_ReturnsAggregatedLiveResultsAndCompatibilityJson()
    {
        using WebApplicationFactory<Program> factory = new();
        using HttpClient client = factory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(2);
        string? configuredApiKeys = factory.Services
            .GetRequiredService<IConfiguration>()["SEARCH_API_KEYS"];
        string? apiKey = configuredApiKeys?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        using HttpResponseMessage health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        output.WriteLine("API health: HTTP {0}.", (int)health.StatusCode);

        using HttpResponseMessage inventoryResponse = await client.GetAsync("/api/providers");
        Assert.Equal(HttpStatusCode.OK, inventoryResponse.StatusCode);
        using JsonDocument inventory = await JsonDocument.ParseAsync(
            await inventoryResponse.Content.ReadAsStreamAsync());
        string[] providerNames = inventory.RootElement.GetProperty("providers")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
        output.WriteLine(
            "API inventory: {0} providers; names: {1}",
            providerNames.Length,
            string.Join(", ", providerNames));

        using HttpResponseMessage diagnostics = await client.GetAsync("/api/diagnostics");
        Assert.Equal(HttpStatusCode.OK, diagnostics.StatusCode);
        output.WriteLine("API diagnostics: HTTP {0}.", (int)diagnostics.StatusCode);

        using HttpResponseMessage searchResponse = await client.GetAsync("/api/search?q=dotnet&limit=10");
        string searchBody = await searchResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        using JsonDocument search = JsonDocument.Parse(searchBody);
        JsonElement root = search.RootElement;
        JsonElement results = root.GetProperty("results");
        JsonElement outcomes = root.GetProperty("providerOutcomes");
        int resultCount = results.GetArrayLength();
        int succeeded = 0;
        int failed = 0;
        int skipped = 0;
        List<string> successfulProviders = new();
        foreach (JsonElement outcome in outcomes.EnumerateArray())
        {
            string name = outcome.GetProperty("providerName").GetString() ?? "unknown";
            string status = outcome.GetProperty("status").GetString() ?? "unknown";
            string failureKind = outcome.TryGetProperty("failureKind", out JsonElement failure)
                ? failure.ToString()
                : "none";
            output.WriteLine("API PROVIDER {0}: {1}; failure={2}", name, status, failureKind);
            if (status == "Succeeded")
            {
                succeeded++;
                successfulProviders.Add(name);
            }
            else if (status == "Failed")
            {
                failed++;
            }
            else if (status == "Skipped")
            {
                skipped++;
            }
        }

        HashSet<string> resultUrls = new(StringComparer.OrdinalIgnoreCase);
        foreach (JsonElement result in results.EnumerateArray())
        {
            string title = result.GetProperty("title").GetString() ?? string.Empty;
            string url = result.GetProperty("url").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(title));
            Assert.True(Uri.TryCreate(url, UriKind.Absolute, out Uri? uri));
            Assert.True(uri!.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            resultUrls.Add(url);
        }

        output.WriteLine(
            "API aggregate: HTTP {0}; results={1}; uniqueUrls={2}; succeededProviders={3}; failedProviders={4}; skippedProviders={5}.",
            (int)searchResponse.StatusCode,
            resultCount,
            resultUrls.Count,
            succeeded,
            failed,
            skipped);
        foreach (JsonElement result in results.EnumerateArray().Take(10))
        {
            output.WriteLine(
                "API RESULT: {0} | {1} | engines={2}",
                result.GetProperty("title").GetString(),
                result.GetProperty("url").GetString(),
                string.Join(",", result.GetProperty("engines").EnumerateArray().Select(item => item.GetString())));
        }

        Assert.NotEmpty(providerNames);
        Assert.True(succeeded > 0, "The aggregate API search had no successful providers.");
        Assert.True(resultCount > 0, "The aggregate API search returned no results for 'dotnet'.");
        Assert.Equal(resultCount, resultUrls.Count);

        string selectedProvider = successfulProviders[0];
        using HttpResponseMessage compatibilityResponse = await client.GetAsync(
            "/search?q=dotnet&format=json&engines=" + Uri.EscapeDataString(selectedProvider));
        Assert.Equal(HttpStatusCode.OK, compatibilityResponse.StatusCode);
        using JsonDocument compatibility = await JsonDocument.ParseAsync(
            await compatibilityResponse.Content.ReadAsStreamAsync());
        JsonElement compatibilityRoot = compatibility.RootElement;
        int compatibilityResults = compatibilityRoot.GetProperty("results").GetArrayLength();
        output.WriteLine(
            "SearXNG compatibility endpoint: HTTP {0}; provider={1}; results={2}; unresponsive={3}; coolingDown={4}.",
            (int)compatibilityResponse.StatusCode,
            selectedProvider,
            compatibilityResults,
            compatibilityRoot.GetProperty("unresponsive_engines").GetArrayLength(),
            compatibilityRoot.GetProperty("cooling_down_engines").GetArrayLength());
        Assert.Equal("dotnet", compatibilityRoot.GetProperty("query").GetString());
    }

    private async Task CheckProviderAsync(ISearchProvider provider, ICollection<string> failures)
    {
        using CancellationTokenSource timeout = new(provider.Timeout);
        try
        {
            IReadOnlyList<SearchResult> results = await provider.SearchAsync(
                new SearchQuery("dotnet", 3),
                timeout.Token);
            ValidateResults(provider.Name, results);
            if (results.Count == 0)
            {
                output.WriteLine(
                    "EMPTY {0}: provider responded successfully but returned no results for the assessment query.",
                    provider.Name);
            }
            else
            {
                output.WriteLine(
                    "PASS {0}: {1} results. {2}",
                    provider.Name,
                    results.Count,
                    string.Join(" | ", results.Take(3).Select(result => result.Title)));
            }
        }
        catch (Exception exception)
        {
            string failure = $"{provider.Name}: {exception.GetType().Name}: {exception.Message}";
            failures.Add(failure);
            output.WriteLine("FAIL {0}", failure);
        }
    }

    private static void ValidateResults(string providerName, IReadOnlyList<SearchResult> results)
    {
        foreach (SearchResult result in results)
        {
            if (string.IsNullOrWhiteSpace(result.Title))
            {
                throw new InvalidOperationException($"Provider '{providerName}' returned a result without a title.");
            }

            if (!Uri.TryCreate(result.Url, UriKind.Absolute, out Uri? uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException($"Provider '{providerName}' returned a non-HTTP result URL.");
            }
        }
    }

    private static int ReadBatchSetting(string name, int defaultValue)
    {
        string? value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, out int parsedValue) || parsedValue < 0)
        {
            throw new InvalidOperationException($"Environment variable '{name}' must be a non-negative integer.");
        }

        return parsedValue;
    }

    private async Task<IReadOnlyList<SearchResult>> SearchJsonProviderAsync(
        string providerName,
        string queryText)
    {
        JsonApiSearchDefinition definition = Assert.Single(
            JsonApiProviderCatalog.Definitions,
            item => item.Name == providerName);
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
            Assert.True(
                uri!.Scheme == Uri.UriSchemeHttp
                || uri.Scheme == Uri.UriSchemeHttps);
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
