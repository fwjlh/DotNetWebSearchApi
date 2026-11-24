using System.Net;
using System.Threading.RateLimiting;
using DotNetWebSearchApi;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class ApiRequestSecurityRateLimitTests
{
    [Theory]
    [InlineData(true, "203.0.113.10", false, true)]
    [InlineData(false, "127.0.0.1", true, true)]
    [InlineData(false, "::ffff:127.0.0.1", true, true)]
    [InlineData(false, "127.0.0.1", false, false)]
    [InlineData(false, "203.0.113.10", true, false)]
    public void SecureTransport_RequiresHttpsForRemoteAddresses(
        bool isHttps,
        string remoteAddress,
        bool allowLoopbackHttp,
        bool expected)
    {
        Assert.Equal(
            expected,
            ApiRequestSecurity.IsSecureTransport(isHttps, IPAddress.Parse(remoteAddress), allowLoopbackHttp));
    }

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
    [Fact]
    public void BoundedPartitionSelector_SendsNewKeysToOverflowAfterCapacity()
    {
        BoundedPartitionKeySelector selector = new(2);

        Assert.Equal("client-a", selector.GetPartitionKey("client-a"));
        Assert.Equal("client-b", selector.GetPartitionKey("client-b"));
        Assert.Equal("__partition_overflow__", selector.GetPartitionKey("client-c"));
        Assert.Equal("client-a", selector.GetPartitionKey("client-a"));
        Assert.Equal("__partition_overflow__", selector.GetPartitionKey("client-d"));
    }

    [Fact]
    public void BoundedPartitionSelector_ConcurrentNewKeysNeverExceedCapacity()
    {
        const int capacity = 2;
        const int clientCount = 100;
        const string overflowKey = "__partition_overflow__";
        BoundedPartitionKeySelector selector = new(capacity);
        string[] partitions = new string[clientCount];

        Parallel.For(0, clientCount, index =>
        {
            partitions[index] = selector.GetPartitionKey($"client-{index}");
        });

        HashSet<string> retainedPartitions = new(StringComparer.Ordinal);
        int overflowCount = 0;
        foreach (string partition in partitions)
        {
            if (partition == overflowKey)
            {
                overflowCount++;
            }
            else
            {
                retainedPartitions.Add(partition);
            }
        }

        Assert.Equal(capacity, retainedPartitions.Count);
        Assert.Equal(clientCount - capacity, overflowCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BoundedPartitionSelector_RejectsNonPositiveCapacity(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BoundedPartitionKeySelector(capacity));
    }

    [Fact]
    public void BoundedPartitionSelector_RejectsNullKey()
    {
        BoundedPartitionKeySelector selector = new(1);

        Assert.Throws<ArgumentNullException>(() => selector.GetPartitionKey(null!));
    }

}
