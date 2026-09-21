# Non-Goals

Do not build any of the following unless a later requirement explicitly asks for it:

- a workflow engine
- a finite-state-machine framework
- readiness/liveness probing
- dependency orchestration
- application startup sequencing
- a service supervisor
- an event bus
- a telemetry backend
- a retry framework
- a persistence mechanism
- a queueing framework
- a health aggregation engine
- a dependency graph engine
- automatic startup gating

The library observes lifecycle behaviour.

It does not control lifecycle behaviour.

## Anti-overengineering guidance

Avoid adding abstractions before they have a concrete use.

Prefer:

```text
small interfaces
immutable event objects
simple adapters
small concurrency primitives
clear names
explicit configuration
```

Avoid:

```text
plugin systems
reflection
annotation scanning
code generation
DSLs
state-machine engines
generalised middleware pipelines
complex builders
global registries
hidden threads
```

The library may eventually sit inside a large proportion of the service estate.

Optimise for code that is easy to understand during an incident at 03:00.
