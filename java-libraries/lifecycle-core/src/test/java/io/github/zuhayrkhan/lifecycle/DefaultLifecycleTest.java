package io.github.zuhayrkhan.lifecycle;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import java.time.Clock;
import java.time.Duration;
import java.time.Instant;
import java.time.ZoneOffset;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.concurrent.Callable;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicLong;
import static org.assertj.core.api.Assertions.*;

class DefaultLifecycleTest {
    @Test
    void emitsEveryPhaseIncludingDuplicatesAndUnexpectedTransitions() {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        Lifecycle lifecycle = new DefaultLifecycle(sink);

        assertThat(sink.events()).isEmpty();

        lifecycle.phase(LifecyclePhase.RUNNING);
        lifecycle.phase(LifecyclePhase.RUNNING);
        lifecycle.phase(LifecyclePhase.STARTING);

        assertThat(sink.events()).hasSize(3);
        assertThat(sink.events().get(0).attributes()).doesNotContainKey("lifecycle.phase.previous");
        assertThat(value(sink.events().get(1), "lifecycle.phase.previous")).isEqualTo("RUNNING");
        assertThat(value(sink.events().get(2), "lifecycle.phase.current")).isEqualTo("STARTING");
    }

    @Test
    void remembersConditionsIndependentlyAndAllowsDuplicates() {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        Lifecycle lifecycle = new DefaultLifecycle(sink);

        lifecycle.condition("db", ConditionState.UP);
        lifecycle.condition("health", ConditionState.DEGRADED);
        lifecycle.condition("db", ConditionState.UP);
        lifecycle.condition("db", ConditionState.DOWN, Map.of("reason", "offline"));

        assertThat(sink.events().get(1).attributes()).doesNotContainKey("lifecycle.condition.previous");
        assertThat(value(sink.events().get(2), "lifecycle.condition.previous")).isEqualTo("UP");
        assertThat(value(sink.events().get(3), "reason")).isEqualTo("offline");
    }

    @Test
    void separatesWallTimeFromMonotonicElapsedAndTruncatesMilliseconds() {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        AtomicLong ticks = new AtomicLong(9_000_000);
        Instant wall = Instant.parse("2026-09-21T12:00:00Z");
        DefaultLifecycle lifecycle = new DefaultLifecycle(sink, Clock.fixed(wall, ZoneOffset.UTC), ticks::get, e -> {});

        ticks.addAndGet(1_999_999);
        lifecycle.milestone("ready");

        assertThat(sink.events().get(0).timestamp()).isEqualTo(wall);
        assertThat(sink.events().get(0).elapsed()).isEqualTo(Duration.ofNanos(1_999_999));
        assertThat(value(sink.events().get(0), "lifecycle.elapsed_ms")).isEqualTo(1L);
    }

    @Test
    void copiesAndNormalizesAttributesIntoImmutableEvents() {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        Map<String, Object> attrs = new HashMap<String, Object>(Map.of("count", 42, "ratio", 0.5f, "enabled", true, "text", ""));

        new DefaultLifecycle(sink).milestone("ready", attrs);
        attrs.put("count", 99);

        LifecycleEvent event = sink.events().get(0);

        assertThat(value(event, "count")).isEqualTo(42L);
        assertThat(value(event, "ratio")).isEqualTo(0.5d);
        assertThat(value(event, "enabled")).isEqualTo(true);
        assertThat(value(event, "text")).isEqualTo("");
        assertThatThrownBy(() -> event.attributes().clear()).isInstanceOf(UnsupportedOperationException.class);
        assertThatThrownBy(() -> sink.events().clear()).isInstanceOf(UnsupportedOperationException.class);
    }

    @ParameterizedTest
    @ValueSource(strings = {"", " ", "\t", "\u0085", "\u00a0", "\u2007", "\u202f"})
    void rejectsBlankNamesConsistently(String name) {

        DefaultLifecycle lifecycle = new DefaultLifecycle(new InMemoryLifecycleEventSink());

        assertThatIllegalArgumentException().isThrownBy(() -> lifecycle.milestone(name));
        assertThatIllegalArgumentException().isThrownBy(() -> lifecycle.condition(name, ConditionState.UP));
    }

