# Application Lifecycle Telemetry Pack

This pack defines the design and implementation brief for a small, cross-language application lifecycle telemetry library.

The goal is to provide equivalent Java and .NET libraries that let applications describe lifecycle behaviour in a consistent way without exposing OpenTelemetry, Prometheus, Loki, Grafana, or any other telemetry backend in the application-facing API.

## Read order

1. `01-objective-and-scope.md`
2. `02-domain-model.md`
3. `03-event-schema.md`
4. `04-java-implementation.md`
5. `05-dotnet-implementation.md`
6. `06-testing-and-parity.md`
7. `07-non-goals.md`
8. `08-coco-task.md`

## Core design principle

The library observes lifecycle behaviour; it does not control it.

It is deliberately **not** a general-purpose state machine, workflow engine, startup orchestrator, health-check framework, or event bus.

The implementation should be small, dependency-light, thread-safe, testable, and boring.
