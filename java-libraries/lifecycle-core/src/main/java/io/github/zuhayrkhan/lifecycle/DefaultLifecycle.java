package io.github.zuhayrkhan.lifecycle;

import java.time.Clock;
import java.time.Duration;
import java.util.HashMap;
import java.util.Map;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicReference;
import java.util.function.Consumer;
import java.util.function.LongSupplier;

public final class DefaultLifecycle implements Lifecycle {
    private final LifecycleEventSink sink;
    private final Clock clock;
    private final LongSupplier nanoTime;
    private final long origin;
    private final Consumer<RuntimeException> onError;
    private final AtomicReference<LifecyclePhase> phase = new AtomicReference<>();
    private final ConcurrentHashMap<String, ConditionState> conditions = new ConcurrentHashMap<>();

    public DefaultLifecycle(LifecycleEventSink sink) {
        this(sink, Clock.systemUTC(), System::nanoTime, failure -> {});
    }

    public DefaultLifecycle(LifecycleEventSink sink, Clock clock, LongSupplier nanoTime,
                            Consumer<RuntimeException> onError) {
        this.sink = Objects.requireNonNull(sink, "sink");
        this.clock = Objects.requireNonNull(clock, "clock");
        this.nanoTime = Objects.requireNonNull(nanoTime, "nanoTime");
        this.onError = Objects.requireNonNull(onError, "onError");
        this.origin = nanoTime.getAsLong();
    }

    @Override
    public void phase(LifecyclePhase current) {
        Objects.requireNonNull(current, "phase");
        var attributes = new HashMap<String, AttributeValue>();
        LifecyclePhase previous = phase.getAndSet(current);
        attributes.put("lifecycle.phase.current", new AttributeValue(current.name()));
        if (previous != null) attributes.put("lifecycle.phase.previous", new AttributeValue(previous.name()));
        emit("application.lifecycle.phase_changed", attributes);
    }

    @Override
    public void milestone(String name, Map<String, ?> attributes) {
        Names.requireName(name);
        var values = callerAttributes(attributes);
        values.put("lifecycle.milestone", new AttributeValue(name));
        emit("application.lifecycle.milestone", values);
    }

    @Override
    public void condition(String name, ConditionState current, Map<String, ?> attributes) {
        Names.requireName(name);
        Objects.requireNonNull(current, "state");
        var values = callerAttributes(attributes);
        ConditionState previous = conditions.put(name, current);
        values.put("lifecycle.condition.name", new AttributeValue(name));
        values.put("lifecycle.condition.current", new AttributeValue(current.name()));
        if (previous != null) values.put("lifecycle.condition.previous", new AttributeValue(previous.name()));
        emit("application.lifecycle.condition_changed", values);
    }

    private static HashMap<String, AttributeValue> callerAttributes(Map<String, ?> attributes) {
        Objects.requireNonNull(attributes, "attributes");
        var result = new HashMap<String, AttributeValue>();
        attributes.forEach((key, value) -> {
            Names.requireName(key);
            if (key.startsWith("lifecycle.") || key.equals("{OriginalFormat}")) {
                throw new IllegalArgumentException("Reserved attribute key: " + key);
            }
            result.put(key, new AttributeValue(value));
        });
        return result;
    }

    private void emit(String name, Map<String, AttributeValue> attributes) {
        var elapsed = Duration.ofNanos(nanoTime.getAsLong() - origin);
        attributes.put("lifecycle.elapsed_ms", new AttributeValue(elapsed.toMillis()));
        var event = new LifecycleEvent(name, clock.instant(), elapsed, attributes);
        SinkFailures.emit(sink, event, onError);
    }
}