    @Test
    void rejectsInvalidAttributesBeforeUpdatingState() {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        DefaultLifecycle lifecycle = new DefaultLifecycle(sink);
        List<Map<String, ?>> invalid = new ArrayList<Map<String, ?>>();

        invalid.add(Map.of("lifecycle.phase.current", "spoof"));
        invalid.add(Map.of("{OriginalFormat}", "spoof"));
        invalid.add(Map.of("n", Double.NaN));
        invalid.add(Map.of("n", Double.POSITIVE_INFINITY));
        invalid.add(Map.of("n", new Object()));
        invalid.add(Map.of("n", List.of(1)));

        Map<String, Object> nullValue = new HashMap<String, Object>();

        nullValue.put("n", null);
        invalid.add(nullValue);

        for (Map<String, ?> attributes : invalid) {
            assertThatIllegalArgumentException().isThrownBy(() -> lifecycle.condition("db", ConditionState.DOWN, attributes));
        }

        lifecycle.condition("db", ConditionState.UP);

        assertThat(sink.events()).hasSize(1);
        assertThat(sink.events().get(0).attributes()).doesNotContainKey("lifecycle.condition.previous");
    }

    @Test
    void containsFailuresWithoutRollingBackStateAndProtectsDiagnosticCallbacks() {

        InMemoryLifecycleEventSink seen = new InMemoryLifecycleEventSink();
        AtomicInteger attempts = new AtomicInteger();
        DefaultLifecycle lifecycle = new DefaultLifecycle(event -> {
            if (attempts.getAndIncrement() == 0) {
                throw new IllegalStateException("offline");
            }

            seen.emit(event);
        }, Clock.systemUTC(), System::nanoTime, failure -> {
            throw new IllegalStateException("diagnostics also down");
        });

        lifecycle.phase(LifecyclePhase.STARTING);
        lifecycle.phase(LifecyclePhase.RUNNING);

        assertThat(value(seen.events().get(0), "lifecycle.phase.previous")).isEqualTo("STARTING");
    }

    @Test
    void compositeAttemptsEveryChildEvenWhenErrorCallbackFails() {

        InMemoryLifecycleEventSink seen = new InMemoryLifecycleEventSink();
        CompositeLifecycleEventSink composite = new CompositeLifecycleEventSink(List.of(event -> {
            throw new IllegalStateException();
        }, seen),
                failure -> {
                    throw new IllegalStateException();
                });

        new DefaultLifecycle(composite).milestone("ready");

        assertThat(seen.events()).hasSize(1);
    }

    @Test
    void doesNotSuppressFatalErrors() {

        DefaultLifecycle lifecycle = new DefaultLifecycle(event -> {
            throw new AssertionError("fatal");
        });

        assertThatThrownBy(() -> lifecycle.milestone("ready")).isInstanceOf(AssertionError.class);
    }

    @Test
    void concurrentUpdatesHaveOneInitialValueAndConsistentTransitionCounts() throws Exception {

        InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();
        DefaultLifecycle lifecycle = new DefaultLifecycle(sink);
        ExecutorService pool = Executors.newFixedThreadPool(8);

        try {
            List<Callable<Void>> tasks = new ArrayList<Callable<Void>>();

            for (int i = 0; i < 500; i++) {
                final int n = i;

                tasks.add(() -> {
                    lifecycle.condition("db", n % 2 == 0 ? ConditionState.UP : ConditionState.DOWN);

                    return null;
                });
            }

            for (Future<Void> result : pool.invokeAll(tasks)) {
                result.get();
            }
        } finally {
            pool.shutdown();

            assertThat(pool.awaitTermination(10, TimeUnit.SECONDS)).isTrue();
        }

        lifecycle.condition("db", ConditionState.DEGRADED);

        List<LifecycleEvent> events = sink.events();

        assertThat(events).hasSize(501);
        assertThat(events.stream().filter(e -> !e.attributes().containsKey("lifecycle.condition.previous"))).hasSize(1);

        for (ConditionState state : ConditionState.values()) {
            long current = events.stream().filter(e -> state.name().equals(value(e, "lifecycle.condition.current"))).count();
            long previous = events.stream().filter(e -> state.name().equals(value(e, "lifecycle.condition.previous"))).count();

            assertThat(current - previous).isEqualTo(state == ConditionState.DEGRADED ? 1 : 0);
        }
    }

    private static Object value(LifecycleEvent event, String key) {

        AttributeValue value = event.attributes().get(key);

        return value == null ? null : value.value();
    }
}
