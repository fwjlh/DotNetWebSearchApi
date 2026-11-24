using System.Threading.RateLimiting;
using DotNetWebSearchApi;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class ApiRequestSecurityRateLimitTests
{
    [Fact]
    public void PerKeyLimiter_EnforcesIndependentQuotasForEachKey()
    {
        using PartitionedRateLimiter<string> limiter = ApiRequestSecurity.CreatePerKeySearchLimiter(2);

        using RateLimitLease first = limiter.AttemptAcquire("alpha-key");
        using RateLimitLease second = limiter.AttemptAcquire("alpha-key");
        using RateLimitLease blocked = limiter.AttemptAcquire("alpha-key");
        using RateLimitLease otherKey = limiter.AttemptAcquire("beta-key");

        Assert.True(first.IsAcquired);
        Assert.True(second.IsAcquired);
        Assert.False(blocked.IsAcquired);
        Assert.True(otherKey.IsAcquired);
    }

    [Fact]
    public void PerKeyLimiter_RejectsInvalidPermitLimit()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => ApiRequestSecurity.CreatePerKeySearchLimiter(0));
    }
}
