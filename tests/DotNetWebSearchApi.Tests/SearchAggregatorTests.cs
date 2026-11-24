using System.Net;
using System.Text.Json;
using Xunit;
using DotNetWebSearchApi.Search;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetWebSearchApi.Tests;

public sealed class SearchAggregatorTests
{
    [Fact]
    public async Task SearchAsync_ReturnsResultsFromAllProviders()
    {
        SearchAggregator aggregator = CreateAggregator(
            Provider("alpha", Result("Alpha", "https://example.com/a")),
            Provider("beta", Result("Beta", "https://example.com/b")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, result => result.Title == "Alpha");
        Assert.Contains(results, result => result.Title == "Beta");
    }

    [Fact]
    public async Task SearchAsync_DeduplicatesUrlsAndKeepsHighestScore()
    {
        SearchAggregator aggregator = CreateAggregator(
            Provider("first", Result("Lower", "https://example.com/item#part", 0.2)),
            Provider("second", Result("Higher", "https://example.com/item/", 0.9)));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        SearchResult result = Assert.Single(results);
        Assert.Equal("Higher", result.Title);
        Assert.Equal(new[] { "first", "second" }, result.Engines.OrderBy(name => name));
        Assert.Equal(1, result.Positions["first"]);
        Assert.Equal(1, result.Positions["second"]);
    }

    [Fact]
    public async Task SearchAsync_SortsByProviderPositionAndUsesTitleAsTieBreaker()
    {
        SearchAggregator aggregator = CreateAggregator(
            Provider("one",
                Result("Zulu", "https://example.com/z"),
                Result("Alpha", "https://example.com/a")),
            Provider("two", Result("Beta", "https://example.com/b")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Equal(new[] { "Beta", "Zulu", "Alpha" }, results.Select(result => result.Title));
        Assert.Equal(new[] { 1.0, 1.0, 0.5 }, results.Select(result => result.Score));
    }

    [Fact]
    public async Task SearchAsync_AppliesConfiguredEngineWeightsToMergedPositions()
    {
        SearchAggregator aggregator = CreateAggregator(
            new CapabilityProvider(
                "heavy",
                new EngineCapabilities(
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general" },
                    false, 1, false, false, false, 2.0),
                (_, _) => Task.FromResult<IReadOnlyList<SearchResult>>(
                    new[] { Result("Shared", "https://example.com/shared") })),
            new CapabilityProvider(
                "light",
                new EngineCapabilities(
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general" },
                    false, 1, false, false, false, 1.0),
                (_, _) => Task.FromResult<IReadOnlyList<SearchResult>>(
                    new[] { Result("Shared", "https://example.com/shared") })));

        SearchResult result = Assert.Single(await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None));

        Assert.Equal(8.0, result.Score);
        Assert.Equal(new[] { "heavy", "light" }, result.Engines.OrderBy(name => name));
    }

    [Fact]
    public async Task SearchAsync_RespectsRequestedLimit()
    {
        SearchAggregator aggregator = CreateAggregator(
            Provider("one",
                Result("One", "https://example.com/1"),
                Result("Two", "https://example.com/2"),
                Result("Three", "https://example.com/3")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 2),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task SearchAsync_IgnoresProviderFailureAndKeepsOtherResults()
    {
        SearchAggregator aggregator = CreateAggregator(
            new TestProvider("broken", (_, _) => throw new InvalidOperationException("failure")),
            Provider("working", Result("Available", "https://example.com/ok")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Equal("Available", Assert.Single(results).Title);
    }

    [Fact]
    public async Task SearchAsync_TreatsProviderTimeoutAsAnEmptyProviderResult()
    {
        TestProvider slowProvider = new(
            "slow",
            async (_, cancellationToken) =>
            {
                await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken);
                return Array.Empty<SearchResult>();
            },
            TimeSpan.FromMilliseconds(20));
        SearchAggregator aggregator = CreateAggregator(slowProvider);

        SearchExecutionResult execution = await aggregator.SearchDetailedAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Empty(execution.Results);
        Assert.Equal(ProviderFailureKind.Timeout, Assert.Single(execution.ProviderOutcomes).FailureKind);
    }

    [Theory]
    [InlineData(401, ProviderFailureKind.Authentication)]
    [InlineData(403, ProviderFailureKind.AccessDenied)]
    [InlineData(429, ProviderFailureKind.RateLimited)]
    [InlineData(503, ProviderFailureKind.TransientHttp)]
    public async Task SearchDetailedAsync_ClassifiesHttpFailures(int statusCode, ProviderFailureKind expectedKind)
    {
        TestProvider provider = new(
            "http",
            (_, _) => throw new HttpRequestException("upstream failure", null, (HttpStatusCode)statusCode));
        SearchAggregator aggregator = CreateAggregator(provider);

        SearchExecutionResult execution = await aggregator.SearchDetailedAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        ProviderOutcome outcome = Assert.Single(execution.ProviderOutcomes);
        Assert.Equal(ProviderOutcomeStatus.Failed, outcome.Status);
        Assert.Equal(expectedKind, outcome.FailureKind);
        Assert.Equal("http", Assert.Single(execution.UnresponsiveProviders));
    }

    [Fact]
    public async Task SearchDetailedAsync_ClassifiesMalformedProviderResponse()
    {
        TestProvider provider = new("malformed", (_, _) => throw new JsonException("invalid JSON"));
        SearchAggregator aggregator = CreateAggregator(provider);

        SearchExecutionResult execution = await aggregator.SearchDetailedAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Equal(ProviderFailureKind.InvalidResponse, Assert.Single(execution.ProviderOutcomes).FailureKind);
    }

    [Fact]
    public async Task SearchDetailedAsync_PreservesTypedChallengeAndRetryAfter()
    {
        TimeSpan retryAfter = TimeSpan.FromSeconds(15);
        TestProvider provider = new(
            "challenge",
            (_, _) => throw new ProviderFailureException(
                ProviderFailureKind.Challenge,
                "Verification required",
                retryAfter));
        SearchAggregator aggregator = CreateAggregator(provider);

        SearchExecutionResult execution = await aggregator.SearchDetailedAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        ProviderOutcome outcome = Assert.Single(execution.ProviderOutcomes);
        Assert.Equal(ProviderFailureKind.Challenge, outcome.FailureKind);
        Assert.Equal(retryAfter, outcome.RetryAfter);
    }

    [Fact]
    public async Task SearchDetailedAsync_ReportsSuccessfulEmptyResponseAsSuccess()
    {
        SearchAggregator aggregator = CreateAggregator(
            new TestProvider("empty", (_, _) =>
                Task.FromResult<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>())));

        SearchExecutionResult execution = await aggregator.SearchDetailedAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Empty(execution.UnresponsiveProviders);
        Assert.Equal(ProviderOutcomeStatus.Succeeded, Assert.Single(execution.ProviderOutcomes).Status);
    }

    [Fact]
    public async Task SearchAsync_PropagatesCallerCancellation()
    {
        using CancellationTokenSource cancellation = new();
        TestProvider waitingProvider = new(
            "waiting",
            async (_, cancellationToken) =>
            {
                await Task.Delay(System.Threading.Timeout.Infinite, cancellationToken);
                return Array.Empty<SearchResult>();
            });
        ProviderHealthRegistry health = new();
        SearchAggregator aggregator = new(
            new[] { waitingProvider },
            NullLogger<SearchAggregator>.Instance,
            health: health);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            aggregator.SearchAsync(new SearchQuery("test", 10), cancellation.Token));

        ProviderHealthSnapshot snapshot = Assert.Single(health.GetSnapshot(new[] { "waiting" }));
        Assert.Equal(ProviderHealthStatus.Closed, snapshot.Status);
        Assert.Equal(0, snapshot.ConsecutiveFailures);
    }

    [Fact]
    public async Task SearchDetailedAsync_ReportsUnresponsiveProviders()
    {
        SearchAggregator aggregator = CreateAggregator(
            new TestProvider("broken", (_, _) => throw new InvalidOperationException("failure")),
            Provider("working", Result("Available", "https://example.com/ok")));

        SearchExecutionResult execution = await aggregator.SearchDetailedAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Single(execution.Results);
        Assert.Equal("broken", Assert.Single(execution.UnresponsiveProviders));
    }

    [Fact]
    public async Task SearchDetailedAsync_RunsOnlyRequestedProviders()
    {
        int alphaCalls = 0;
        int betaCalls = 0;
        SearchAggregator aggregator = CreateAggregator(
            new TestProvider("alpha", (_, _) =>
            {
                alphaCalls++;
                return Task.FromResult<IReadOnlyList<SearchResult>>(new[] { Result("Alpha", "https://example.com/a") });
            }),
            new TestProvider("beta", (_, _) =>
            {
                betaCalls++;
                return Task.FromResult<IReadOnlyList<SearchResult>>(new[] { Result("Beta", "https://example.com/b") });
            }));

        SearchExecutionResult execution = await aggregator.SearchDetailedAsync(
            new SearchQuery("test", 10, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "alpha" }),
            CancellationToken.None);

        Assert.Equal(1, alphaCalls);
        Assert.Equal(0, betaCalls);
        Assert.Equal("Alpha", Assert.Single(execution.Results).Title);
    }

    [Fact]
    public async Task SearchDetailedAsync_SkipsProvidersThatDoNotSupportRequestedPage()
    {
        int capableCalls = 0;
        int legacyCalls = 0;
        SearchAggregator aggregator = CreateAggregator(
            new CapabilityProvider("paged", new EngineCapabilities(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "general" },
                true, 3, false, false, false),
                (_, _) =>
                {
                    capableCalls++;
                    return Task.FromResult<IReadOnlyList<SearchResult>>(
                        new[] { Result("Paged", "https://example.com/paged") });
                }),
            new TestProvider("legacy", (_, _) =>
            {
                legacyCalls++;
                return Task.FromResult<IReadOnlyList<SearchResult>>(Array.Empty<SearchResult>());
            }));

        SearchExecutionResult execution = await aggregator.SearchDetailedAsync(
            new SearchQuery("test", 10) { Page = 2 },
            CancellationToken.None);

        Assert.Equal(1, capableCalls);
        Assert.Equal(0, legacyCalls);
        Assert.Equal("Paged", Assert.Single(execution.Results).Title);
        Assert.Equal("legacy", Assert.Single(execution.UnsupportedProviders));
        Assert.Empty(execution.UnresponsiveProviders);
    }

    [Fact]
    public async Task SearchAsync_ReturnsEmptyForBlankQuery()
    {
        SearchAggregator aggregator = CreateAggregator(Provider("one", Result("One", "https://example.com/1")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery(" ", 10),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_MergesCaseInsensitiveDuplicateProviderPositions()
    {
        SearchResult result = Result("Positioned", "https://example.com/positioned") with
        {
            Engines = new[] { "alpha", "ALPHA" },
            Positions = new Dictionary<string, int>
            {
                ["alpha"] = 3,
                ["ALPHA"] = 1,
                ["beta"] = 0
            }
        };
        SearchAggregator aggregator = CreateAggregator(Provider("alpha", result));

        SearchResult merged = Assert.Single(await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None));

        Assert.Equal(1, merged.Positions["alpha"]);
        Assert.Single(merged.Positions);
        Assert.Single(merged.Engines);
    }

    [Fact]
    public async Task SearchAsync_RejectsCredentialBearingUrlsAndBlankTitles()
    {
        SearchAggregator aggregator = CreateAggregator(
            Provider("one",
                Result("Credential URL", "https://user:password@example.com/secret"),
                Result("  ", "https://example.com/blank-title"),
                Result("Safe", "https://example.com/safe")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Equal("Safe", Assert.Single(results).Title);
    }

    [Fact]
    public async Task SearchAsync_RejectsNonHttpUrls()
    {
        SearchAggregator aggregator = CreateAggregator(
            Provider("one", Result("File", "file:///etc/passwd")));

        IReadOnlyList<SearchResult> results = await aggregator.SearchAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Empty(results);
    }

    private static SearchAggregator CreateAggregator(params ISearchProvider[] providers)
    {
        return new SearchAggregator(providers, NullLogger<SearchAggregator>.Instance);
    }

    private static TestProvider Provider(string name, params SearchResult[] results)
    {
        return new TestProvider(name, (_, _) => Task.FromResult<IReadOnlyList<SearchResult>>(results));
    }

    private static SearchResult Result(string title, string url, double score = 0.5)
    {
        return new SearchResult(title, url, title, "test", score);
    }

    private sealed class CapabilityProvider(
        string name,
        EngineCapabilities capabilities,
        Func<SearchQuery, CancellationToken, Task<IReadOnlyList<SearchResult>>> search)
        : ISearchProvider, ISearchProviderCapabilities
    {
        public string Name => name;

        public TimeSpan Timeout => TimeSpan.FromSeconds(2);

        public EngineCapabilities Capabilities => capabilities;

        public Task<IReadOnlyList<SearchResult>> SearchAsync(
            SearchQuery query,
            CancellationToken cancellationToken)
        {
            return search(query, cancellationToken);
        }
    }

    private sealed class TestProvider(
        string name,
        Func<SearchQuery, CancellationToken, Task<IReadOnlyList<SearchResult>>> search,
        TimeSpan? timeout = null) : ISearchProvider
    {
        public string Name => name;

        public TimeSpan Timeout => timeout ?? TimeSpan.FromSeconds(2);

        public Task<IReadOnlyList<SearchResult>> SearchAsync(
            SearchQuery query,
            CancellationToken cancellationToken)
        {
            return search(query, cancellationToken);
        }
    }
}
