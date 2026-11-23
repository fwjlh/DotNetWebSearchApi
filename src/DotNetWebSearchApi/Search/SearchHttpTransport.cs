using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;

namespace DotNetWebSearchApi.Search;

public sealed record SearchHttpTransportOptions
{
    public int MaximumResponseBytes { get; init; } = 4 * 1024 * 1024;
    public int MaximumConcurrentRequestsPerHost { get; init; } = 4;
    public string DefaultUserAgent { get; init; } =
        "DotNetWebSearchApi/0.1 (+https://github.com/fwjlh/DotNetWebSearchApi)";
}

public sealed class SearchHttpResponse
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _headers;

    public SearchHttpResponse(
        HttpStatusCode statusCode,
        string content,
        Uri requestUri,
        TimeSpan? retryAfter,
        IReadOnlyDictionary<string, IReadOnlyList<string>> headers)
    {
        StatusCode = statusCode;
        Content = content;
        RequestUri = requestUri;
        RetryAfter = retryAfter;
        _headers = headers;
    }

    public HttpStatusCode StatusCode { get; }

    public string Content { get; }

    public Uri RequestUri { get; }

    public TimeSpan? RetryAfter { get; }

    public bool TryGetHeader(string name, out IReadOnlyList<string>? values)
    {
        return _headers.TryGetValue(name, out values);
    }

    public void EnsureSuccessStatusCode(string providerName)
    {
        if ((int)StatusCode is >= 200 and < 300)
        {
            return;
        }

        ProviderFailureKind failureKind = GetFailureKind(StatusCode);
        string message = $"{providerName} returned HTTP {(int)StatusCode}.";
        throw new ProviderFailureException(
            failureKind,
            message,
            RetryAfter,
            new HttpRequestException(message, null, StatusCode));
    }

    private static ProviderFailureKind GetFailureKind(HttpStatusCode statusCode)
    {
        int status = (int)statusCode;
        if (status == 401) return ProviderFailureKind.Authentication;
        if (status == 403) return ProviderFailureKind.AccessDenied;
        if (status == 429) return ProviderFailureKind.RateLimited;
        if (status is 408 or 425 || status >= 500) return ProviderFailureKind.TransientHttp;
        return ProviderFailureKind.Unknown;
    }
}

