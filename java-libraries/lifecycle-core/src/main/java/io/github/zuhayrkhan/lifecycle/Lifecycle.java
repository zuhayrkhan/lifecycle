package io.github.zuhayrkhan.lifecycle;

import java.util.Map;

/** Thread-safe lifecycle notifications. Invalid caller input throws; ordinary sink failures do not. */
public interface Lifecycle {
    void phase(LifecyclePhase phase);

    default void milestone(String name) {

        milestone(name, Map.of());
    }

    void milestone(String name, Map<String, ?> attributes);

    default void condition(String name, ConditionState state) {

        condition(name, state, Map.of());
    }

    void condition(String name, ConditionState state, Map<String, ?> attributes);
}
