# Domain Model

## 1. Phase

A phase represents the broad execution phase of the process.

Initial standard phases:

```text
STARTING
RUNNING
STOPPING
STOPPED
```

Example:

```java
lifecycle.phase(LifecyclePhase.STARTING);
```

A phase change should emit an event.

The implementation may remember the previous phase in order to enrich the emitted event.

The library must **not** reject a transition because it is unexpected.

For example, the library should not contain logic that forbids:

```text
RUNNING -> STARTING
```

Even if such a transition looks odd, the telemetry layer should observe and report what the application says happened rather than controlling application behaviour.

---

## 2. Milestone

A milestone represents something noteworthy that happened.

Milestones are events, not state.

Examples:

```text
configuration-loaded
database-connected
reference-data-loaded
initial-position-load-complete
first-market-data-received
```

Example:

```java
lifecycle.milestone("configuration-loaded");
```

Milestones may carry attributes:

```java
lifecycle.milestone(
        "initial-position-load-complete",
        Map.of("position.count", "18342"));
```

The library must not maintain a closed enum of milestone names.

Applications need to be able to define domain-specific milestones.

---

## 3. Condition

A condition represents the current state of an independently varying aspect of the application.

Initial standard condition states:

```text
UP
DOWN
DEGRADED
```

Examples:

```java
lifecycle.condition(
        "health",
        ConditionState.UP);

lifecycle.condition(
        "dependency.amps",
        ConditionState.DOWN);
```

Conditions may change repeatedly over the lifetime of the process.

The library should remember the previous value so an emitted event can contain:

```text
lifecycle.condition.name = dependency.amps
lifecycle.condition.previous = UP
lifecycle.condition.current = DOWN
```

No transition should be rejected.

A duplicate value should still emit an event.

For example:

```java
lifecycle.condition("database", ConditionState.UP);
lifecycle.condition("database", ConditionState.UP);
```

The second event should contain:

```text
previous = UP
current = UP
```

This may be operationally useful because it tells us the application explicitly reassessed the condition.

---

## Why this is not a state machine

The application may simultaneously be:

```text
phase      = RUNNING
health     = DEGRADED
amps       = DOWN
database   = UP
```

These are separate dimensions.

Trying to represent all of them as one state quickly creates meaningless combined states such as:

```text
RUNNING_DEGRADED_AMPS_DOWN_DATABASE_UP
```

The library should instead retain a small amount of contextual state purely to enrich telemetry.
