---
name: lifecycle-integration
description: Integrate the zuhayrkhan/lifecycle Java or .NET libraries into an application, including shared lifecycle state, OpenTelemetry log export, startup and shutdown notifications, and integration verification. Use for adopting or repairing this library integration, not generic lifecycle orchestration or unrelated telemetry libraries.
---

# Integrate Lifecycle telemetry

## Establish the host setup

1. Inspect the consuming application's runtime, dependency management, composition
   root, existing OTel/logging configuration and shutdown hooks.
2. Use Java 17+ or .NET 8+. Load only the relevant reference:
   [Java integration](references/java.md) or [.NET integration](references/dotnet.md).
3. Confirm the library version and package source. The examples target Lifecycle
   `0.1.0`, OTel Java `1.66.0` and OTel .NET `1.19.1`. These are the repository's
   tested baseline, not a claim that they are the newest versions. Check the
   selected checkout's `pom.xml` and project files when using another version.
4. Reuse the application's SDK, Resource identity and exporter configuration.
   Add only missing dependencies; do not introduce a second logging provider or
   replace host logging policy merely to copy an example.

Source repository: [zuhayrkhan/lifecycle](https://github.com/zuhayrkhan/lifecycle).
The skill folder is portable; its relative reference links resolve from this folder,
not from the consuming application's working directory. Package build commands in
the references run in a separate Lifecycle checkout, not in the consumer.

## Wire one shared instance

- Construct one `DefaultLifecycle` per application/process and share its
  `Lifecycle` (Java) or `ILifecycle` (.NET) abstraction across components. Do not
  create one per request, dependency or notification. Construction emits nothing;
  monotonic elapsed time starts then, not at OS process start.
- The core is backend-independent. Use `InMemoryLifecycleEventSink` for bounded
  tests/examples, a custom sink for another backend, or `OpenTelemetryLifecycleSink`
  for OTel. `CompositeLifecycleEventSink` fans out to multiple sinks.
- The host owns provider lifetime, batching, export, retry, endpoint configuration,
  service identity and bounded flushing. Sinks and diagnostic callbacks are
  synchronous and must return promptly; the library adds no workers or timeouts.
- In .NET, register `AddLifecycleTelemetry()` **before every exporter** in the
  provider receiving these events. It restores original timestamps and removes the
  private timestamp attribute. Enable Information-level logs for the category
  `Lifecycle.OpenTelemetry`. Use the stable `ILogger` integration in the library.

## Place notifications at application boundaries

- Report `STARTING` / `Starting` once startup begins, and `RUNNING` / `Running`
  when the application considers itself ready. Use milestones for completed work
  such as `configuration-loaded` or `initial-data-loaded`.
- Conditions are independent named observations, such as `dependency.database`
  and `health`, using `UP`, `DOWN`, `DEGRADED` (PascalCase enum members in C#).
  The application decides these values. The library does not probe dependencies,
  aggregate health or coordinate startup.
- Use stable, bounded condition names: each distinct name is retained for the
  instance lifetime. Avoid request IDs or unbounded user identifiers as names.
- Every valid call emits, including duplicates. All transitions are accepted.
  Previous values are included whenever known. Concurrent state updates are atomic,
  but event delivery can reorder; do not use emission order as a global sequence.
- During orderly shutdown: report STOPPING, finish relevant application shutdown
  work, report STOPPED, perform a bounded flush while logging/transport services
  are alive, then dispose the host-owned provider. STOPPED is a declaration before
  process exit, not proof of termination. Handle application failure paths using
  its existing shutdown policy; telemetry must not invent a successful shutdown.

## Preserve the event contract

- Pass ordinary scalar values in caller attribute maps: string, signed integer,
  finite floating point or boolean. The core normalizes integers to signed 64-bit
  and floating point to double. Do not pre-wrap caller values in `AttributeValue`.
- Names and keys must be nonblank. Null values, collections, arbitrary objects,
  nonfinite numbers, and C# unsigned integers/decimal are rejected. Keys beginning
  `lifecycle.` and the exact key `{OriginalFormat}` are reserved.
- Invalid input throws before updating state. Ordinary sink failures are contained
  and do not roll state back. Configure the optional error callback if the host
  needs diagnostics; avoid recursively logging failures through the failing sink.
- OTel records use the native event-name field, the same name as body, INFO severity
  (number 9), original UTC event timestamp, monotonic `lifecycle.elapsed_ms`, typed
  attributes, host Resource and active trace/span context when present. Do not
  replace this with stdout scraping or an `event.name` attribute.
- Expected names are `application.lifecycle.phase_changed`,
  `application.lifecycle.milestone` and `application.lifecycle.condition_changed`.
  For detailed schema work, read the selected checkout's `docs/event-contract.md`;
  `docs/original-pack` is historical, not the resolved contract.

## Verify the integration

1. Build the consumer with its normal toolchain. Use explicit types instead of
   `var`, braces for control-flow bodies and blank lines between logical steps in
   new examples and integration code; retain language-appropriate brace placement.
2. Exercise its startup/shutdown path with an in-memory sink. Check intended
   phases/milestones/conditions, shared previous-state tracking and caller values.
3. For an OTel integration, test the **host's configured pipeline** with a local
   receiver or test exporter. Check native event name, original timestamp, INFO,
   scalar types, Resource identity and absence of the .NET private timestamp key.
   Include the host's filters/processors and a bounded shutdown flush.
4. Treat export as best effort. A returned lifecycle call does not prove backend
   receipt. Report what was compiled, tested and observed, and any unverified live
   backend delivery. No live Collector is required for core tests.

When working in the Lifecycle repository itself, `./mvnw clean verify` builds both
runtimes and runs the shared-fixture OTLP/HTTP parity tests against local receivers.
Keep consumer integration changes within the user's request; this skill does not
grant permission to publish packages or change a production telemetry destination.
