package io.github.zuhayrkhan.lifecycle.otel;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.sun.net.httpserver.HttpServer;
import io.github.zuhayrkhan.lifecycle.*;
import io.opentelemetry.api.common.Attributes;
import io.opentelemetry.api.trace.*;
import io.opentelemetry.context.Scope;
import io.opentelemetry.exporter.otlp.http.logs.OtlpHttpLogRecordExporter;
import io.opentelemetry.sdk.logs.SdkLoggerProvider;
import io.opentelemetry.sdk.logs.export.BatchLogRecordProcessor;
import io.opentelemetry.sdk.resources.Resource;
import org.junit.jupiter.api.Test;
import java.io.InputStream;
import java.net.InetSocketAddress;
import java.time.*;
import java.util.*;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicLong;
import java.util.concurrent.atomic.AtomicReference;
import static org.assertj.core.api.Assertions.*;

class OpenTelemetryLifecycleSinkTest {
    @Test
    void sharedFixtureSurvivesRealBatchedOtlpExportWithOriginalTimestampsAndNativeTypes() throws Exception {

        ObjectMapper mapper = new ObjectMapper();
        JsonNode steps;

        try (InputStream fixture = getClass().getResourceAsStream("/lifecycle-sequence.json")) {
            assertThat(fixture).isNotNull();
            steps = mapper.readTree(fixture);
        }

        LinkedBlockingQueue<byte[]> payloads = new LinkedBlockingQueue<byte[]>();
        HttpServer server = HttpServer.create(new InetSocketAddress("127.0.0.1", 0), 0);

        server.createContext("/v1/logs", exchange -> {
            payloads.add(exchange.getRequestBody().readAllBytes());
            exchange.getResponseHeaders().add("Content-Type", "application/x-protobuf");
            exchange.sendResponseHeaders(200, -1);
            exchange.close();
        });

        server.start();

        AtomicLong ticks = new AtomicLong();
        AtomicReference<Instant> wall = new AtomicReference<Instant>(Instant.parse("2026-09-21T12:00:00Z"));
        Clock clock = new Clock() {

            public ZoneId getZone() {

                return ZoneOffset.UTC;
            }

            public Clock withZone(ZoneId zone) {

                return this;
            }

            public Instant instant() {

                return wall.get();
            }
        };

        List<RuntimeException> failures = new ArrayList<RuntimeException>();
        OtlpHttpLogRecordExporter exporter = OtlpHttpLogRecordExporter.builder()
                .setEndpoint("http://127.0.0.1:" + server.getAddress().getPort() + "/v1/logs").build();

        try (SdkLoggerProvider provider = SdkLoggerProvider.builder()
                .setResource(Resource.create(Attributes.builder().put("service.name", "parity-service").build()))
                .addLogRecordProcessor(BatchLogRecordProcessor.builder(exporter).setScheduleDelay(Duration.ofDays(1)).build())
                .build()) {

            InMemoryLifecycleEventSink core = new InMemoryLifecycleEventSink();
            DefaultLifecycle lifecycle = new DefaultLifecycle(CompositeLifecycleEventSink.of(core, new OpenTelemetryLifecycleSink(provider)),
                    clock, ticks::get, failures::add);

            String traceId = "0123456789abcdef0123456789abcdef";
            String spanId = "0123456789abcdef";

            try (Scope ignored = Span.wrap(SpanContext.create(traceId, spanId, TraceFlags.getSampled(), TraceState.getDefault())).makeCurrent()) {
                for (JsonNode step : steps) {
                    ticks.set(step.get("elapsedMs").asLong() * 1_000_000);
                    wall.set(Instant.parse(step.get("expected").get("timestamp").asText()));

                    Map<String, Object> attrs = mapper.convertValue(step.get("attributes"), new TypeReference<Map<String, Object>>() {});
                    String name = step.get("name").asText();

                    switch (step.get("operation").asText()) {
                        case "phase" -> lifecycle.phase(LifecyclePhase.valueOf(name));
                        case "milestone" -> lifecycle.milestone(name, attrs);
                        case "condition" -> lifecycle.condition(name, ConditionState.valueOf(step.get("state").asText()), attrs);
                        default -> fail("Unknown fixture operation");
                    }
                }
            }

            assertThat(provider.forceFlush().join(10, TimeUnit.SECONDS).isSuccess()).isTrue();

            byte[] payload = payloads.poll(10, TimeUnit.SECONDS);

            assertThat(payload).isNotNull();

            List<OtlpWire.Event> events = OtlpWire.decode(payload);

            assertThat(events).hasSize(steps.size());
            assertThat(core.events()).hasSize(steps.size());
            assertThat(failures).isEmpty();

            for (int i = 0; i < steps.size(); i++) {
                JsonNode expected = steps.get(i).get("expected");
                OtlpWire.Event actual = events.get(i);

                assertThat(actual.name()).isEqualTo(expected.get("eventName").asText());
                assertThat(actual.body()).isEqualTo(expected.get("body").asText());
                assertThat(actual.severity()).isEqualTo(expected.get("severity").asInt());

                Instant timestamp = Instant.parse(expected.get("timestamp").asText());

                assertThat(actual.nanos()).isEqualTo(timestamp.getEpochSecond() * 1_000_000_000L + timestamp.getNano());

                Map<String, Object> attrs = mapper.convertValue(expected.get("attributes"), new TypeReference<Map<String, Object>>() {});

                attrs.replaceAll((key, value) -> new AttributeValue(value).value());

                assertThat(actual.attributes()).isEqualTo(attrs);

                Map<String, Object> coreAttrs = new HashMap<String, Object>();

                core.events().get(i).attributes().forEach((key, value) -> coreAttrs.put(key, value.value()));

                assertThat(coreAttrs).isEqualTo(attrs);
                assertThat(actual.resource().get("service.name")).isEqualTo("parity-service");
                assertThat(HexFormat.of().formatHex(actual.traceId())).isEqualTo(traceId);
                assertThat(HexFormat.of().formatHex(actual.spanId())).isEqualTo(spanId);
            }
        } finally {
            server.stop(0);
        }
    }
}
