# Java Implementation

## Target

Prefer:

```text
Java 17+
Maven
JUnit 5
AssertJ
OpenTelemetry Java API / SDK
```

Avoid:

```text
Spring
reflection-heavy frameworks
runtime magic
XML configuration
```

Configuration should be programmatic.

---

## Suggested public API

The API should look approximately like:

```java
public interface Lifecycle {

    void phase(LifecyclePhase phase);

    void milestone(String name);

    void milestone(
            String name,
            Map<String, ?> attributes);

    void condition(
            String name,
            ConditionState state);

    void condition(
            String name,
            ConditionState state,
            Map<String, ?> attributes);
}
```

If a richer typed attribute abstraction is preferred, keep it small and explicit.

Do not expose OpenTelemetry types through this API.

---

## Core types

Suggested concepts:

```java
public enum LifecyclePhase {
    STARTING,
    RUNNING,
    STOPPING,
    STOPPED
}
```

```java
public enum ConditionState {
    UP,
    DOWN,
    DEGRADED
}
```

An immutable event model should represent the library's own domain event independently of OTel.

---

## Sink abstraction

Define something approximately equivalent to:

```java
public interface LifecycleEventSink {

    void emit(LifecycleEvent event);
}
```

The core library depends on this interface.

OpenTelemetry belongs in an adapter module.

---

## Suggested Maven structure

Prefer a small multi-module build:

```text
lifecycle-parent
  lifecycle-core
  lifecycle-otel
```

Optionally:

```text
lifecycle-test
```

only if enough reusable test functionality exists to justify it.

Do not create modules for architectural purity alone.

---

## OpenTelemetry adapter

Implement an adapter that emits a structured event through the OpenTelemetry Logs API.

Conceptually:

```java
logger.logRecordBuilder()
        .setEventName(event.name())
        .setTimestamp(event.timestamp())
        // attributes
        .emit();
```

Before coding, inspect the current OpenTelemetry Java API and confirm the supported event/log API surface.

If there is a material mismatch between the proposed design and the current Java API, document it explicitly rather than hiding it behind a workaround.

---

## Concurrency

Assume lifecycle methods can be called concurrently.

Candidate implementation techniques include:

```text
AtomicReference<LifecyclePhase>
ConcurrentMap<String, ConditionState>
```

Avoid a single coarse-grained lock unless it is actually required.

The aim is correctness with minimal contention and minimal machinery.

---

## Time

Do not scatter `Instant.now()` calls throughout the code.

Use injectable/programmatic time sources.

For example:

```text
Clock
```

for wall time, plus a monotonic source for elapsed duration.

---

## Testing

Use:

```text
JUnit 5
AssertJ
```

Tests should cover at least:

- initial phase event
- phase previous/current values
- milestone emission
- milestone custom attributes
- initial condition
- condition transition
- duplicate condition value
- elapsed time
- wall-clock timestamp
- concurrent condition updates
- sink failure behaviour
- composite sink behaviour
