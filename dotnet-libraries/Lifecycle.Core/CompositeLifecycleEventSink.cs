namespace Lifecycle.Core;

public sealed class CompositeLifecycleEventSink : ILifecycleEventSink
{
    private readonly ILifecycleEventSink[] sinks;
    private readonly Action<Exception> onError;

    public CompositeLifecycleEventSink(IEnumerable<ILifecycleEventSink> sinks, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(sinks);
        this.sinks = sinks.ToArray();
        if (this.sinks.Any(s => s is null)) throw new ArgumentException("Sinks must not contain null", nameof(sinks));
        this.onError = onError ?? (_ => { });
    }

    public static CompositeLifecycleEventSink Of(params ILifecycleEventSink[] sinks) => new(sinks);

    public void Emit(LifecycleEvent lifecycleEvent)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        foreach (var sink in sinks) SinkFailures.Emit(sink, lifecycleEvent, onError);
    }
}
