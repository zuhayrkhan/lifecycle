package io.github.zuhayrkhan.lifecycle;

import java.util.List;
import java.util.Objects;
import java.util.function.Consumer;

public final class CompositeLifecycleEventSink implements LifecycleEventSink {
    private final List<LifecycleEventSink> sinks;
    private final Consumer<RuntimeException> onError;

    public CompositeLifecycleEventSink(List<LifecycleEventSink> sinks, Consumer<RuntimeException> onError) {

        this.sinks = List.copyOf(sinks);
        this.onError = Objects.requireNonNull(onError, "onError");
    }

    public static CompositeLifecycleEventSink of(LifecycleEventSink... sinks) {

        return new CompositeLifecycleEventSink(List.of(sinks), failure -> {});
    }

    @Override
    public void emit(LifecycleEvent event) {

        Objects.requireNonNull(event, "event");

        for (LifecycleEventSink sink : sinks) {
            SinkFailures.emit(sink, event, onError);
        }
    }
}
