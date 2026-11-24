using System.Net;
using System.Threading.RateLimiting;
using DotNetWebSearchApi;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class ApiRequestSecurityRateLimitTests
{
    [Fact]
    public void ClientIpPartitionKey_NormalizesIpv4MappedIpv6()
    {
        IPAddress ipv4 = IPAddress.Parse("192.0.2.15");
        IPAddress mapped = IPAddress.Parse("::ffff:192.0.2.15");

        Assert.Equal(
            ApiRequestSecurity.GetClientIpPartitionKey(ipv4),
            ApiRequestSecurity.GetClientIpPartitionKey(mapped));
    }

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
