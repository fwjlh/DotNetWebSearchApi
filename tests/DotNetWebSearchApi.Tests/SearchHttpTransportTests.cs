using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using DotNetWebSearchApi.Search;
using Xunit;

namespace DotNetWebSearchApi.Tests;

public sealed class SearchHttpTransportTests
{
    [Fact]
    public async Task SendAsync_ReturnsBodyHeadersAndRetryAfter()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
        {
            HttpResponseMessage response = new(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent("rate limited")
            };
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(20));
            response.Headers.TryAddWithoutValidation("X-Provider-State", "limited");
            return response;
        }));
        SearchHttpTransport transport = new();
        using HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/search");

        SearchHttpResponse response = await transport.SendAsync(client, request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal("rate limited", response.Content);
        Assert.Equal(TimeSpan.FromSeconds(20), response.RetryAfter);
        Assert.True(response.TryGetHeader("X-Provider-State", out IReadOnlyList<string>? values));
        Assert.Equal("limited", Assert.Single(values!));
    }

    [Theory]
    [InlineData(401, ProviderFailureKind.Authentication)]
    [InlineData(403, ProviderFailureKind.AccessDenied)]
    [InlineData(429, ProviderFailureKind.RateLimited)]
    [InlineData(503, ProviderFailureKind.TransientHttp)]
    public async Task EnsureSuccessStatusCode_MapsHttpFailure(int statusCode, ProviderFailureKind expectedKind)
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ => new HttpResponseMessage((HttpStatusCode)statusCode)));
        SearchHttpTransport transport = new();
        using HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/search");
        SearchHttpResponse response = await transport.SendAsync(client, request, CancellationToken.None);

        ProviderFailureException exception = Assert.Throws<ProviderFailureException>(
            () => response.EnsureSuccessStatusCode("fixture"));
        Assert.Equal(expectedKind, exception.FailureKind);
    }

    [Fact]
    public async Task SendAsync_RejectsResponseLargerThanConfiguredLimit()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("01234567890") }));
        SearchHttpTransport transport = new(new SearchHttpTransportOptions { MaximumResponseBytes = 10 });
        using HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/search");

        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(
            () => transport.SendAsync(client, request, CancellationToken.None));

        Assert.Equal(ProviderFailureKind.InvalidResponse, exception.FailureKind);
    }

    [Fact]
    public async Task SendAsync_EnforcesPerHostConcurrencyLimit()
    {
        int active = 0;
        int maximumActive = 0;
        using HttpClient client = new(new AsyncStubHttpMessageHandler(async (_, cancellationToken) =>
        {
            int current = Interlocked.Increment(ref active);
            int observed = Volatile.Read(ref maximumActive);
            while (current > observed)
            {
                int previous = Interlocked.CompareExchange(ref maximumActive, current, observed);
                if (previous == observed) break;
                observed = previous;
            }
            await Task.Delay(20, cancellationToken);
            Interlocked.Decrement(ref active);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        SearchHttpTransport transport = new(new SearchHttpTransportOptions
        {
            MaximumConcurrentRequestsPerHost = 1
        });

        Task<SearchHttpResponse>[] requests = Enumerable.Range(0, 5)
            .Select(_ => SendAsync(transport, client))
            .ToArray();
        await Task.WhenAll(requests);

        Assert.Equal(1, maximumActive);
    }


    [Theory]
    [InlineData("ftp://example.com/search")]
    [InlineData("http://localhost/search")]
    [InlineData("http://service.internal/search")]
    [InlineData("http://127.0.0.1/search")]
    [InlineData("http://10.0.0.1/search")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://[::1]/search")]
    [InlineData("https://user:password@example.com/search")]
    public async Task SendAsync_RejectsUnsafeRequestDestinations(string address)
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("The HTTP handler must not be called for an unsafe URI.")));
        SearchHttpTransport transport = new();
        using HttpRequestMessage request = new(HttpMethod.Get, address);

        ProviderFailureException exception = await Assert.ThrowsAsync<ProviderFailureException>(
            () => transport.SendAsync(client, request, CancellationToken.None));

        Assert.Equal(ProviderFailureKind.InvalidResponse, exception.FailureKind);
    }

    [Fact]
    public async Task SendAsync_AllowsPublicIpLiteral()
    {
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)));
        SearchHttpTransport transport = new();
        using HttpRequestMessage request = new(HttpMethod.Get, "https://8.8.8.8/search");

        SearchHttpResponse response = await transport.SendAsync(client, request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }


    [Theory]
    [InlineData("0.0.0.0", false)]
    [InlineData("10.0.0.1", false)]
    [InlineData("100.64.0.1", false)]
    [InlineData("127.0.0.1", false)]
    [InlineData("169.254.169.254", false)]
    [InlineData("172.16.0.1", false)]
    [InlineData("192.168.1.1", false)]
    [InlineData("198.18.0.1", false)]
    [InlineData("192.88.99.1", false)]
    [InlineData("224.0.0.1", false)]
    [InlineData("8.8.8.8", true)]
    [InlineData("1.1.1.1", true)]
    [InlineData("::1", false)]
    [InlineData("fc00::1", false)]
    [InlineData("fe80::1", false)]
    [InlineData("2001:db8::1", false)]
    [InlineData("2001:20::1", false)]
    [InlineData("2001:20:f::1", false)]
    [InlineData("2001:0::1", false)]
    [InlineData("2002::1", false)]
    [InlineData("3fff::1", false)]
    [InlineData("2606:4700:4700::1111", true)]
    public void PublicNetworkPolicy_ClassifiesAddressRanges(string value, bool expectedPublic)
    {
        Assert.Equal(expectedPublic, PublicNetworkSocketsHandler.IsPublicAddress(IPAddress.Parse(value)));
    }

    [Fact]
    public void PublicNetworkHandler_DisablesProxyAndValidatesEveryConnection()
    {
        using SocketsHttpHandler handler = PublicNetworkSocketsHandler.Create();

        Assert.False(handler.UseProxy);
        Assert.True(handler.AllowAutoRedirect);
        Assert.NotNull(handler.ConnectCallback);
    }

    private static async Task<SearchHttpResponse> SendAsync(
        SearchHttpTransport transport,
        HttpClient client,
        string? providerName = null,
        TimeSpan? minimumInterval = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/search");
        return await transport.SendAsync(client, request, CancellationToken.None, providerName, minimumInterval);
    }

    [Fact]
    public async Task SendAsync_AppliesDefaultHeadersWhenProviderDoesNotSpecifyThem()
    {
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            Assert.Contains(request.Headers.UserAgent, item => item.Product?.Name == "DotNetWebSearchApi");
            Assert.Contains(request.Headers.Accept, item => item.MediaType == "*/*");
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        SearchHttpTransport transport = new();
        using HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/search");

        await transport.SendAsync(client, request, CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_PreservesProviderSpecificHeaders()
    {
        using HttpClient client = new(new StubHttpMessageHandler(request =>
        {
            Assert.Contains(request.Headers.UserAgent, item => item.Product?.Name == "ProviderClient");
            Assert.Contains(request.Headers.Accept, item => item.MediaType == "text/html");
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        SearchHttpTransport transport = new();
        using HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/search");
        request.Headers.UserAgent.ParseAdd("ProviderClient/2.0");
        request.Headers.Accept.ParseAdd("text/html");

        await transport.SendAsync(client, request, CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_AppliesMinimumIntervalPerProvider()
    {
        ConcurrentQueue<long> requestTimes = new();
        Stopwatch clock = Stopwatch.StartNew();
        using HttpClient client = new(new AsyncStubHttpMessageHandler((_, _) =>
        {
            requestTimes.Enqueue(clock.ElapsedMilliseconds);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }));
        SearchHttpTransport transport = new();

        await Task.WhenAll(
            SendAsync(transport, client, "slow-provider", TimeSpan.FromMilliseconds(60)),
            SendAsync(transport, client, "slow-provider", TimeSpan.FromMilliseconds(60)));

        long[] times = requestTimes.OrderBy(value => value).ToArray();
        Assert.Equal(2, times.Length);
        Assert.True(times[1] - times[0] >= 45);
    }

    [Fact]
    public async Task SendAsync_DecodesDeclaredResponseCharset()
    {
        byte[] content = System.Text.Encoding.Latin1.GetBytes("café");
        using HttpClient client = new(new StubHttpMessageHandler(_ =>
        {
            ByteArrayContent body = new(content);
            body.Headers.ContentType = new MediaTypeHeaderValue("text/plain") { CharSet = "iso-8859-1" };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = body };
        }));
        SearchHttpTransport transport = new();
        using HttpRequestMessage request = new(HttpMethod.Get, "https://example.com/search");

        SearchHttpResponse response = await transport.SendAsync(client, request, CancellationToken.None);

        Assert.Equal("café", response.Content);
    }

    private sealed class AsyncStubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return responseFactory(request, cancellationToken);
        }
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
