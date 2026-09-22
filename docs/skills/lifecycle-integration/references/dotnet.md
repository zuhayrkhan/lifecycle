# .NET integration

## Obtain dependencies

Target .NET 8 or newer. Do not assume the Lifecycle packages are published to NuGet.org.
Use the user's configured feed, or build the selected Lifecycle checkout with Maven:

```sh
# In the Lifecycle checkout; Maven needs a JDK and the build needs the .NET 8 SDK.
./mvnw -pl dotnet-libraries -am verify
```

This produces `Lifecycle.Core.0.1.0.nupkg` and
`Lifecycle.OpenTelemetry.0.1.0.nupkg` in `dotnet-libraries/target/packages`.
Use that directory as a local NuGet source alongside the existing upstream sources.
Maven `install` does not put `.nupkg` files in a NuGet feed.

For a standalone smoke project, use this `Consumer.csproj`. Unlike projects in the
library checkout, it must explicitly set its framework, implicit usings and nullable
context. The console exporter is only the example's transport choice.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Lifecycle.Core" Version="0.1.0" />
    <PackageReference Include="Lifecycle.OpenTelemetry" Version="0.1.0" />
    <PackageReference Include="OpenTelemetry.Exporter.Console" Version="1.19.1" />
  </ItemGroup>
</Project>
```

For that standalone project, restore with both sources (replace the checkout path):

```sh
dotnet restore Consumer.csproj \
  --source /absolute/path/to/lifecycle/dotnet-libraries/target/packages \
  --source https://api.nuget.org/v3/index.json
```

In a corporate consumer, preserve its configured feeds and central package/version
management rather than copying these public-feed arguments blindly.

## Wire the host

Save as `Program.cs` in the smoke project:

```csharp
using Lifecycle.Core;
using Lifecycle.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

ServiceCollection services = new ServiceCollection();

services.AddLogging(logging =>
{
    logging.AddFilter(OpenTelemetryLifecycleSink.CategoryName, LogLevel.Information);
    logging.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService("lifecycle-consumer"));
        options.AddLifecycleTelemetry();
        options.AddConsoleExporter();
    });
});

using ServiceProvider host = services.BuildServiceProvider();
ILifecycle lifecycle = new DefaultLifecycle(
    new OpenTelemetryLifecycleSink(host.GetRequiredService<ILoggerFactory>()));

lifecycle.Phase(LifecyclePhase.Starting);
lifecycle.Milestone("configuration-loaded", new Dictionary<string, object?> { ["config.cached"] = true });
lifecycle.Condition("dependency.database", ConditionState.Up);
lifecycle.Phase(LifecyclePhase.Running);

// The real application runs here, sharing this ILifecycle instance.

lifecycle.Phase(LifecyclePhase.Stopping);
// Finish application shutdown work before declaring Stopped.
lifecycle.Phase(LifecyclePhase.Stopped);

if (!host.GetRequiredService<LoggerProvider>().ForceFlush(10_000))
{
    Console.Error.WriteLine("Lifecycle telemetry flush did not complete successfully");
}
```

Run `dotnet run --project Consumer.csproj --no-restore`. Expect six structured
records. This demonstrates successful startup and orderly shutdown; adapt the
notifications to the actual host's existing success/failure/cleanup paths.

In an existing Generic Host/ASP.NET application, configure its logging pipeline and
register one shared `ILifecycle` in its composition root. Use its `ILoggerFactory`;
do not call `BuildServiceProvider()` to make an extra application container. Preserve
its Resource and existing logging rules. Verify that any provider-specific filtering
also allows Information-level events for `Lifecycle.OpenTelemetry`.

`AddLifecycleTelemetry()` must run before all exporters receiving these records,
including console and OTLP exporters. It restores the original UTC event timestamp
and removes `lifecycle.internal.timestamp_ticks` before export. A successful build
alone does not detect an incorrectly ordered or filtered pipeline.

For OTLP/HTTP, add `OpenTelemetry.Exporter.OpenTelemetryProtocol` version `1.19.1`
and replace `AddConsoleExporter()` with `AddOtlpExporter(...)`, setting
`exporter.Protocol = OtlpExportProtocol.HttpProtobuf` and `exporter.Endpoint` to the
host's full logs URL, for example `http://localhost:4318/v1/logs`.
Import `OpenTelemetry.Exporter`. Reuse approved endpoint/auth configuration.

Flush the actual logger provider while its transport and DI services still exist,
after reporting Stopped and before host disposal. In a host-managed shutdown,
integrate this into its existing shutdown ordering; do not depend solely on provider
disposal to initiate the first export. Handle a failed/timed-out flush through the
host's diagnostics policy without promising backend delivery.

## Core-only tests and diagnostics

Use `InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();` and
`ILifecycle lifecycle = new DefaultLifecycle(sink);`, then inspect `sink.Events`.
Keep tests finite because the sink retains every event. Optional constructor
arguments `time: TimeProvider` and `onError: Action<Exception>` support deterministic
timing and failure diagnostics. Configure a composite's `onError` to observe child
failures that it contains internally. No experimental `Logger.EmitLog`, reflection,
or experimental-warning suppression is needed.
