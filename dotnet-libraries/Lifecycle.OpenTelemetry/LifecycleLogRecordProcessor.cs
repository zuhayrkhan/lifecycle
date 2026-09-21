using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Lifecycle.OpenTelemetry;

internal sealed class LifecycleLogRecordProcessor : BaseProcessor<LogRecord>
{
    public override void OnEnd(LogRecord record)
    {
        if (record.CategoryName != OpenTelemetryLifecycleSink.CategoryName || record.Attributes is null) return;
        var marker = record.Attributes.FirstOrDefault(p => p.Key == OpenTelemetryLifecycleSink.TimestampKey);
        if (marker.Value is not long ticks) return;
        record.Timestamp = new DateTime(ticks, DateTimeKind.Utc);
        record.Attributes = record.Attributes.Where(p => p.Key != OpenTelemetryLifecycleSink.TimestampKey).ToArray();
    }
}
