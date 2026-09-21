# Architecture and cross-language parity

## Scope and dependencies

The public Lifecycle API emits a small immutable domain event through a synchronous sink.
Phase, milestone and condition are independent dimensions. Core packages contain no OTel,
logging framework, transport or dependency-injection dependencies.

The Java adapter uses `LoggerProvider`/`LogRecordBuilder` from the stable OTel API. SDKs,
processors and exporters are host concerns. The .NET adapter integrates `ILogger` with
the stable OTel SDK, so its adapter package depends on that SDK. This dependency difference
is confined to the adapters; neither core exposes OTel types.

There is no product HTTP API or service boundary. The shared event contract and fixtures
are the cross-language protocol. HTTP receivers exist only in export tests.

## Mapping

| Concept | Java | .NET |
|---|---|---|
| Minimum runtime | Java 17 | .NET 8 |
| Lifecycle abstraction | `Lifecycle` | `ILifecycle` |
| Implementation | `DefaultLifecycle` | `DefaultLifecycle` |
| Event | immutable `LifecycleEvent` record | immutable `LifecycleEvent` record |
| Scalar | validated `AttributeValue` record | validated `AttributeValue` record |
| Phase API | `phase(STARTING)` | `Phase(Starting)` |
| Condition API | `condition(name, UP)` | `Condition(name, Up)` |
| Test sink | `InMemoryLifecycleEventSink` | `InMemoryLifecycleEventSink` |
| Fan-out | `CompositeLifecycleEventSink` | `CompositeLifecycleEventSink` |
| Wall clock | injectable `Clock` | injectable `TimeProvider` |
| Monotonic clock | injectable nanosecond supplier | `TimeProvider.GetTimestamp()` |
| Event name | `LogRecordBuilder.setEventName` | `EventId.Name` → OTLP `event_name` |
| Timestamp | `setTimestamp` | timestamp-restoring processor before export |
| Resource | host-owned `SdkLoggerProvider` | host-owned OTel logger provider |

The root Maven reactor builds Java JARs and orchestrates restore/build/test/pack for the
.NET solution. NuGet consumers receive ordinary .NET packages. `pom.xml` supplies their
version; direct SDK builds read that same value in `Directory.Build.props`.
Initial package version is `0.1.0`; event contract revision is 1. No release publishing is
part of the build. Tests use JUnit 5/AssertJ and xUnit without mocking frameworks.

## State and failures

Java uses `AtomicReference.getAndSet` for phase and `ConcurrentHashMap.put` for conditions.
.NET uses `Interlocked.Exchange` for phase and a stable per-name slot in a concurrent
dictionary for conditions. These operations return the actual previous value at the atomic
update. Sinks run afterward, outside state locks; concurrent delivery order may differ.
All attributes are validated/copied before a condition update. Native scalar wrappers prevent
mutable or arbitrary caller objects from entering the event model.

Telemetry failures do not roll back observed state. Both the lifecycle boundary and composite
children contain ordinary sink exceptions. Error callbacks are optional and isolated; callers
wanting diagnostics for individual composite children should configure the composite callback.
Java does not catch `Error`; .NET excludes out-of-memory, stack-overflow and access-violation
exceptions. No claim is made that a hung synchronous sink can be contained without blocking.

## .NET timestamp compatibility

OTel .NET 1.19.1's low-level `Logger.EmitLog` is experimental, while its public `ILogger`
integration is stable. The bridge initially stamps a record at logging time. To retain our
original event timestamp, the sink adds one reserved, integer timestamp marker to its state.
`AddLifecycleTelemetry()` registers a small synchronous processor that sets `LogRecord.Timestamp`
and removes the marker. It only touches our category and records carrying our marker, uses
public APIs, creates no threads and must run before every exporter. ObservedTimestamp remains
the SDK observation time. No reflection, experimental package or warning suppression is used.

The example host explicitly flushes through `LoggerProvider.ForceFlush` before service disposal.
This also avoids an observed SDK 1.19.1 failure where a lazy HTTP client is first initialized
during disposal and resolves an already-disposed service provider. Export still has no delivery
guarantee; the host owns timeout and failure reporting policies.

Relevant upstream sources checked during implementation:

- [Java LogRecordBuilder](https://github.com/open-telemetry/opentelemetry-java/blob/v1.66.0/api/all/src/main/java/io/opentelemetry/api/logs/LogRecordBuilder.java)
- [.NET experimental Logger](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.19.1/src/OpenTelemetry.Api/Logs/Logger.cs)
- [.NET ILogger bridge timestamp](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.19.1/src/OpenTelemetry/Logs/ILogger/OpenTelemetryLogger.cs)
- [.NET mutable SDK LogRecord](https://github.com/open-telemetry/opentelemetry-dotnet/blob/core-1.19.1/src/OpenTelemetry/Logs/LogRecord.cs)
- [OTLP protobuf schema](https://github.com/open-telemetry/opentelemetry-proto/blob/v1.9.0/opentelemetry/proto/logs/v1/logs.proto)

## Acceptance evidence

Both suites replay `fixtures/lifecycle-sequence.json`, asserting the core event and decoded
OTLP payload against the same expected records. Each test starts a loopback HTTP receiver
on an OS-assigned port, batches records through the real SDK/exporter and flushes before shutdown.
Test-only protobuf readers inspect official field numbers with protobuf libraries; they are
not production exporters or a custom wire protocol.

Assertions include native event name, original timestamp, body, severity number, missing versus
present previous attributes, integer/double/boolean/string types (including integers above
JavaScript's exact range), Resource preservation and active trace/span correlation. Unit tests
cover duplicates, unexpected transitions, independent conditions, defensive copies, invalid input,
elapsed-time truncation, concurrent updates, sink failure and composite continuation.

Parity is semantic rather than byte-for-byte: attribute order, instrumentation scope, observed
timestamp and severity text may differ. Java has nanosecond timestamps and .NET has 100-nanosecond
ticks; compare shared timestamps at 100-nanosecond precision. Host filters, enrichment and SDK
attribute limits can affect telemetry, so the golden tests use equivalent, explicit configurations.
