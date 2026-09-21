# Objective and Scope

## Objective

Build two small libraries with equivalent behaviour:

- Java
- .NET / C#

Applications should be able to report lifecycle information such as:

- process starting
- configuration loaded
- dependency available
- dependency lost
- startup complete
- running
- health degraded
- health restored
- stopping
- stopped

The libraries should make this information available as structured telemetry.

The first telemetry adapter should emit OpenTelemetry events directly through the OTel API/SDK rather than relying on stdout/stderr scraping.

## Architectural intent

Application code should depend only on the lifecycle abstraction.

For example:

```java
lifecycle.phase(LifecyclePhase.STARTING);
lifecycle.milestone("configuration-loaded");
lifecycle.condition("dependency.amps", ConditionState.UP);
lifecycle.phase(LifecyclePhase.RUNNING);
lifecycle.condition("health", ConditionState.UP);
```

The caller should not know whether the implementation is ultimately using:

- OpenTelemetry
- Loki
- Prometheus / Mimir
- Tempo
- Grafana
- structured logging
- a test sink
- some future backend

## Design goals

The library should be:

- simple to adopt
- language-neutral at the semantic level
- idiomatic in each implementation language
- safe to call from multiple threads
- easy to test
- low-dependency
- programmatically configured
- suitable for use in many services
- explicit rather than framework-driven

## Behavioural model

The library models three independent concepts:

1. Phase
2. Milestone
3. Condition

These concepts are intentionally orthogonal.

The library should not attempt to force them into a single combined state machine.
