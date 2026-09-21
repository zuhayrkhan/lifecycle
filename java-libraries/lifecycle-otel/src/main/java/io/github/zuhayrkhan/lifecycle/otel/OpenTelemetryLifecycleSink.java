package io.github.zuhayrkhan.lifecycle.otel;

import io.github.zuhayrkhan.lifecycle.LifecycleEvent;
import io.github.zuhayrkhan.lifecycle.LifecycleEventSink;
import io.opentelemetry.api.logs.Logger;
import io.opentelemetry.api.logs.LoggerProvider;
import io.opentelemetry.api.logs.Severity;
import java.util.Objects;

/** Uses the host's provider; neither closes it nor configures transport or Resource identity. */
public final class OpenTelemetryLifecycleSink implements LifecycleEventSink {
    public static final String SCOPE_NAME = "io.github.zuhayrkhan.lifecycle";
    private final Logger logger;

    public OpenTelemetryLifecycleSink(LoggerProvider provider) {
        logger = Objects.requireNonNull(provider, "provider").get(SCOPE_NAME);
    }

    @Override
    public void emit(LifecycleEvent event) {
        var builder = logger.logRecordBuilder().setEventName(event.name()).setTimestamp(event.timestamp())
                .setSeverity(Severity.INFO).setBody(event.name());
        event.attributes().forEach((key, attribute) -> {
            Object value = attribute.value();
            if (value instanceof String text) builder.setAttribute(key, text);
            else if (value instanceof Long number) builder.setAttribute(key, number.longValue());
            else if (value instanceof Double number) builder.setAttribute(key, number.doubleValue());
            else if (value instanceof Boolean flag) builder.setAttribute(key, flag.booleanValue());
        });
        builder.emit();
    }
}
