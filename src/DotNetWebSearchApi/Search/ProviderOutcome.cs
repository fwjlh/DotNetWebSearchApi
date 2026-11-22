using System.Net;
using System.Text.Json;
using System.Xml;
using System.Text.Json.Serialization;

namespace DotNetWebSearchApi.Search;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProviderOutcomeStatus
{
    Succeeded,
    Failed,
    Skipped
}

[JsonConverter(typeof(JsonStringEnumConverter))]
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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProviderChallengeKind
{
    TemporaryBlock,
    BrowserVerification,
    ProofOfWork,
    InteractiveCaptcha,
    InteractiveVerification,
    LoginRequired,
    Unknown
}

public sealed record ProviderOutcome(
    string ProviderName,
    ProviderOutcomeStatus Status,
    ProviderFailureKind? FailureKind = null,
    TimeSpan? RetryAfter = null,
    ProviderChallengeKind? ChallengeKind = null);

public sealed class ProviderFailureException : Exception
{
    public ProviderFailureException(
        ProviderFailureKind failureKind,
        string message,
        TimeSpan? retryAfter = null,
        Exception? innerException = null,
        ProviderChallengeKind? challengeKind = null)
        : base(message, innerException)
    {
        FailureKind = failureKind;
        RetryAfter = retryAfter;
        ChallengeKind = challengeKind;
    }

    public ProviderChallengeKind? ChallengeKind { get; }

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
                providerFailure.RetryAfter,
                providerFailure.ChallengeKind);
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

        if (exception is IOException)
        {
            return ProviderFailureKind.TransientHttp;
        }

        if (exception is JsonException or XmlException or FormatException or InvalidDataException or InvalidOperationException or NotSupportedException)
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
