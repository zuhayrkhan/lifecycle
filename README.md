# Lifecycle telemetry

Small Java 17+ and .NET 8 libraries for reporting application **phases**, **milestones**
and independent **conditions**. The core libraries have no OpenTelemetry dependencies.
Adapters emit structured OTel log events directly in-process.

The library observes what the application reports. It does not orchestrate startup,
probe dependencies, aggregate health, or enforce state transitions.

## Build both runtimes with Maven

Prerequisites: JDK 17+ and the .NET 8 SDK. The Maven wrapper downloads Maven 3.9.11;
dependency restore requires Maven Central and NuGet access. On Windows use `mvnw.cmd`.

```sh
./mvnw clean verify
```

This compiles both implementations, runs unit tests and real OTLP/HTTP export tests,
and produces Java JARs and NuGet packages. Tests start ephemeral loopback receivers;
no Collector, Docker, credentials, or external telemetry backend is required.

| Path | Purpose |
|---|---|
| `java-libraries/lifecycle-core` | Java API, immutable events, state tracking and sinks |
| `java-libraries/lifecycle-otel` | Java OTel API adapter |
| `java-libraries/lifecycle-example` | Java sample host with console or OTLP exporter |
| `dotnet-libraries/Lifecycle.Core` | Equivalent .NET API and core |
| `dotnet-libraries/Lifecycle.OpenTelemetry` | Stable ILogger/OTel adapter and timestamp processor |
| `dotnet-libraries/Lifecycle.Example` | .NET sample host |
| `dotnet-libraries/Lifecycle.Tests` | .NET unit and wire-parity tests |
| `fixtures/lifecycle-sequence.json` | Shared inputs and expected emitted telemetry |

Maven drives the .NET lifecycle through `exec-maven-plugin`: initialize → restore,
compile → build, test → test, package → NuGet pack. `clean` removes the .NET `bin`,
`obj` and `target` outputs. The SDK remains responsible for C# compilation.
The root POM is the version source for both runtimes, including direct `dotnet` builds.

```sh
# Java only (build and tests)
./mvnw -f java-libraries/pom.xml verify

# .NET only, still driven by Maven
./mvnw -pl dotnet-libraries -am verify

# Install Java artifacts to the local Maven cache; also build .NET packages
./mvnw install

# Native .NET commands are available too
cd dotnet-libraries
dotnet build Lifecycle.sln -c Release
dotnet test Lifecycle.sln -c Release
dotnet pack Lifecycle.sln -c Release -o target/packages
```

Java artifacts appear in each module's `target/`; NuGet packages appear in
`dotnet-libraries/target/packages/`. Nothing is published by the build. `-DskipTests`
skips both Java and .NET tests. Maven `install` installs the .NET orchestration POM;
the `.nupkg` files are consumed with NuGet, not as Maven dependencies.

## Java usage

Use Maven artifacts `io.github.zuhayrkhan.lifecycle:lifecycle-core` and
`io.github.zuhayrkhan.lifecycle:lifecycle-otel`, version `0.1.0`.

```java
import io.github.zuhayrkhan.lifecycle.*;
import io.github.zuhayrkhan.lifecycle.otel.OpenTelemetryLifecycleSink;
import java.util.Map;

// sdkLoggerProvider is configured and owned by the host application.
Lifecycle lifecycle = new DefaultLifecycle(new OpenTelemetryLifecycleSink(sdkLoggerProvider));
lifecycle.phase(LifecyclePhase.STARTING);
lifecycle.milestone("configuration-loaded");
lifecycle.condition("dependency.database", ConditionState.UP);
lifecycle.milestone("initial-data-loaded", Map.of("position.count", 18342));
lifecycle.phase(LifecyclePhase.RUNNING);
lifecycle.condition("health", ConditionState.UP);
```

The Java adapter depends only on `opentelemetry-api`; the host supplies a compatible
SDK and exporter. The tested OTel version is 1.66.0. See the
[complete Java host](java-libraries/lifecycle-example/src/main/java/io/github/zuhayrkhan/lifecycle/example/LifecycleExample.java)
for Resource identity and bounded flushing before shutdown.

## .NET usage

Use NuGet packages `Lifecycle.Core` and `Lifecycle.OpenTelemetry`, version `0.1.0`.
The latter uses OTel 1.19.1 and stable Microsoft logging APIs, with no experimental
Logs Bridge APIs. The host supplies the exporter package and configuration.

