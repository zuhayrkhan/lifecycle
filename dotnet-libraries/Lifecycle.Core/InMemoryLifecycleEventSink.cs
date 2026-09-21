using System.Collections.Concurrent;

namespace Lifecycle.Core;

/// <summary>Unbounded test/example storage, not a production retention mechanism.</summary>
public sealed class InMemoryLifecycleEventSink : ILifecycleEventSink
{
    private readonly ConcurrentQueue<LifecycleEvent> events = new();
    public IReadOnlyList<LifecycleEvent> Events => Array.AsReadOnly(events.ToArray());

    public void Emit(LifecycleEvent lifecycleEvent)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        events.Enqueue(lifecycleEvent);
    }
}
