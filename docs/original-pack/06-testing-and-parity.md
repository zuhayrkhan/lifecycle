# Testing and Cross-Language Parity

## In-memory/test sink

Provide an in-memory sink in each implementation.

Its purpose is to make application and library tests straightforward.

Java usage should support assertions such as:

```java
assertThat(events)
        .extracting(LifecycleEvent::name)
        .containsExactly(
                "application.lifecycle.phase_changed",
                "application.lifecycle.milestone");
```

The test sink should not depend on OpenTelemetry.

---

## Composite sink

Provide a lightweight composite sink.

Conceptually:

```java
LifecycleEventSink sink =
        CompositeLifecycleEventSink.of(
                otelSink,
                loggingSink);
```

Equivalent functionality should exist in .NET.

Do not turn the composite sink into a queue, dispatcher framework, or event bus.

---

## Failure behaviour

Telemetry must never unexpectedly prevent the application from completing its own lifecycle work.

If a sink fails, think carefully about where failure containment belongs.

Do not add:

- retry loops
- disk queues
- background worker pools
- persistence
- delivery guarantees

unless a concrete requirement exists.

Prefer to rely on the underlying telemetry SDK for transport/retry concerns.

Document the chosen failure semantics explicitly.

---

## Cross-language compatibility table

Maintain a short document/table similar to:

| Concept | Java | .NET |
|---|---|---|
| Phase | `LifecyclePhase` | `LifecyclePhase` |
| Milestone | `milestone()` | `Milestone()` |
| Condition | `condition()` | `Condition()` |
| Sink | `LifecycleEventSink` | `ILifecycleEventSink` |
| Event | `LifecycleEvent` | `LifecycleEvent` |

This table is useful, but the more important parity requirement is the emitted telemetry.

---

## Golden schema tests

Create cross-language golden tests or fixtures covering representative events.

At minimum, both implementations should produce semantically equivalent output for:

```text
STARTING
configuration-loaded
dependency.database = UP
dependency.amps = UP
initial-data-loaded
RUNNING
health = UP
dependency.amps = DOWN
health = DEGRADED
dependency.amps = UP
health = UP
```

Normalise language/runtime-specific representation before comparison if necessary.

The event names and attribute names from `03-event-schema.md` must match exactly.

---

## Example lifecycle sequence

Equivalent sample applications should demonstrate:

```java
lifecycle.phase(STARTING);

lifecycle.milestone("configuration-loaded");

lifecycle.condition("dependency.database", UP);
lifecycle.condition("dependency.amps", UP);

lifecycle.milestone("initial-data-loaded");

lifecycle.phase(RUNNING);

lifecycle.condition("health", UP);

lifecycle.condition("dependency.amps", DOWN);
lifecycle.condition("health", DEGRADED);

lifecycle.condition("dependency.amps", UP);
lifecycle.condition("health", UP);
```

The examples should show the resulting structured telemetry.

---

## Build and CI

Provide simple commands for:

- Java build
- Java tests
- .NET build
- .NET tests

If appropriate, add a lightweight CI workflow that verifies both implementations.

Keep the CI straightforward.
