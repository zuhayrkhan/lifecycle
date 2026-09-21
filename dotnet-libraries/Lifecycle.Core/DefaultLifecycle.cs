using System.Collections.Concurrent;

namespace Lifecycle.Core;

public sealed class DefaultLifecycle : ILifecycle
{
    private readonly ILifecycleEventSink sink;
    private readonly TimeProvider time;
    private readonly long origin;
    private readonly Action<Exception> onError;
    private int phase = -1;
    private readonly ConcurrentDictionary<string, ConditionSlot> conditions = new(StringComparer.Ordinal);

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
        if (!Enum.IsDefined(phase)) throw new ArgumentOutOfRangeException(nameof(phase));
        var previous = Interlocked.Exchange(ref this.phase, (int)phase);
        var attributes = new Dictionary<string, AttributeValue>(StringComparer.Ordinal)
        {
            ["lifecycle.phase.current"] = new(phase.ToString().ToUpperInvariant())
        };
        if (previous >= 0) attributes["lifecycle.phase.previous"] = new(((LifecyclePhase)previous).ToString().ToUpperInvariant());
        Emit("application.lifecycle.phase_changed", attributes);
    }

    public void Milestone(string name) => Milestone(name, new Dictionary<string, object?>());

    public void Milestone(string name, IReadOnlyDictionary<string, object?> attributes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var values = CallerAttributes(attributes);
        values["lifecycle.milestone"] = new(name);
        Emit("application.lifecycle.milestone", values);
    }

    public void Condition(string name, ConditionState state) => Condition(name, state, new Dictionary<string, object?>());

    public void Condition(string name, ConditionState state, IReadOnlyDictionary<string, object?> attributes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Enum.IsDefined(state)) throw new ArgumentOutOfRangeException(nameof(state));
        var values = CallerAttributes(attributes);
        var slot = conditions.GetOrAdd(name, _ => new ConditionSlot());
        var previous = Interlocked.Exchange(ref slot.Value, (int)state);
        values["lifecycle.condition.name"] = new(name);
        values["lifecycle.condition.current"] = new(state.ToString().ToUpperInvariant());
        if (previous >= 0) values["lifecycle.condition.previous"] = new(((ConditionState)previous).ToString().ToUpperInvariant());
        Emit("application.lifecycle.condition_changed", values);
    }

    private static Dictionary<string, AttributeValue> CallerAttributes(IReadOnlyDictionary<string, object?> attributes)
    {
        ArgumentNullException.ThrowIfNull(attributes);
        var result = new Dictionary<string, AttributeValue>(StringComparer.Ordinal);
        foreach (var (key, value) in attributes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            if (key.StartsWith("lifecycle.", StringComparison.Ordinal) || key == "{OriginalFormat}")
                throw new ArgumentException($"Reserved attribute key: {key}", nameof(attributes));
            result.Add(key, new(value));
        }
        return result;
    }

    private void Emit(string name, Dictionary<string, AttributeValue> attributes)
    {
        var elapsed = time.GetElapsedTime(origin, time.GetTimestamp());
        attributes["lifecycle.elapsed_ms"] = new(elapsed.Ticks / TimeSpan.TicksPerMillisecond);
        var lifecycleEvent = new LifecycleEvent(name, time.GetUtcNow(), elapsed, attributes);
        SinkFailures.Emit(sink, lifecycleEvent, onError);
    }

    private sealed class ConditionSlot { internal int Value = -1; }
}