public sealed class SearchHttpTransport
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _hostGates =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ProviderRateState> _providerRates =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly SearchHttpTransportOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SearchHttpTransport>? _logger;

    public SearchHttpTransport(
        SearchHttpTransportOptions? options = null,
        TimeProvider? timeProvider = null,
        ILogger<SearchHttpTransport>? logger = null)
    {
        _options = options ?? new SearchHttpTransportOptions();
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger;
        if (_options.MaximumResponseBytes < 1
            || _options.MaximumConcurrentRequestsPerHost < 1
            || string.IsNullOrWhiteSpace(_options.DefaultUserAgent))
        {
            throw new ArgumentOutOfRangeException(nameof(options));
        }
    }

    public async Task<SearchHttpResponse> SendAsync(
        HttpClient client,
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        string? providerName = null,
        TimeSpan? minimumInterval = null)
    {
        Uri requestUri = request.RequestUri
            ?? throw new ArgumentException("The request URI is required.", nameof(request));
        ValidateRequestUri(requestUri);
        ApplyDefaultHeaders(client, request);
        await WaitForProviderSlotAsync(providerName, minimumInterval, cancellationToken);
        SemaphoreSlim gate = _hostGates.GetOrAdd(
            requestUri.IdnHost,
            _ => new SemaphoreSlim(_options.MaximumConcurrentRequestsPerHost));
        await gate.WaitAsync(cancellationToken);
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            using HttpResponseMessage response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            byte[] content = await ReadContentAsync(response.Content, cancellationToken);
            string body = DecodeContent(content, response.Content.Headers.ContentType?.CharSet);
            _logger?.LogDebug(
                "Search HTTP {Method} to {Host} returned {StatusCode} with {ResponseBytes} bytes in {ElapsedMilliseconds} ms.",
                request.Method.Method,
                requestUri.IdnHost,
                (int)response.StatusCode,
                content.Length,
                stopwatch.Elapsed.TotalMilliseconds);
            return new SearchHttpResponse(
                response.StatusCode,
                body,
                response.RequestMessage?.RequestUri ?? requestUri,
                GetRetryAfter(response.Headers.RetryAfter),
                CopyHeaders(response));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger?.LogWarning(
                "Search HTTP {Method} to {Host} failed with {ExceptionType} after {ElapsedMilliseconds} ms.",
                request.Method.Method,
                requestUri.IdnHost,
                exception.GetType().Name,
                stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
        finally
        {
            gate.Release();
        }
    }


    private static void ValidateRequestUri(Uri uri)
    {
        if (!uri.IsAbsoluteUri
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || string.IsNullOrWhiteSpace(uri.Host)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ProviderFailureException(
                ProviderFailureKind.InvalidResponse,
                "Provider request URI must be an absolute HTTP(S) URI without embedded credentials.");
        }

        string host = uri.IdnHost.TrimEnd('.');
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
        {
            throw new ProviderFailureException(
                ProviderFailureKind.InvalidResponse,
                "Provider request URI targets a local or internal host.");
        }

        if (IPAddress.TryParse(host, out IPAddress? address) && !PublicNetworkSocketsHandler.IsPublicAddress(address))
        {
            throw new ProviderFailureException(
                ProviderFailureKind.InvalidResponse,
                "Provider request URI targets a non-public IP address.");
        }
    }

    private void ApplyDefaultHeaders(HttpClient client, HttpRequestMessage request)
    {
        if (request.Headers.UserAgent.Count == 0 && client.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            request.Headers.UserAgent.ParseAdd(_options.DefaultUserAgent);
        }

        if (request.Headers.Accept.Count == 0 && client.DefaultRequestHeaders.Accept.Count == 0)
        {
            request.Headers.Accept.ParseAdd("*/*");
        }
    }

    private async Task WaitForProviderSlotAsync(
        string? providerName,
        TimeSpan? minimumInterval,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerName) || minimumInterval is not { } interval || interval <= TimeSpan.Zero)
        {
            return;
        }

        ProviderRateState state = _providerRates.GetOrAdd(providerName, _ => new ProviderRateState());
        await state.Gate.WaitAsync(cancellationToken);
        try
        {
            TimeSpan delay = state.NextRequestUtc - _timeProvider.GetUtcNow();
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, _timeProvider, cancellationToken);
            }

            state.NextRequestUtc = _timeProvider.GetUtcNow().Add(interval);
        }
        finally
        {
            state.Gate.Release();
        }
    }

    private async Task<byte[]> ReadContentAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > _options.MaximumResponseBytes)
        {
            throw new ProviderFailureException(
                ProviderFailureKind.InvalidResponse,
                "Provider response exceeded the configured size limit.");
        }

        await using Stream stream = await content.ReadAsStreamAsync(cancellationToken);
        using MemoryStream buffer = new();
        byte[] chunk = new byte[81920];
        while (true)
        {
            int read = await stream.ReadAsync(chunk.AsMemory(), cancellationToken);
            if (read == 0)
            {
                return buffer.ToArray();
            }

            if (buffer.Length + read > _options.MaximumResponseBytes)
            {
                throw new ProviderFailureException(
                    ProviderFailureKind.InvalidResponse,
                    "Provider response exceeded the configured size limit.");
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
    }

    private static string DecodeContent(byte[] content, string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset))
        {
            return Encoding.UTF8.GetString(content);
        }

        try
        {
            return Encoding.GetEncoding(charset.Trim('"')).GetString(content);
        }
        catch (ArgumentException exception)
        {
            throw new ProviderFailureException(
                ProviderFailureKind.InvalidResponse,
                "Provider response declared an unsupported character encoding.",
                innerException: exception);
        }
    }

    private TimeSpan? GetRetryAfter(System.Net.Http.Headers.RetryConditionHeaderValue? retryAfter)
    {
        if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
        {
            return delta;
        }

        if (retryAfter?.Date is { } date)
        {
            TimeSpan remaining = date - _timeProvider.GetUtcNow();
            return remaining > TimeSpan.Zero ? remaining : null;
        }

        return null;
    }

    private sealed class ProviderRateState
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public DateTimeOffset NextRequestUtc { get; set; } = DateTimeOffset.MinValue;
    }

    private static IReadOnlyDictionary<string, IReadOnlyList<string>> CopyHeaders(HttpResponseMessage response)
    {
        Dictionary<string, IReadOnlyList<string>> headers = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header in response.Content.Headers)
        {
            headers[header.Key] = header.Value.ToArray();
        }

        return headers;
    }
}
