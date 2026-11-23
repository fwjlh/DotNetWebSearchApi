using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DotNetWebSearchApi;

internal sealed record ApiRequestSecurityOptions(IReadOnlyList<string> ApiKeys);

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

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("search", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
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

        return new ApiRequestSecurityOptions(apiKeys);
    }

    public static void UseAuthentication(
        WebApplication app,
        ApiRequestSecurityOptions options)
    {
        app.Use(async (context, next) =>
        {
            bool isFormSearchPost = context.Request.Path.Equals("/search", StringComparison.OrdinalIgnoreCase)
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
                (context.Request.Path.Equals("/search", StringComparison.OrdinalIgnoreCase)
                    && (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsPost(context.Request.Method)))
                || (context.Request.Path.Equals("/api/search", StringComparison.OrdinalIgnoreCase)
                    && HttpMethods.IsGet(context.Request.Method))
                || (context.Request.Path.Equals("/", StringComparison.OrdinalIgnoreCase)
                    && context.Request.Query.ContainsKey("q"));

            if (isSearchRequest && options.ApiKeys.Count > 0)
            {
                string? suppliedKey = context.Request.Headers["Authorization"]
                    .FirstOrDefault(value => value is not null && value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))?.Substring(7);
                byte[] suppliedBytes = Encoding.UTF8.GetBytes(suppliedKey ?? string.Empty);
                bool authorized = options.ApiKeys.Any(configuredKey =>
                    CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(configuredKey),
                        suppliedBytes));
                if (!authorized)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.Headers["WWW-Authenticate"] = "Bearer";
                    await context.Response.WriteAsJsonAsync(new { error = "A valid API key is required." });
                    return;
                }
            }

            await next();
        });
    }
}
