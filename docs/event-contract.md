# Lifecycle event contract — version 1

This is the normative contract for both implementations. It resolves the choices in
Myra's [original pack](original-pack/README.md) using the review decisions agreed on
21 September 2026. The original pack is retained unchanged for provenance.

## Operations and state

One explicitly constructed, shared Lifecycle instance represents one application/process.
Construction emits nothing. Its elapsed-time origin is construction, not OS process start.
There is initially no phase and no value for any named condition.

Every valid call emits one event to the configured sink, including duplicate phases and
conditions. Milestones have no remembered state. All phase and condition transitions are
accepted. Previous values are always included when known, otherwise the key is omitted.
STOPPED is the application's declaration before exit; it does not prove process termination.

| Operation | OTel EventName | Required attributes | Optional attributes |
|---|---|---|---|
| Phase | `application.lifecycle.phase_changed` | `lifecycle.phase.current` | `lifecycle.phase.previous` |
| Milestone | `application.lifecycle.milestone` | `lifecycle.milestone` | Caller attributes |
| Condition | `application.lifecycle.condition_changed` | `lifecycle.condition.name`, `lifecycle.condition.current` | `lifecycle.condition.previous`, caller attributes |

Every event also contains `lifecycle.elapsed_ms`, a signed 64-bit integer obtained by
truncating nonnegative monotonic elapsed duration to whole milliseconds. Phases are
`STARTING`, `RUNNING`, `STOPPING`, `STOPPED`; condition states are `UP`, `DOWN`, `DEGRADED`.
Names and attribute keys are nonblank, case-sensitive, preserved verbatim.

## Values and validation

Caller values support strings, signed 64-bit integers, finite IEEE-754 doubles and booleans.
Java byte/short/int/long and C# sbyte/short/int/long normalize to signed 64-bit; float/double
normalize to double. C# unsigned integers, decimals, arbitrary objects, collections, null
values, blank keys and nonfinite numbers are rejected. There is no implicit stringification.
Both milestone and condition attributes use these rules. Inputs are defensively copied.
Empty strings are valid attribute values; names/keys consisting solely of whitespace are not.
Whitespace means Unicode White_Space (including NBSP), identically in both runtimes.

Caller keys starting with `lifecycle.` are reserved. `{OriginalFormat}` is also reserved
because the .NET logging bridge interprets it specially. Invalid input throws a caller
argument exception before changing remembered state or emitting. Null required dependencies fail
at construction (optional time/error callbacks may use defaults). Validation errors are distinct from telemetry transport failures.

## Concurrency and failure

Updates are atomic per phase or condition name. Each previous/current pair reflects that
atomic update. Concurrent emissions may arrive out of order; no global order is guaranteed.
Sinks must support concurrent calls. No sink call executes while a lifecycle state lock is held.

Ordinary sink exceptions are contained. State remains updated even when emission fails.
A composite attempts every child, independently containing each failure. An optional error
callback observes failures; ordinary exceptions from that callback are also contained.
Fatal runtime failures are not deliberately contained. Synchronous sinks and error callbacks
must return promptly: no retries, queues, threads, timeouts or nonblocking guarantees are added.

## OTel mapping

EventName is the native top-level OTel/OTLP field, not an `event.name` attribute. Timestamp
is the original event wall-clock instant in UTC. Elapsed time uses a separate monotonic clock.
Body is the event name; severity number is INFO (9). Severity text and instrumentation scope
may use runtime conventions. Active trace/span context is inherited when present. SDK Resource
owns service/process identity; the library neither creates a competing identity nor owns the SDK.

The Java adapter uses the stable Logs API. The .NET adapter uses stable ILogger APIs plus
a synchronous processor registered **before exporters** to restore the original timestamp and
remove its private transport attribute. No experimental logs bridge API is used.
ObservedTimestamp is SDK observation time and excluded from parity comparisons.
Timestamps compare at 100-nanosecond precision (the .NET clock resolution); elapsed values
compare at whole milliseconds. Body, severity number, event names, key presence and native
attribute types must agree. Resource is host-supplied and must survive export.

Hosts configure SDK batching/export/retry and emit STOPPED before flushing/shutting down the
SDK. Export is best effort, with no delivery promise on normal shutdown or abrupt termination.
Condition names should be a bounded set of application aspects; each distinct name is retained
for the Lifecycle instance lifetime. There is no automatic eviction/reset.
