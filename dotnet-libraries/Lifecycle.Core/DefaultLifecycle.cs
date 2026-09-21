using System.Collections.Concurrent;

namespace Lifecycle.Core;

public sealed class DefaultLifecycle : ILifecycle
{
    private readonly ILifecycleEventSink sink;
    private readonly TimeProvider time;
    private readonly long origin;
    private readonly Action<Exception> onError;
    private int phase = -1;
    private readonly ConcurrentDictionary<string, ConditionSlot> conditions = new ConcurrentDictionary<string, ConditionSlot>(StringComparer.Ordinal);

    public DefaultLifecycle(ILifecycleEventSink sink, TimeProvider? time = null, Action<Exception>? onError = null)
    {

        ArgumentNullException.ThrowIfNull(sink);

        this.sink = sink;
        this.time = time ?? TimeProvider.System;
        this.onError = onError ?? (_ => { });

        origin = this.time.GetTimestamp();
    }

    public void Phase(LifecyclePhase phase)
    {

        if (!Enum.IsDefined(phase))
        {
            throw new ArgumentOutOfRangeException(nameof(phase));
        }

        int previous = Interlocked.Exchange(ref this.phase, (int)phase);

        Dictionary<string, AttributeValue> attributes = new Dictionary<string, AttributeValue>(StringComparer.Ordinal)
        {
            ["lifecycle.phase.current"] = new AttributeValue(phase.ToString().ToUpperInvariant())
        };

        if (previous >= 0)
        {
            attributes["lifecycle.phase.previous"] = new AttributeValue(((LifecyclePhase)previous).ToString().ToUpperInvariant());
        }

        Emit("application.lifecycle.phase_changed", attributes);
    }

    public void Milestone(string name)
    {

        Milestone(name, new Dictionary<string, object?>());
    }

    public void Milestone(string name, IReadOnlyDictionary<string, object?> attributes)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Dictionary<string, AttributeValue> values = CallerAttributes(attributes);

        values["lifecycle.milestone"] = new AttributeValue(name);

        Emit("application.lifecycle.milestone", values);
    }

    public void Condition(string name, ConditionState state)
    {

        Condition(name, state, new Dictionary<string, object?>());
    }

    public void Condition(string name, ConditionState state, IReadOnlyDictionary<string, object?> attributes)
    {

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!Enum.IsDefined(state))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }

        Dictionary<string, AttributeValue> values = CallerAttributes(attributes);

        ConditionSlot slot = conditions.GetOrAdd(name, _ => new ConditionSlot());
        int previous = Interlocked.Exchange(ref slot.Value, (int)state);

        values["lifecycle.condition.name"] = new AttributeValue(name);
        values["lifecycle.condition.current"] = new AttributeValue(state.ToString().ToUpperInvariant());

        if (previous >= 0)
        {
            values["lifecycle.condition.previous"] = new AttributeValue(((ConditionState)previous).ToString().ToUpperInvariant());
        }

        Emit("application.lifecycle.condition_changed", values);
    }

    private static Dictionary<string, AttributeValue> CallerAttributes(IReadOnlyDictionary<string, object?> attributes)
    {

        ArgumentNullException.ThrowIfNull(attributes);

        Dictionary<string, AttributeValue> result = new Dictionary<string, AttributeValue>(StringComparer.Ordinal);

        foreach ((string key, object? value) in attributes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            if (key.StartsWith("lifecycle.", StringComparison.Ordinal) || key == "{OriginalFormat}")
            {
                throw new ArgumentException($"Reserved attribute key: {key}", nameof(attributes));
            }

            result.Add(key, new AttributeValue(value));
        }

        return result;
    }

    private void Emit(string name, Dictionary<string, AttributeValue> attributes)
    {

        TimeSpan elapsed = time.GetElapsedTime(origin, time.GetTimestamp());

        attributes["lifecycle.elapsed_ms"] = new AttributeValue(elapsed.Ticks / TimeSpan.TicksPerMillisecond);

        LifecycleEvent lifecycleEvent = new LifecycleEvent(name, time.GetUtcNow(), elapsed, attributes);

        SinkFailures.Emit(sink, lifecycleEvent, onError);
    }

    private sealed class ConditionSlot
    {
        internal int Value = -1;
    }
}
