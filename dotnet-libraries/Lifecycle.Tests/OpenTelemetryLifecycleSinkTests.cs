using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Lifecycle.Core;
using Lifecycle.OpenTelemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using Xunit;

namespace Lifecycle.Tests;

public sealed class OpenTelemetryLifecycleSinkTests
{
    [Fact]
    public async Task SharedFixtureSurvivesBatchedOtlpExportWithOriginalTimestampsAndNativeTypes()
    {

        using OtelDiagnostics diagnostics = new OtelDiagnostics();
        using JsonDocument fixture = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "lifecycle-sequence.json")));
        JsonElement[] steps = fixture.RootElement.EnumerateArray().ToArray();
        ConcurrentQueue<byte[]> payloads = new ConcurrentQueue<byte[]>();
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();

        builder.Logging.ClearProviders();

        await using WebApplication app = builder.Build();

        app.Urls.Add("http://127.0.0.1:0");
        app.MapPost("/v1/logs", async (HttpContext context) =>
        {
            using MemoryStream body = new MemoryStream();

            await context.Request.Body.CopyToAsync(body);
            payloads.Enqueue(body.ToArray());
            context.Response.ContentType = "application/x-protobuf";
            context.Response.StatusCode = 200;
        });

        await app.StartAsync();

        TestTimeProvider time = new TestTimeProvider();
        InMemoryLifecycleEventSink core = new InMemoryLifecycleEventSink();
        List<Exception> failures = new List<Exception>();
        string traceId, spanId;

        ServiceCollection services = new ServiceCollection();

        services.AddLogging(logging => logging.AddOpenTelemetry(options =>
        {
            options.SetResourceBuilder(ResourceBuilder.CreateEmpty().AddService("parity-service"));
            options.AddLifecycleTelemetry();
            options.AddOtlpExporter(exporter =>
            {
                exporter.Endpoint = new Uri(app.Urls.Single() + "/v1/logs");
                exporter.Protocol = OtlpExportProtocol.HttpProtobuf;
                exporter.ExportProcessorType = ExportProcessorType.Batch;
                exporter.BatchExportProcessorOptions.ScheduledDelayMilliseconds = 60_000;
            });
        }));

        using (ServiceProvider host = services.BuildServiceProvider())
        {
            ILoggerFactory factory = host.GetRequiredService<ILoggerFactory>();
            CompositeLifecycleEventSink sink = new CompositeLifecycleEventSink(new ILifecycleEventSink[] { core, new OpenTelemetryLifecycleSink(factory) }, failures.Add);
            DefaultLifecycle lifecycle = new DefaultLifecycle(sink, time, failures.Add);
            using Activity activity = new Activity("parity").SetIdFormat(ActivityIdFormat.W3C)
                .SetParentId("00-0123456789abcdef0123456789abcdef-0123456789abcdef-01").Start();

            traceId = activity.TraceId.ToHexString();
            spanId = activity.SpanId.ToHexString();

            foreach (JsonElement step in steps)
            {
                time.Ticks = step.GetProperty("elapsedMs").GetInt64() * TimeSpan.TicksPerMillisecond;
                time.Wall = DateTimeOffset.Parse(step.GetProperty("expected").GetProperty("timestamp").GetString()!);

                Dictionary<string, object?> attrs = step.GetProperty("attributes").EnumerateObject().ToDictionary(p => p.Name, p => (object?)Scalar(p.Value));
                string name = step.GetProperty("name").GetString()!;

                switch (step.GetProperty("operation").GetString())
                {
                    case "phase":
                        lifecycle.Phase(Enum.Parse<LifecyclePhase>(name, true));
                        break;
                    case "milestone":
                        lifecycle.Milestone(name, attrs);
                        break;
                    case "condition":
                        lifecycle.Condition(name, Enum.Parse<ConditionState>(step.GetProperty("state").GetString()!, true), attrs);
                        break;
                    default:
                        Assert.Fail("Unknown fixture operation");
                        break;
                }
            }

            Assert.True(host.GetRequiredService<LoggerProvider>().ForceFlush(10_000));
        } // Flush before host disposal so lazy transport dependencies are still available.

        OtlpWire.Event[] events = payloads.SelectMany(OtlpWire.Decode).ToArray();

        Assert.Empty(failures);
        Assert.True(steps.Length == events.Length, $"Expected {steps.Length} records, received {events.Length}. {string.Join("\n", diagnostics.Messages)}");
        Assert.Equal(steps.Length, core.Events.Count);

        for (int i = 0; i < steps.Length; i++)
        {
            JsonElement expected = steps[i].GetProperty("expected");
            OtlpWire.Event actual = events[i];

            Assert.Equal(expected.GetProperty("eventName").GetString(), actual.Name);
            Assert.Equal(expected.GetProperty("body").GetString(), actual.Body);
            Assert.Equal(expected.GetProperty("severity").GetInt32(), actual.Severity);

            DateTimeOffset timestamp = DateTimeOffset.Parse(expected.GetProperty("timestamp").GetString()!);

            Assert.Equal((ulong)((timestamp.UtcTicks - DateTimeOffset.UnixEpoch.UtcTicks) * 100), actual.Nanos);
            Assert.Equal(timestamp, core.Events[i].Timestamp);

            Dictionary<string, object> attrs = expected.GetProperty("attributes").EnumerateObject().ToDictionary(p => p.Name, p => Scalar(p.Value));

            Assert.Equal(attrs.Count, actual.Attributes.Count);
            Assert.Equal(attrs.Count, core.Events[i].Attributes.Count);

            foreach ((string key, object value) in attrs)
            {
                Assert.Equal(value.GetType(), actual.Attributes[key].GetType());
                Assert.Equal(value, actual.Attributes[key]);
                Assert.Equal(value, core.Events[i].Attributes[key].Value);
            }

            Assert.Equal("parity-service", actual.Resource["service.name"]);
            Assert.Equal(traceId, Convert.ToHexString(actual.TraceId).ToLowerInvariant());
            Assert.Equal(spanId, Convert.ToHexString(actual.SpanId).ToLowerInvariant());
        }

        await app.StopAsync();
    }

    private static object Scalar(JsonElement value)
    {

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()!,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when value.TryGetInt64(out long number) => number,
            JsonValueKind.Number => value.GetDouble(),
            _ => throw new InvalidDataException("Unsupported fixture scalar")
        };
    }
}

internal sealed class OtelDiagnostics : System.Diagnostics.Tracing.EventListener
{
    internal ConcurrentQueue<string> Messages { get; } = new ConcurrentQueue<string>();

    protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource source)
    {

        if (source.Name.StartsWith("OpenTelemetry", StringComparison.Ordinal))
        {
            EnableEvents(source, System.Diagnostics.Tracing.EventLevel.Verbose);
        }
    }

    protected override void OnEventWritten(System.Diagnostics.Tracing.EventWrittenEventArgs data)
    {

        Messages?.Enqueue($"{data.EventName}: {string.Join(" | ", data.Payload?.ToArray() ?? [])}");
    }
}
