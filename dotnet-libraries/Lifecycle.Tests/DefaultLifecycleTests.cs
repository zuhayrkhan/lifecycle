using Lifecycle.Core;
using Xunit;

namespace Lifecycle.Tests;

public sealed class DefaultLifecycleTests
{
    [Fact]
    public void EmitsDuplicatesAndUnexpectedPhaseTransitions()
    {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        DefaultLifecycle lifecycle = new DefaultLifecycle(sink);

        Assert.Empty(sink.Events);

        lifecycle.Phase(LifecyclePhase.Running);
        lifecycle.Phase(LifecyclePhase.Running);
        lifecycle.Phase(LifecyclePhase.Starting);

        Assert.Equal(3, sink.Events.Count);
        Assert.False(sink.Events[0].Attributes.ContainsKey("lifecycle.phase.previous"));
        Assert.Equal("RUNNING", Value(sink.Events[1], "lifecycle.phase.previous"));
        Assert.Equal("STARTING", Value(sink.Events[2], "lifecycle.phase.current"));
    }

    [Fact]
    public void RemembersConditionsIndependentlyAndAllowsDuplicates()
    {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        DefaultLifecycle lifecycle = new DefaultLifecycle(sink);

        lifecycle.Condition("db", ConditionState.Up);
        lifecycle.Condition("health", ConditionState.Degraded);
        lifecycle.Condition("db", ConditionState.Up);
        lifecycle.Condition("db", ConditionState.Down, new Dictionary<string, object?> { ["reason"] = "offline" });

        Assert.False(sink.Events[1].Attributes.ContainsKey("lifecycle.condition.previous"));
        Assert.Equal("UP", Value(sink.Events[2], "lifecycle.condition.previous"));
        Assert.Equal("offline", Value(sink.Events[3], "reason"));
    }

    [Fact]
    public void UsesMonotonicTimeAndTruncatesMillisecondsDespiteWallClockRollback()
    {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        TestTimeProvider time = new TestTimeProvider();
        DefaultLifecycle lifecycle = new DefaultLifecycle(sink, time);

        time.Ticks += 19_999;
        time.Wall = time.Wall.AddHours(-1);
        lifecycle.Milestone("ready");

        Assert.Equal(time.Wall, sink.Events[0].Timestamp);
        Assert.Equal(TimeSpan.FromTicks(19_999), sink.Events[0].Elapsed);
        Assert.Equal(1L, Value(sink.Events[0], "lifecycle.elapsed_ms"));
    }

