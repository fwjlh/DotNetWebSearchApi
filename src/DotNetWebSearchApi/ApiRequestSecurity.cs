using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotNetWebSearchApi;

internal sealed record ApiRequestSecurityOptions(
    IReadOnlyList<string> ApiKeys,
    PartitionedRateLimiter<HttpContext> AuthenticationFailureLimiter,
    PartitionedRateLimiter<string> PerKeySearchLimiter);

internal static class ApiRequestSecurity
{
    private const int MaximumFormBodyBytes = 16 * 1024;
    public static ApiRequestSecurityOptions Configure(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        string[] apiKeys = (configuration["SEARCH_API_KEYS"] ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (apiKeys.Any(key => Encoding.UTF8.GetByteCount(key) < 32))
        {
            throw new InvalidOperationException("Every SEARCH_API_KEYS entry must contain at least 32 UTF-8 bytes.");
        }
        if (!environment.IsDevelopment() && apiKeys.Length == 0)
        {
            throw new InvalidOperationException("SEARCH_API_KEYS must be configured before running outside Development.");
        }

        string[] trustedProxyValues = (configuration["TRUSTED_PROXY_IPS"] ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        IPAddress[] trustedProxies = trustedProxyValues
            .Select(value => IPAddress.TryParse(value, out IPAddress? address)
                ? address
                : throw new InvalidOperationException($"TRUSTED_PROXY_IPS contains an invalid IP address: '{value}'."))
            .Distinct()
            .ToArray();
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (IPAddress proxy in trustedProxies)
            {
                options.KnownProxies.Add(proxy);
                if (proxy.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    options.KnownProxies.Add(proxy.MapToIPv6());
                }
                else if (proxy.IsIPv4MappedToIPv6)
                {
                    options.KnownProxies.Add(proxy.MapToIPv4());
                }
            }
        });

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                AddRetryAfterHeader(context.HttpContext.Response, context.Lease);
                return ValueTask.CompletedTask;
            };
            options.AddPolicy("search", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    GetClientIpPartitionKey(context.Connection.RemoteIpAddress),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 60,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });
        services.Configure<FormOptions>(options =>
        {
            options.BufferBodyLengthLimit = 16 * 1024;
            options.KeyLengthLimit = 128;
            options.ValueCountLimit = 32;
            options.ValueLengthLimit = 4096;
            options.MultipartBodyLengthLimit = 16 * 1024;
        });

        PartitionedRateLimiter<HttpContext> authenticationFailureLimiter =
            PartitionedRateLimiter.Create<HttpContext, string>(
                context => RateLimitPartition.GetFixedWindowLimiter(
                    GetClientIpPartitionKey(context.Connection.RemoteIpAddress),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));

        PartitionedRateLimiter<string> perKeySearchLimiter = CreatePerKeySearchLimiter();
        services.AddSingleton(authenticationFailureLimiter);
        services.AddSingleton(perKeySearchLimiter);
        return new ApiRequestSecurityOptions(apiKeys, authenticationFailureLimiter, perKeySearchLimiter);
    }

    private static void AddRetryAfterHeader(HttpResponse response, RateLimitLease lease)
    {
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            int retrySeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            response.Headers["Retry-After"] = retrySeconds.ToString(CultureInfo.InvariantCulture);
        }
    }

    internal static bool IsSecureTransport(bool isHttps, IPAddress? remoteAddress, bool allowLoopbackHttp)
        => isHttps || (allowLoopbackHttp && remoteAddress is not null && IPAddress.IsLoopback(remoteAddress));

    internal static string GetClientIpPartitionKey(IPAddress? address)
    {
        if (address?.IsIPv4MappedToIPv6 == true)
        {
            address = address.MapToIPv4();
        }

        return address?.ToString() ?? "unknown";
    }

    internal static PartitionedRateLimiter<string> CreatePerKeySearchLimiter(int permitLimit = 60)
    {
        if (permitLimit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(permitLimit));
        }

        return PartitionedRateLimiter.Create<string, string>(
            apiKey => RateLimitPartition.GetFixedWindowLimiter(
                Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey))),
                _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = permitLimit,
                    QueueLimit = 0,
                    Window = TimeSpan.FromMinutes(1)
                }));
    }

    public static void UseAuthentication(
        WebApplication app,
        ApiRequestSecurityOptions options,
        bool allowLoopbackHttp)
    {
        app.Use(async (context, next) =>
        {
            string? routePattern = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText;
            bool isFormSearchPost = string.Equals(routePattern, "/search", StringComparison.OrdinalIgnoreCase)
                && HttpMethods.IsPost(context.Request.Method);
            if (isFormSearchPost)
            {
                if (context.Request.ContentLength > MaximumFormBodyBytes)
                {
                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    return;
                }

                IHttpMaxRequestBodySizeFeature? bodySizeFeature =
                    context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                if (bodySizeFeature is { IsReadOnly: false })
                {
                    bodySizeFeature.MaxRequestBodySize = MaximumFormBodyBytes;
                }
            }

            bool isSearchRequest =
                string.Equals(routePattern, "/search", StringComparison.OrdinalIgnoreCase)
                || string.Equals(routePattern, "/api/search", StringComparison.OrdinalIgnoreCase)
                || (string.Equals(routePattern, "/", StringComparison.OrdinalIgnoreCase)
                    && context.Request.Query.ContainsKey("q"));

            if (isSearchRequest && options.ApiKeys.Count > 0)
            {
                if (!IsSecureTransport(context.Request.IsHttps, context.Connection.RemoteIpAddress, allowLoopbackHttp))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "HTTPS is required for remote API-key requests."
                    });
                    return;
                }

                string? suppliedKey = context.Request.Headers["Authorization"]
                    .FirstOrDefault(value => value is not null && value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))?.Substring(7);
                byte[] suppliedBytes = Encoding.UTF8.GetBytes(suppliedKey ?? string.Empty);
                bool authorized = options.ApiKeys.Any(configuredKey =>
                    CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(configuredKey),
                        suppliedBytes));
                if (!authorized)
                {
                    using RateLimitLease failureLease = await options.AuthenticationFailureLimiter.AcquireAsync(
                        context,
                        1,
                        context.RequestAborted);
                    if (!failureLease.IsAcquired)
                    {
                        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                        AddRetryAfterHeader(context.Response, failureLease);
                        return;
                    }

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers["WWW-Authenticate"] = "Bearer";
                    await context.Response.WriteAsJsonAsync(new { error = "A valid API key is required." });
                    return;
                }

                using RateLimitLease keyLease = await options.PerKeySearchLimiter.AcquireAsync(
                    suppliedKey!,
                    1,
                    context.RequestAborted);
                if (!keyLease.IsAcquired)
                {
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    AddRetryAfterHeader(context.Response, keyLease);
                    return;
                }
            }

            await next();
        });
    }
}
