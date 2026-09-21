using System.Collections.ObjectModel;

namespace Lifecycle.Core;

public sealed record LifecycleEvent
{
    public string Name { get; }

    public DateTimeOffset Timestamp { get; }

    public TimeSpan Elapsed { get; }

    public IReadOnlyDictionary<string, AttributeValue> Attributes { get; }

    public LifecycleEvent(string name, DateTimeOffset timestamp, TimeSpan elapsed,
        IReadOnlyDictionary<string, AttributeValue> attributes)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(attributes);

        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        Dictionary<string, AttributeValue> copy = new Dictionary<string, AttributeValue>(StringComparer.Ordinal);

        foreach ((string key, AttributeValue value) in attributes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            ArgumentNullException.ThrowIfNull(value);

            copy.Add(key, value);
        }

        Name = name;
        Timestamp = timestamp.ToUniversalTime();
        Elapsed = elapsed;
        Attributes = new ReadOnlyDictionary<string, AttributeValue>(copy);
    }
}
