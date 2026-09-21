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
    options.SetResourceBuilder(ResourceBuilder.CreateEmpty()
        .AddService("lifecycle-dotnet-example", serviceInstanceId: Guid.NewGuid().ToString()));
    options.AddLifecycleTelemetry(); // Must precede exporters.
    if (args.Length == 0) options.AddConsoleExporter();
    else options.AddOtlpExporter(exporter =>
    {
        exporter.Endpoint = new Uri(args[0]);
        exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
    });
}));

using var host = services.BuildServiceProvider();
ILifecycle lifecycle = new DefaultLifecycle(new OpenTelemetryLifecycleSink(host.GetRequiredService<ILoggerFactory>()));
lifecycle.Phase(LifecyclePhase.Starting);
lifecycle.Milestone("configuration-loaded");
lifecycle.Condition("dependency.database", ConditionState.Up);
lifecycle.Condition("dependency.amps", ConditionState.Up);
lifecycle.Milestone("initial-data-loaded", new Dictionary<string, object?> { ["position.count"] = 18342, ["cache.enabled"] = true });
lifecycle.Phase(LifecyclePhase.Running);
lifecycle.Condition("health", ConditionState.Up);
lifecycle.Condition("dependency.amps", ConditionState.Down);
lifecycle.Condition("health", ConditionState.Degraded);
lifecycle.Condition("dependency.amps", ConditionState.Up);
lifecycle.Condition("health", ConditionState.Up);
lifecycle.Phase(LifecyclePhase.Stopping);
lifecycle.Phase(LifecyclePhase.Stopped);
if (!host.GetRequiredService<LoggerProvider>().ForceFlush(10_000))
    throw new InvalidOperationException("Example telemetry flush did not complete successfully");
