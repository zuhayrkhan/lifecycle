package io.github.zuhayrkhan.lifecycle.example;

import io.github.zuhayrkhan.lifecycle.*;
import io.github.zuhayrkhan.lifecycle.otel.OpenTelemetryLifecycleSink;
import io.opentelemetry.api.common.Attributes;
import io.opentelemetry.exporter.logging.SystemOutLogRecordExporter;
import io.opentelemetry.exporter.otlp.http.logs.OtlpHttpLogRecordExporter;
import io.opentelemetry.sdk.logs.SdkLoggerProvider;
import io.opentelemetry.sdk.logs.export.BatchLogRecordProcessor;
import io.opentelemetry.sdk.logs.export.LogRecordExporter;
import io.opentelemetry.sdk.resources.Resource;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.TimeUnit;

public final class LifecycleExample {
    private LifecycleExample() {}

    public static void main(String[] args) {
        LogRecordExporter exporter = args.length == 0 ? SystemOutLogRecordExporter.create()
                : OtlpHttpLogRecordExporter.builder().setEndpoint(args[0]).build();
        try (var provider = SdkLoggerProvider.builder()
                .setResource(Resource.create(Attributes.builder()
                        .put("service.name", "lifecycle-java-example")
                        .put("service.instance.id", UUID.randomUUID().toString()).build()))
                .addLogRecordProcessor(BatchLogRecordProcessor.builder(exporter).build()).build()) {
            Lifecycle lifecycle = new DefaultLifecycle(new OpenTelemetryLifecycleSink(provider));
            lifecycle.phase(LifecyclePhase.STARTING);
            lifecycle.milestone("configuration-loaded");
            lifecycle.condition("dependency.database", ConditionState.UP);
            lifecycle.condition("dependency.amps", ConditionState.UP);
            lifecycle.milestone("initial-data-loaded", Map.of("position.count", 18342, "cache.enabled", true));
            lifecycle.phase(LifecyclePhase.RUNNING);
            lifecycle.condition("health", ConditionState.UP);
            lifecycle.condition("dependency.amps", ConditionState.DOWN);
            lifecycle.condition("health", ConditionState.DEGRADED);
            lifecycle.condition("dependency.amps", ConditionState.UP);
            lifecycle.condition("health", ConditionState.UP);
            lifecycle.phase(LifecyclePhase.STOPPING);
            lifecycle.phase(LifecyclePhase.STOPPED);
            if (!provider.forceFlush().join(10, TimeUnit.SECONDS).isSuccess())
                throw new IllegalStateException("Example telemetry flush did not complete successfully");
        }
    }
}
