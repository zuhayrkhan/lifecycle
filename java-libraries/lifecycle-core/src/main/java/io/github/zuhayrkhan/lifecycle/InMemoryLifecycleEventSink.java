package io.github.zuhayrkhan.lifecycle;

import java.util.List;
import java.util.Objects;
import java.util.concurrent.ConcurrentLinkedQueue;

/** Unbounded storage for tests and short-lived examples; not a production retention mechanism. */
public final class InMemoryLifecycleEventSink implements LifecycleEventSink {
    private final ConcurrentLinkedQueue<LifecycleEvent> events = new ConcurrentLinkedQueue<>();

    @Override
    public void emit(LifecycleEvent event) { events.add(Objects.requireNonNull(event, "event")); }

    public List<LifecycleEvent> events() { return List.copyOf(events); }
}
