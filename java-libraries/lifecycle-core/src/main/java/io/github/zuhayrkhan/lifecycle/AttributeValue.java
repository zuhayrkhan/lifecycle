package io.github.zuhayrkhan.lifecycle;

/** An immutable telemetry scalar, normalized to String, Long, Double or Boolean. */
public record AttributeValue(Object value) {

    public AttributeValue {

        if (value instanceof Byte || value instanceof Short || value instanceof Integer || value instanceof Long) {
            value = ((Number) value).longValue();
        } else if (value instanceof Float || value instanceof Double) {
            double number = ((Number) value).doubleValue();

            if (!Double.isFinite(number)) {
                throw new IllegalArgumentException("Attribute numbers must be finite");
            }

            value = number;
        } else if (!(value instanceof String) && !(value instanceof Boolean)) {
            throw new IllegalArgumentException("Attribute values must be string, signed integer, finite double or boolean");
        }
    }
}
