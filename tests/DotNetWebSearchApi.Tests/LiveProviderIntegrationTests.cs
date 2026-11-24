using DotNetWebSearchApi.Search;
using Microsoft.AspNetCore.Mvc.Testing;
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

    private async Task CheckProviderAsync(ISearchProvider provider, ICollection<string> failures)
    {
        using CancellationTokenSource timeout = new(provider.Timeout);
        try
        {
            IReadOnlyList<SearchResult> results = await provider.SearchAsync(
                new SearchQuery("dotnet", 3),
                timeout.Token);
            ValidateResults(provider.Name, results);
            output.WriteLine(
                "PASS {0}: {1} results. {2}",
                provider.Name,
                results.Count,
                string.Join(" | ", results.Take(3).Select(result => result.Title)));
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
