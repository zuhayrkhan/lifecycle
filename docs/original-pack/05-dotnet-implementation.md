# .NET Implementation

## Target

Build an idiomatic .NET/C# implementation with the same semantics as the Java library.

The public API does not need to mechanically mimic Java naming where doing so would be unnatural.

A developer familiar with either implementation should, however, immediately recognise the equivalent concepts.

Suggested package split:

```text
Lifecycle.Core
Lifecycle.OpenTelemetry
```

Add another package only if there is a clear practical reason.

---

## Public API

The conceptual API should match Java:

```text
Phase
Milestone
Condition
```

Likely C# shape:

```csharp
public interface ILifecycle
{
    void Phase(LifecyclePhase phase);

    void Milestone(string name);

    void Milestone(
        string name,
        IReadOnlyDictionary<string, object?> attributes);

    void Condition(
        string name,
        ConditionState state);

    void Condition(
        string name,
        ConditionState state,
        IReadOnlyDictionary<string, object?> attributes);
}
```

Exact signatures may be adjusted to current idiomatic .NET conventions.

Do not expose OpenTelemetry implementation types through the core API.

---

## Core implementation

Maintain only enough state to enrich emitted events:

- previous phase
- previous value for each named condition
- creation/start time

Do not enforce transition legality.

Do not build a workflow/state-machine framework.

---

## OpenTelemetry adapter

Implement a direct programmatic OpenTelemetry event/log adapter using the current supported .NET APIs.

Do not require lifecycle events to be written to stdout/stderr and then scraped.

Before implementation:

1. Inspect the current .NET OpenTelemetry API.
2. Determine the best supported way to emit a structured event/log record.
3. Preserve the event names and attributes defined in `03-event-schema.md`.
4. Document any limitation that prevents exact parity with Java.

Do not silently invent a different semantic model for .NET just because the OTel API shape differs.

---

## Concurrency

The implementation must support concurrent lifecycle notifications.

Use normal .NET concurrency primitives and collections.

Keep the concurrency model simple and obvious.

---

## Time

Separate:

- wall-clock timestamp
- monotonic elapsed duration

Make time controllable for tests.

---

## Testing

Use the conventional current .NET unit testing/assertion stack already established in the target repository if one exists.

If there is no existing standard, choose a conventional lightweight stack and document the choice.

Test the same behavioural cases as the Java implementation.
