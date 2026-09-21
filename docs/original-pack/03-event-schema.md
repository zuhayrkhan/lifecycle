# Event Schema

This document is the normative cross-language telemetry contract.

Java and .NET implementations may differ internally, but they must emit semantically identical event names and attribute names.

## Event names

Use the following stable event names:

```text
application.lifecycle.phase_changed
application.lifecycle.milestone
application.lifecycle.condition_changed
```

These names form part of the wire-level contract.

Changing them later should be treated as a compatibility concern.

---

## Common event fields

Every event should have:

```text
event name
wall-clock timestamp
elapsed duration since Lifecycle instance creation
attributes
```

The event model should be immutable.

Conceptually:

```java
public record LifecycleEvent(
        String name,
        Instant timestamp,
        Duration elapsed,
        Map<String, String> attributes) {
}
```

The precise type names may differ between Java and .NET.

---

## Phase event

Event name:

```text
application.lifecycle.phase_changed
```

Attributes:

```text
lifecycle.phase.current
lifecycle.phase.previous   # omit when no previous phase exists
lifecycle.elapsed_ms
```

Example:

```text
event.name = application.lifecycle.phase_changed
lifecycle.phase.previous = STARTING
lifecycle.phase.current = RUNNING
lifecycle.elapsed_ms = 13891
```

---

## Milestone event

Event name:

```text
application.lifecycle.milestone
```

Attributes:

```text
lifecycle.milestone
lifecycle.elapsed_ms
```

Example:

```text
event.name = application.lifecycle.milestone
lifecycle.milestone = configuration-loaded
lifecycle.elapsed_ms = 420
```

Application-supplied milestone attributes should also be attached.

Example:

```text
event.name = application.lifecycle.milestone
lifecycle.milestone = initial-position-load-complete
lifecycle.elapsed_ms = 7421
position.count = 18342
```

---

## Condition event

Event name:

```text
application.lifecycle.condition_changed
```

Attributes:

```text
lifecycle.condition.name
lifecycle.condition.current
lifecycle.condition.previous   # omit when no previous value exists
lifecycle.elapsed_ms
```

Example:

```text
event.name = application.lifecycle.condition_changed
lifecycle.condition.name = dependency.amps
lifecycle.condition.previous = UP
lifecycle.condition.current = DOWN
lifecycle.elapsed_ms = 735421
```

---

## Resource attributes

Service/process identity should normally be supplied through the OpenTelemetry Resource rather than repeated manually in every lifecycle call.

Typical resource attributes include:

```text
service.name
service.version
service.instance.id
deployment.environment.name
```

The lifecycle library should not introduce a second competing configuration model for those attributes unless a concrete need emerges.

---

## Timestamp semantics

Two different clocks are needed conceptually:

### Wall-clock time

Used for the emitted event timestamp.

### Monotonic elapsed time

Used for:

```text
lifecycle.elapsed_ms
```

Elapsed duration should not be calculated by subtracting two wall-clock timestamps if a monotonic time source is available.

Clock/time sources should be injectable or otherwise controllable in tests.

---

## Attribute typing

The first version may use a deliberately small attribute model if that materially simplifies cross-language parity.

However, before implementation, assess whether attributes should support native telemetry value types such as:

- string
- long/integer
- double
- boolean

Do not accidentally lock the schema into strings purely because the first example API used `Map<String, String>`.

If richer types are adopted, Java and .NET must still have equivalent semantics.

---

## Compatibility rule

Treat event and attribute naming as a protocol.

Implementation code can evolve freely.

The emitted semantic schema should evolve deliberately.
