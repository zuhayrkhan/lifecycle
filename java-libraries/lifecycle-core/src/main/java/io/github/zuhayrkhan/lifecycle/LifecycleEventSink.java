package io.github.zuhayrkhan.lifecycle;

/** Implementations must accept concurrent calls and should return promptly. */
@FunctionalInterface
public interface LifecycleEventSink {
    void emit(LifecycleEvent event);
}
