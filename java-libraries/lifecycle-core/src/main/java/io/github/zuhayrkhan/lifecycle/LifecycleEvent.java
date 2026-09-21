package io.github.zuhayrkhan.lifecycle;

import java.time.Duration;
import java.time.Instant;
import java.util.Map;
import java.util.Objects;

public record LifecycleEvent(String name, Instant timestamp, Duration elapsed, Map<String, AttributeValue> attributes) {
    public LifecycleEvent {
        Names.requireName(name);
        Objects.requireNonNull(timestamp, "timestamp");
        Objects.requireNonNull(elapsed, "elapsed");
        if (elapsed.isNegative()) throw new IllegalArgumentException("Elapsed duration must be nonnegative");
        attributes = Map.copyOf(attributes);
        attributes.keySet().forEach(Names::requireName);
    }
}
