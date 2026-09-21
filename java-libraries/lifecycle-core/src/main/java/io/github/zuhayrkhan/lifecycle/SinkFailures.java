package io.github.zuhayrkhan.lifecycle;

import java.util.function.Consumer;

final class SinkFailures {
    private SinkFailures() {}

    static void emit(LifecycleEventSink sink, LifecycleEvent event, Consumer<RuntimeException> onError) {
        try {
            sink.emit(event);
        } catch (RuntimeException failure) {
            try {
                onError.accept(failure);
            } catch (RuntimeException ignored) {
                // A diagnostic callback must not make a telemetry failure affect the application.
            }
        }
    }
}
