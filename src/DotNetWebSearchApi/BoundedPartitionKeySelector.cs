namespace DotNetWebSearchApi;

internal sealed class BoundedPartitionKeySelector
{
    private readonly object _gate = new();
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);
    private readonly int _maximumKeys;

    public BoundedPartitionKeySelector(int maximumKeys)
    {
        if (maximumKeys < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumKeys));
        }

        _maximumKeys = maximumKeys;
    }

    public string GetPartitionKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        lock (_gate)
        {
            if (_keys.Contains(key))
            {
                return key;
            }

            if (_keys.Count >= _maximumKeys)
            {
                return "__partition_overflow__";
            }

            _keys.Add(key);
            return key;
        }
    }
}
