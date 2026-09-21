namespace Lifecycle.Core;

/// <summary>An immutable telemetry scalar normalized to string, long, double or bool.</summary>
public sealed record AttributeValue
{
    public object Value { get; }

    public AttributeValue(object? value)
    {

        Value = value switch
        {
            string or bool or long => value,
            sbyte n => (long)n,
            short n => (long)n,
            int n => (long)n,
            float n when float.IsFinite(n) => (double)n,
            double n when double.IsFinite(n) => n,
            _ => throw new ArgumentException("Attribute values must be string, signed integer, finite double or boolean", nameof(value))
        };
    }
}
