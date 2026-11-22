using DotNetWebSearchApi.Search;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class ProviderHealthRegistryTests
{
    [Fact]
    public void TryAcquire_BlocksProviderUntilCooldownExpires()
    {
        TestTimeProvider clock = new(DateTimeOffset.Parse("2026-11-22T12:00:00Z"));
        ProviderHealthRegistry registry = new(
            new ProviderHealthPolicy { ChallengeCooldown = TimeSpan.FromSeconds(15) },
            clock);
        registry.RecordFailure("alpha", ProviderFailureKind.Challenge);

        ProviderHealthAdmission blocked = registry.TryAcquire("alpha");

        Assert.False(blocked.CanExecute);
        Assert.Equal(TimeSpan.FromSeconds(15), blocked.RetryAfter);
        Assert.Equal(ProviderHealthStatus.Open, blocked.Snapshot.Status);
    }

    [Fact]
    public void TryAcquire_AllowsOnlyOneHalfOpenProbe()
    {
        TestTimeProvider clock = new(DateTimeOffset.Parse("2026-11-22T12:00:00Z"));
        ProviderHealthRegistry registry = new(
            new ProviderHealthPolicy { ChallengeCooldown = TimeSpan.FromSeconds(5) },
            clock);
        registry.RecordFailure("alpha", ProviderFailureKind.Challenge);
        clock.Advance(TimeSpan.FromSeconds(5));

        ProviderHealthAdmission probe = registry.TryAcquire("alpha");
        ProviderHealthAdmission concurrent = registry.TryAcquire("alpha");

        Assert.True(probe.CanExecute);
        Assert.Equal(ProviderHealthStatus.HalfOpen, probe.Snapshot.Status);
        Assert.False(concurrent.CanExecute);
    }

    [Fact]
    public void RecordSuccess_ClosesHalfOpenCircuitAndResetsFailures()
    {
        TestTimeProvider clock = new(DateTimeOffset.Parse("2026-11-22T12:00:00Z"));
        ProviderHealthRegistry registry = new(
            new ProviderHealthPolicy { ChallengeCooldown = TimeSpan.FromSeconds(1) },
            clock);
        registry.RecordFailure("alpha", ProviderFailureKind.Challenge);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(registry.TryAcquire("alpha").CanExecute);

        registry.RecordSuccess("alpha");

        ProviderHealthSnapshot snapshot = Assert.Single(registry.GetSnapshot(new[] { "alpha" }));
        Assert.Equal(ProviderHealthStatus.Closed, snapshot.Status);
        Assert.Equal(0, snapshot.ConsecutiveFailures);
        Assert.Null(snapshot.LastFailureKind);
    }

    [Fact]
    public void RecordFailure_OpensCircuitAfterConfiguredTransientThreshold()
    {
        ProviderHealthRegistry registry = new(new ProviderHealthPolicy { FailureThreshold = 2 });
        registry.RecordFailure("alpha", ProviderFailureKind.TransientHttp);

        Assert.Equal(ProviderHealthStatus.Closed, Assert.Single(registry.GetSnapshot(new[] { "alpha" })).Status);

        registry.RecordFailure("alpha", ProviderFailureKind.TransientHttp);

        Assert.Equal(ProviderHealthStatus.Open, Assert.Single(registry.GetSnapshot(new[] { "alpha" })).Status);
    }

    [Fact]
    public void RecordFailure_ClampsRetryAfterToMaximumCooldown()
    {
        ProviderHealthRegistry registry = new(new ProviderHealthPolicy
        {
            MaximumCooldown = TimeSpan.FromSeconds(30)
        });
        registry.RecordFailure("alpha", ProviderFailureKind.RateLimited, TimeSpan.FromMinutes(10));

        ProviderHealthAdmission admission = registry.TryAcquire("alpha");

        Assert.False(admission.CanExecute);
        Assert.Equal(TimeSpan.FromSeconds(30), admission.RetryAfter);
    }

    [Fact]
    public async Task SearchAggregator_SkipsCoolingDownProviderAndRecoversAfterProbe()
    {
        TestTimeProvider clock = new(DateTimeOffset.Parse("2026-11-22T12:00:00Z"));
        ProviderHealthRegistry health = new(
            new ProviderHealthPolicy
            {
                FailureThreshold = 1,
                ChallengeCooldown = TimeSpan.FromSeconds(2)
            },
            clock);
        int calls = 0;
        TestProvider provider = new("alpha", (_, _) =>
        {
            calls++;
            if (calls == 1)
            {
                throw new ProviderFailureException(ProviderFailureKind.Challenge, "verification");
            }

            return Task.FromResult<IReadOnlyList<SearchResult>>(
                new[] { new SearchResult("Recovered", "https://example.com/recovered", "test", "alpha", 1) });
        });
        SearchAggregator aggregator = new(
            new[] { provider },
            NullLogger<SearchAggregator>.Instance,
            health: health);

        SearchExecutionResult failed = await aggregator.SearchDetailedAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);
        SearchExecutionResult coolingDown = await aggregator.SearchDetailedAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Equal("alpha", Assert.Single(failed.UnresponsiveProviders));
        Assert.Equal("alpha", Assert.Single(coolingDown.CoolingDownProviders));
        Assert.Equal(ProviderOutcomeStatus.Skipped, Assert.Single(coolingDown.ProviderOutcomes).Status);
        Assert.Equal(1, calls);

        clock.Advance(TimeSpan.FromSeconds(2));
        SearchExecutionResult recovered = await aggregator.SearchDetailedAsync(
            new SearchQuery("test", 10),
            CancellationToken.None);

        Assert.Equal("Recovered", Assert.Single(recovered.Results).Title);
        Assert.Empty(recovered.CoolingDownProviders);
        Assert.Equal(2, calls);
        Assert.Equal(ProviderHealthStatus.Closed, Assert.Single(health.GetSnapshot(new[] { "alpha" })).Status);
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

    private sealed class TestTimeProvider(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan duration) => _now = _now.Add(duration);
    }
}
