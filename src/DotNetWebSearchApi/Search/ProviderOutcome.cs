using System.Net;
using System.Text.Json;
using System.Xml;

namespace DotNetWebSearchApi.Search;

public enum ProviderOutcomeStatus
{
    Succeeded,
    Failed
}

public enum ProviderFailureKind
{
    Timeout,
    RateLimited,
    TransientHttp,
    InvalidResponse,
    Authentication,
    AccessDenied,
    Challenge,
    Configuration,
    Unknown
}

public sealed record ProviderOutcome(
    string ProviderName,
    ProviderOutcomeStatus Status,
    ProviderFailureKind? FailureKind = null,
    TimeSpan? RetryAfter = null);

public sealed class ProviderFailureException : Exception
{
    public ProviderFailureException(
        ProviderFailureKind failureKind,
        string message,
        TimeSpan? retryAfter = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        RetryAfter = retryAfter;
    }

    public ProviderFailureKind FailureKind { get; }

    public TimeSpan? RetryAfter { get; }
}

internal static class ProviderFailureClassifier
{
    public static ProviderOutcome CreateFailure(string providerName, Exception exception, bool timedOut)
    {
        if (exception is ProviderFailureException providerFailure)
        {
            return new ProviderOutcome(
                providerName,
                ProviderOutcomeStatus.Failed,
                providerFailure.FailureKind,
                providerFailure.RetryAfter);
        }

        ProviderFailureKind failureKind = Classify(exception, timedOut);
        return new ProviderOutcome(providerName, ProviderOutcomeStatus.Failed, failureKind);
    }

    private static ProviderFailureKind Classify(Exception exception, bool timedOut)
    {
        if (timedOut || exception is OperationCanceledException)
        {
            return ProviderFailureKind.Timeout;
        }

        if (exception is HttpRequestException httpException)
        {
            return ClassifyHttpFailure(httpException.StatusCode);
        }

        if (exception is JsonException or XmlException or FormatException or InvalidDataException or InvalidOperationException)
        {
            return ProviderFailureKind.InvalidResponse;
        }

        return ProviderFailureKind.Unknown;
    }

    private static ProviderFailureKind ClassifyHttpFailure(HttpStatusCode? statusCode)
    {
        if (statusCode is null)
        {
            return ProviderFailureKind.TransientHttp;
        }

        int status = (int)statusCode.Value;
        if (status == 401)
        {
            return ProviderFailureKind.Authentication;
        }

        if (status == 403)
        {
            return ProviderFailureKind.AccessDenied;
        }

        if (status == 429)
        {
            return ProviderFailureKind.RateLimited;
        }

        if (status is 408 or 425 || status >= 500)
        {
            return ProviderFailureKind.TransientHttp;
        }

        return ProviderFailureKind.Unknown;
    }
}
