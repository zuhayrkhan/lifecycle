using OpenTelemetry.Logs;

namespace Lifecycle.OpenTelemetry;

public static class LifecycleLoggingExtensions
{
    /// <summary>Register before exporters so the event timestamp is restored before batching/export.</summary>
    public static OpenTelemetryLoggerOptions AddLifecycleTelemetry(this OpenTelemetryLoggerOptions options)
    {

        ArgumentNullException.ThrowIfNull(options);

        options.AddProcessor(new LifecycleLogRecordProcessor());

        return options;
    }
}