```csharp
using Lifecycle.Core;
using Lifecycle.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

var services = new ServiceCollection();
services.AddLogging(logging => logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService("my-service"));
    options.AddLifecycleTelemetry(); // Register BEFORE any exporter.
    options.AddOtlpExporter(exporter =>
    {
        exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
        exporter.Endpoint = new Uri("http://localhost:4318/v1/logs");
    });
}));

using var host = services.BuildServiceProvider();
ILifecycle lifecycle = new DefaultLifecycle(
    new OpenTelemetryLifecycleSink(host.GetRequiredService<ILoggerFactory>()));
lifecycle.Phase(LifecyclePhase.Starting);
lifecycle.Milestone("configuration-loaded");
lifecycle.Condition("dependency.database", ConditionState.Up);
lifecycle.Phase(LifecyclePhase.Running);

// At application shutdown, while host services/transport dependencies still exist:
lifecycle.Phase(LifecyclePhase.Stopping);
lifecycle.Phase(LifecyclePhase.Stopped);
var flushed = host.GetRequiredService<LoggerProvider>().ForceFlush(10_000);
// The host decides how to report an unsuccessful best-effort flush.
```

`AddLifecycleTelemetry()` restores the original event timestamp and removes the private
timestamp marker before batching/export. Omitting it, or registering it after an exporter,
does not meet the event contract. Use a dedicated factory/pipeline if your application has
different logging policies. Enable Information-level logs for `Lifecycle.OpenTelemetry`;
filters can otherwise discard events. Scopes/other processors may add host-specific attributes.

Explicitly flush **before** disposing the host. In the tested SDK, a first HTTP export deferred
until disposal can fail while resolving an already-disposed `IHttpClientFactory` dependency.
The [complete .NET host](dotnet-libraries/Lifecycle.Example/Program.cs) demonstrates the tested order.

## Run the examples

After `./mvnw install`:

```sh
./mvnw -q -pl java-libraries/lifecycle-example exec:java \
  -Dexec.mainClass=io.github.zuhayrkhan.lifecycle.example.LifecycleExample

dotnet run --project dotnet-libraries/Lifecycle.Example -c Release --no-build
```

With no arguments, the examples use SDK console exporters so you can inspect the records.
They do not rely on stdout scraping. To send to a running Collector, pass the full OTLP/HTTP
logs URL as the single argument (`-Dexec.args=http://localhost:4318/v1/logs` for Java,
`-- http://localhost:4318/v1/logs` for .NET).

A normalized condition record looks like this; attributes retain their native value types:

```json
{
  "eventName": "application.lifecycle.condition_changed",
  "timestamp": "2026-09-21T12:00:00.8751234Z",
  "body": "application.lifecycle.condition_changed",
  "severity": 9,
  "attributes": {
    "lifecycle.condition.name": "dependency.amps",
    "lifecycle.condition.previous": "UP",
    "lifecycle.condition.current": "DOWN",
    "lifecycle.elapsed_ms": 875,
    "reason": "connection lost"
  }
}
```

## Behaviour and tests

- Share one Lifecycle instance per application/process. It remembers previous phase and
  condition values; elapsed time starts at instance creation.
- Every valid call is emitted, including duplicate values. Updates are atomic; concurrent
  delivery order is unspecified. No phase/condition transition is forbidden.
- Caller attributes are copied and validated. Supported values are string, signed integer,
  finite floating point and boolean. Reserved keys, nulls and unsupported values throw
  before updating state. See the [normative contract](docs/event-contract.md).
- Ordinary sink exceptions do not escape lifecycle calls; state is retained. Composite sinks
  attempt every child. Optional error callbacks observe failures and must return promptly.
  Fatal runtime failures are not deliberately caught.
- Sinks are synchronous. The library adds no queues, retries or worker threads. Transport,
  batching and flushing belong to the host's SDK. Export is best effort.
- Condition names should come from a bounded application-defined set. The in-memory sink
  is deliberately unbounded and intended for tests or short-lived examples.

For tests, inject `InMemoryLifecycleEventSink` without OTel. Java accepts a `Clock` and
monotonic `LongSupplier`; .NET accepts a `TimeProvider`. `CompositeLifecycleEventSink`
fans out to independent sinks. Both test suites also replay the same fixture through actual
OTLP/HTTP exporters and decode protobuf fields to verify the native wire representation.

See [architecture and parity](docs/architecture.md) for the implementation choices and
[the original brief](docs/original-pack/README.md) for provenance. Schema/API compatibility
changes are reviewed together across both runtimes; all packages currently share one version.