    [Fact]
    public void CopiesAndNormalizesAttributesIntoImmutableEvents()
    {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        Dictionary<string, object?> attrs = new Dictionary<string, object?> { ["count"] = 42, ["ratio"] = 0.5f, ["enabled"] = true, ["text"] = "" };

        new DefaultLifecycle(sink).Milestone("ready", attrs);
        attrs["count"] = 99;

        Assert.Equal(42L, Value(sink.Events[0], "count"));
        Assert.Equal(0.5d, Value(sink.Events[0], "ratio"));
        Assert.Equal(true, Value(sink.Events[0], "enabled"));
        Assert.Equal("", Value(sink.Events[0], "text"));
        Assert.Throws<NotSupportedException>(() => ((IDictionary<string, AttributeValue>)sink.Events[0].Attributes).Clear());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\u0085")]
    [InlineData("\u00a0")]
    [InlineData("\u2007")]
    [InlineData("\u202f")]
    public void RejectsBlankNames(string name)
    {

        DefaultLifecycle lifecycle = new DefaultLifecycle(new InMemoryLifecycleEventSink());

        Assert.ThrowsAny<ArgumentException>(() => lifecycle.Milestone(name));
        Assert.ThrowsAny<ArgumentException>(() => lifecycle.Condition(name, ConditionState.Up));
    }

    [Fact]
    public void RejectsInvalidInputBeforeUpdatingState()
    {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        DefaultLifecycle lifecycle = new DefaultLifecycle(sink);

        foreach (object? value in new object?[] { null, double.NaN, double.PositiveInfinity, new object(), new[] { 1 }, 1m, 1u })
        {
            Assert.ThrowsAny<ArgumentException>(() => lifecycle.Condition("db", ConditionState.Down, new Dictionary<string, object?> { ["n"] = value }));
        }

        foreach (string key in new[] { "lifecycle.phase.current", "{OriginalFormat}", " " })
        {
            Assert.ThrowsAny<ArgumentException>(() => lifecycle.Condition("db", ConditionState.Down, new Dictionary<string, object?> { [key] = "spoof" }));
        }

        Assert.ThrowsAny<ArgumentException>(() => lifecycle.Phase((LifecyclePhase)99));
        Assert.ThrowsAny<ArgumentException>(() => lifecycle.Condition("db", (ConditionState)99));

        lifecycle.Condition("db", ConditionState.Up);

        Assert.Single(sink.Events);
        Assert.False(sink.Events[0].Attributes.ContainsKey("lifecycle.condition.previous"));
    }

    [Fact]
    public void ContainsSinkAndDiagnosticFailuresWithoutRollingBackState()
    {

        InMemoryLifecycleEventSink seen = new InMemoryLifecycleEventSink();
        int attempts = 0;
        DefaultLifecycle lifecycle = new DefaultLifecycle(new DelegateSink(e =>
        {
            if (attempts++ == 0)
            {
                throw new InvalidOperationException("offline");
            }

            seen.Emit(e);
        }), onError: _ => throw new InvalidOperationException("diagnostics offline"));

        lifecycle.Phase(LifecyclePhase.Starting);
        lifecycle.Phase(LifecyclePhase.Running);

        Assert.Equal("STARTING", Value(seen.Events[0], "lifecycle.phase.previous"));
    }

    [Fact]
    public void CompositeAttemptsEveryChildEvenIfDiagnosticCallbackFails()
    {

        InMemoryLifecycleEventSink seen = new InMemoryLifecycleEventSink();
        CompositeLifecycleEventSink composite = new CompositeLifecycleEventSink(
            new ILifecycleEventSink[] { new DelegateSink(_ => throw new InvalidOperationException()), seen },
            _ => throw new InvalidOperationException());

        new DefaultLifecycle(composite).Milestone("ready");

        Assert.Single(seen.Events);
    }

    [Fact]
    public void DoesNotDeliberatelyContainFatalFailures()
    {

        DefaultLifecycle lifecycle = new DefaultLifecycle(new DelegateSink(_ => throw new OutOfMemoryException("simulated")));

        Assert.Throws<OutOfMemoryException>(() => lifecycle.Milestone("ready"));
    }

    [Fact]
    public void ConcurrentUpdatesHaveConsistentPreviousCurrentPairs()
    {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        DefaultLifecycle lifecycle = new DefaultLifecycle(sink);

        Parallel.For(0, 500, i => lifecycle.Condition("db", i % 2 == 0 ? ConditionState.Up : ConditionState.Down));
        lifecycle.Condition("db", ConditionState.Degraded);

        IReadOnlyList<LifecycleEvent> events = sink.Events;

        Assert.Equal(501, events.Count);
        Assert.Single(events, e => !e.Attributes.ContainsKey("lifecycle.condition.previous"));

        foreach (ConditionState state in Enum.GetValues<ConditionState>())
        {
            string name = state.ToString().ToUpperInvariant();
            int current = events.Count(e => name.Equals(Value(e, "lifecycle.condition.current")));
            int previous = events.Count(e => name.Equals(Value(e, "lifecycle.condition.previous")));

            Assert.Equal(state == ConditionState.Degraded ? 1 : 0, current - previous);
        }
    }

    private static object? Value(LifecycleEvent e, string key)
    {

        return e.Attributes.TryGetValue(key, out AttributeValue? v) ? v.Value : null;
    }
}

internal sealed class DelegateSink(Action<LifecycleEvent> emit) : ILifecycleEventSink
{
    public void Emit(LifecycleEvent lifecycleEvent)
    {

        emit(lifecycleEvent);
    }
}

internal sealed class TestTimeProvider : TimeProvider
{
    internal long Ticks;
    internal DateTimeOffset Wall = DateTimeOffset.Parse("2026-09-21T12:00:00Z");
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {

        return Ticks;
    }

    public override DateTimeOffset GetUtcNow()
    {

        return Wall;
    }
}
