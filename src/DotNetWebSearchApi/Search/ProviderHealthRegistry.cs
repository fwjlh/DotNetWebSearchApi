using System.Collections.Concurrent;

namespace DotNetWebSearchApi.Search;

public enum ProviderHealthStatus
{
    Closed,
    Open,
    HalfOpen
}

public sealed record ProviderHealthPolicy
{
    public int FailureThreshold { get; init; } = 2;
    public TimeSpan BaseCooldown { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan RateLimitCooldown { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan ChallengeCooldown { get; init; } = TimeSpan.FromSeconds(15);
    public TimeSpan PermanentFailureCooldown { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan InvalidResponseCooldown { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan MaximumCooldown { get; init; } = TimeSpan.FromMinutes(5);
}

public sealed record ProviderHealthSnapshot(
    string ProviderName,
    ProviderHealthStatus Status,
    int ConsecutiveFailures,
    DateTimeOffset? CooldownUntil,
    ProviderFailureKind? LastFailureKind);

public sealed record ProviderHealthAdmission(
    bool CanExecute,
    ProviderHealthSnapshot Snapshot,
    TimeSpan? RetryAfter);

public sealed class ProviderHealthRegistry
{
    private readonly ConcurrentDictionary<string, ProviderState> _states =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ProviderHealthPolicy _policy;
    private readonly TimeProvider _timeProvider;

    public ProviderHealthRegistry(
        ProviderHealthPolicy? policy = null,
        TimeProvider? timeProvider = null)
    {
        _policy = policy ?? new ProviderHealthPolicy();
        _timeProvider = timeProvider ?? TimeProvider.System;
        ValidatePolicy(_policy);
    }

    public ProviderHealthAdmission TryAcquire(string providerName)
    {
        ProviderState state = GetState(providerName);
        lock (state.SyncRoot)
        {
            DateTimeOffset now = _timeProvider.GetUtcNow();
            if (state.Status == ProviderHealthStatus.Open)
            {
                if (state.CooldownUntil > now)
                {
                    return new ProviderHealthAdmission(
                        false,
                        Snapshot(providerName, state),
                        state.CooldownUntil - now);
                }

                state.Status = ProviderHealthStatus.HalfOpen;
                state.ProbeInProgress = false;
            }

            if (state.Status == ProviderHealthStatus.HalfOpen)
            {
                if (state.ProbeInProgress)
                {
                    return new ProviderHealthAdmission(false, Snapshot(providerName, state), null);
                }

                state.ProbeInProgress = true;
                return new ProviderHealthAdmission(true, Snapshot(providerName, state), TimeSpan.Zero);
            }

            return new ProviderHealthAdmission(true, Snapshot(providerName, state), null);
        }
    }

    public void ReleaseProbe(string providerName)
    {
        if (!_states.TryGetValue(providerName, out ProviderState? state))
        {
            return;
        }

        lock (state.SyncRoot)
        {
            if (state.Status == ProviderHealthStatus.HalfOpen)
            {
                state.ProbeInProgress = false;
            }
        }
    }

    public void RecordSuccess(string providerName)
    {
        ProviderState state = GetState(providerName);
        lock (state.SyncRoot)
        {
            state.Status = ProviderHealthStatus.Closed;
            state.ConsecutiveFailures = 0;
            state.CooldownUntil = null;
            state.LastFailureKind = null;
            state.ProbeInProgress = false;
        }
    }

    public void RecordFailure(
        string providerName,
        ProviderFailureKind failureKind,
        TimeSpan? retryAfter = null)
    {
        ProviderState state = GetState(providerName);
        lock (state.SyncRoot)
        {
            state.ConsecutiveFailures++;
            state.LastFailureKind = failureKind;
            state.ProbeInProgress = false;

            bool shouldOpen = state.Status == ProviderHealthStatus.HalfOpen
                || IsImmediateCooldown(failureKind)
                || state.ConsecutiveFailures >= _policy.FailureThreshold;
            if (!shouldOpen)
            {
                return;
            }

            TimeSpan cooldown = GetCooldown(failureKind, state.ConsecutiveFailures, retryAfter);
            state.Status = ProviderHealthStatus.Open;
            state.CooldownUntil = _timeProvider.GetUtcNow().Add(cooldown);
        }
    }

    public IReadOnlyList<ProviderHealthSnapshot> GetSnapshot(IEnumerable<string> providerNames)
    {
        return providerNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name =>
            {
                ProviderState state = GetState(name);
                lock (state.SyncRoot)
                {
                    if (state.Status == ProviderHealthStatus.Open
                        && state.CooldownUntil <= _timeProvider.GetUtcNow())
                    {
                        state.Status = ProviderHealthStatus.HalfOpen;
                        state.ProbeInProgress = false;
                    }

                    return Snapshot(name, state);
                }
            })
            .ToArray();
    }

    private ProviderState GetState(string providerName)
    {
        return _states.GetOrAdd(providerName, _ => new ProviderState());
    }

    private TimeSpan GetCooldown(
        ProviderFailureKind failureKind,
        int consecutiveFailures,
        TimeSpan? retryAfter)
    {
        if (retryAfter is { } requested && requested > TimeSpan.Zero)
        {
            return requested > _policy.MaximumCooldown ? _policy.MaximumCooldown : requested;
        }

        if (failureKind == ProviderFailureKind.RateLimited)
        {
            return ClampCooldown(_policy.RateLimitCooldown);
        }

        if (failureKind == ProviderFailureKind.Challenge)
        {
            return ClampCooldown(_policy.ChallengeCooldown);
        }

        if (failureKind is ProviderFailureKind.Authentication
            or ProviderFailureKind.AccessDenied
            or ProviderFailureKind.Configuration)
        {
            return ClampCooldown(_policy.PermanentFailureCooldown);
        }

        if (failureKind == ProviderFailureKind.InvalidResponse)
        {
            return ClampCooldown(_policy.InvalidResponseCooldown);
        }

        int exponent = Math.Clamp(consecutiveFailures - _policy.FailureThreshold, 0, 10);
        double multiplier = Math.Pow(2, exponent);
        double milliseconds = Math.Min(
            _policy.BaseCooldown.TotalMilliseconds * multiplier,
            _policy.MaximumCooldown.TotalMilliseconds);
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private TimeSpan ClampCooldown(TimeSpan cooldown)
    {
        return cooldown > _policy.MaximumCooldown ? _policy.MaximumCooldown : cooldown;
    }

    private static bool IsImmediateCooldown(ProviderFailureKind failureKind)
    {
        return failureKind is ProviderFailureKind.RateLimited
            or ProviderFailureKind.Challenge
            or ProviderFailureKind.Authentication
            or ProviderFailureKind.AccessDenied
            or ProviderFailureKind.Configuration
            or ProviderFailureKind.InvalidResponse;
    }

    private static ProviderHealthSnapshot Snapshot(string providerName, ProviderState state)
    {
        return new ProviderHealthSnapshot(
            providerName,
            state.Status,
            state.ConsecutiveFailures,
            state.CooldownUntil,
            state.LastFailureKind);
    }

    private static void ValidatePolicy(ProviderHealthPolicy policy)
    {
        if (policy.FailureThreshold < 1
            || policy.BaseCooldown <= TimeSpan.Zero
            || policy.RateLimitCooldown <= TimeSpan.Zero
            || policy.ChallengeCooldown <= TimeSpan.Zero
            || policy.PermanentFailureCooldown <= TimeSpan.Zero
            || policy.InvalidResponseCooldown <= TimeSpan.Zero
            || policy.MaximumCooldown <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(policy));
        }
    }

    private sealed class ProviderState
    {
        public object SyncRoot { get; } = new();
        public ProviderHealthStatus Status { get; set; } = ProviderHealthStatus.Closed;
        public int ConsecutiveFailures { get; set; }
        public DateTimeOffset? CooldownUntil { get; set; }
        public ProviderFailureKind? LastFailureKind { get; set; }
        public bool ProbeInProgress { get; set; }
    }
}
