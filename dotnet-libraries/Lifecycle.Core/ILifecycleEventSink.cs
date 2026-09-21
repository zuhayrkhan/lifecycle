namespace Lifecycle.Core;

/// <summary>Implementations must accept concurrent calls and should return promptly.</summary>
public interface ILifecycleEventSink
{
    void Emit(LifecycleEvent lifecycleEvent);
}
