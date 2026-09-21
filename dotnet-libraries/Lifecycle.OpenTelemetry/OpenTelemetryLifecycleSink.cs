using Lifecycle.Core;
using Microsoft.Extensions.Logging;

namespace Lifecycle.OpenTelemetry;

/// <summary>Uses a host-owned factory configured with AddLifecycleTelemetry before exporters.</summary>
public sealed class OpenTelemetryLifecycleSink : ILifecycleEventSink
{
    public const string CategoryName = "Lifecycle.OpenTelemetry";
    internal const string TimestampKey = "lifecycle.internal.timestamp_ticks";
    private readonly ILogger logger;

    public OpenTelemetryLifecycleSink(ILoggerFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        logger = factory.CreateLogger(CategoryName);
    }

    public void Emit(LifecycleEvent lifecycleEvent)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvent);
        var values = lifecycleEvent.Attributes.Select(pair =>
            new KeyValuePair<string, object?>(pair.Key, pair.Value.Value)).ToList();
        values.Add(new(TimestampKey, lifecycleEvent.Timestamp.UtcTicks));
        logger.Log(LogLevel.Information, new EventId(0, lifecycleEvent.Name), values, null,
            (_, _) => lifecycleEvent.Name);
    }
}
