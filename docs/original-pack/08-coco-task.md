# Coco Task

Read the entire pack before implementing anything.

Treat `03-event-schema.md` as normative.

## Task

Build production-quality Java and .NET implementations of the application lifecycle telemetry library described in this pack.

The two implementations must:

- expose equivalent concepts
- emit semantically identical lifecycle events
- use idiomatic language/runtime APIs
- remain independent of telemetry backends in the core modules
- provide OpenTelemetry adapters
- be thread-safe
- be straightforward to test
- remain deliberately small

## Before coding

Inspect the **current** OpenTelemetry APIs for both Java and .NET.

Confirm:

1. how to emit a structured event/log record programmatically
2. how event names are represented
3. how structured attributes are represented
4. how timestamps are supplied
5. how OTel Resource attributes are associated
6. whether there are any meaningful Java/.NET parity issues

If the current API differs materially from assumptions in this pack, write a short note explaining the issue and propose the smallest compatible adjustment.

Do not silently work around a material mismatch.

## Deliverables

Produce:

1. Java implementation
2. .NET implementation
3. unit tests for both
4. OpenTelemetry adapters for both
5. example applications
6. cross-language compatibility documentation
7. README with intended usage
8. short architecture/design note
9. build/test commands
10. assessment of any OTel parity limitations
11. representative emitted-event examples
12. golden/parity tests for the telemetry schema

## Implementation philosophy

Optimise for boring code.

This library may eventually be embedded in a large number of services.

Keep:

- dependency footprint small
- API surface small
- runtime behaviour unsurprising
- concurrency model simple
- configuration explicit
- telemetry schema stable

Do not turn this into a platform.

The core job is:

> observe application lifecycle behaviour and emit a consistent structured telemetry record.

## Stop conditions

Pause and report before making a major architectural change if you discover that:

- direct programmatic OTel event emission is not viable in one runtime
- the proposed schema conflicts with current semantic conventions
- exact cross-language parity would require a substantial hack
- a requested behaviour would require background infrastructure or delivery guarantees
- the core API would need to expose OTel-specific types

Otherwise, proceed to implementation.
