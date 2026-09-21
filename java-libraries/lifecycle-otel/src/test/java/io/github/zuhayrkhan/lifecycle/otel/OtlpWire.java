package io.github.zuhayrkhan.lifecycle.otel;

import com.google.protobuf.CodedInputStream;
import java.io.IOException;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/** Test-only decoder for the fields asserted in fixtures; uses the official OTLP field numbers.
 * See https://github.com/open-telemetry/opentelemetry-proto/blob/v1.9.0/opentelemetry/proto/logs/v1/logs.proto
 */
final class OtlpWire {
    record Event(String name, long nanos, String body, int severity, Map<String, Object> attributes,
                 Map<String, Object> resource, byte[] traceId, byte[] spanId) {}

    static List<Event> decode(byte[] payload) throws IOException {
        var result = new ArrayList<Event>();
        for (var resourceLogs : messages(payload, 1)) {
            var resource = new HashMap<String, Object>();
            for (var r : messages(resourceLogs, 1))
                for (var pair : messages(r, 1)) readAttribute(pair, resource);
            for (var scope : messages(resourceLogs, 2)) {
                for (var bytes : messages(scope, 2)) {
                    var input = CodedInputStream.newInstance(bytes);
                    String name = "", body = ""; long nanos = 0; int severity = 0;
                    byte[] trace = {}, span = {};
                    var attrs = new HashMap<String, Object>();
                    for (int tag; (tag = input.readTag()) != 0;) {
                        switch (tag >>> 3) {
                            case 1 -> nanos = input.readFixed64();
                            case 2 -> severity = input.readEnum();
                            case 5 -> body = (String) scalar(input.readByteArray());
                            case 6 -> readAttribute(input.readByteArray(), attrs);
                            case 9 -> trace = input.readByteArray();
                            case 10 -> span = input.readByteArray();
                            case 12 -> name = input.readString();
                            default -> input.skipField(tag);
                        }
                    }
                    result.add(new Event(name, nanos, body, severity, attrs, resource, trace, span));
                }
            }
        }
        return result;
    }

    private static List<byte[]> messages(byte[] bytes, int field) throws IOException {
        var result = new ArrayList<byte[]>();
        var input = CodedInputStream.newInstance(bytes);
        for (int tag; (tag = input.readTag()) != 0;) {
            if (tag >>> 3 == field) result.add(input.readByteArray());
            else input.skipField(tag);
        }
        return result;
    }

    private static void readAttribute(byte[] bytes, Map<String, Object> attributes) throws IOException {
        var input = CodedInputStream.newInstance(bytes);
        String key = null; Object value = null;
        for (int tag; (tag = input.readTag()) != 0;) {
            if (tag >>> 3 == 1) key = input.readString();
            else if (tag >>> 3 == 2) value = scalar(input.readByteArray());
            else input.skipField(tag);
        }
        if (key == null || value == null || attributes.putIfAbsent(key, value) != null)
            throw new IOException("Missing or duplicate OTLP attribute");
    }

    private static Object scalar(byte[] bytes) throws IOException {
        var input = CodedInputStream.newInstance(bytes);
        return switch (input.readTag() >>> 3) {
            case 1 -> input.readString();
            case 2 -> input.readBool();
            case 3 -> input.readInt64();
            case 4 -> input.readDouble();
            default -> throw new IOException("Unexpected OTLP scalar type");
        };
    }
}
