using Google.Protobuf;

namespace Lifecycle.Tests;

// Test-only decoder of the asserted fields; schema:
// https://github.com/open-telemetry/opentelemetry-proto/blob/v1.9.0/opentelemetry/proto/logs/v1/logs.proto
internal static class OtlpWire
{
    internal sealed record Event(string Name, ulong Nanos, string Body, int Severity,
        Dictionary<string, object> Attributes, Dictionary<string, object> Resource, byte[] TraceId, byte[] SpanId);

    internal static List<Event> Decode(byte[] payload)
    {

        List<Event> result = new List<Event>();

        foreach (byte[] resourceLogs in Messages(payload, 1))
        {
            Dictionary<string, object> resource = new Dictionary<string, object>();

            foreach (byte[] r in Messages(resourceLogs, 1))
            {
                foreach (byte[] pair in Messages(r, 1))
                {
                    ReadAttribute(pair, resource);
                }
            }

            foreach (byte[] scope in Messages(resourceLogs, 2))
            {
                foreach (byte[] bytes in Messages(scope, 2))
                {
                    using CodedInputStream input = new CodedInputStream(bytes);
                    string name = "";
                    string body = "";
                    ulong nanos = 0;
                    int severity = 0;
                    byte[] trace = [], span = [];
                    Dictionary<string, object> attrs = new Dictionary<string, object>();

                    for (uint tag; (tag = input.ReadTag()) != 0;)
                    {
                        switch (tag >> 3)
                        {
                            case 1:
                                nanos = input.ReadFixed64();
                                break;
                            case 2:
                                severity = input.ReadEnum();
                                break;
                            case 5:
                                body = (string)Scalar(input.ReadBytes().ToByteArray());
                                break;
                            case 6:
                                ReadAttribute(input.ReadBytes().ToByteArray(), attrs);
                                break;
                            case 9:
                                trace = input.ReadBytes().ToByteArray();
                                break;
                            case 10:
                                span = input.ReadBytes().ToByteArray();
                                break;
                            case 12:
                                name = input.ReadString();
                                break;
                            default:
                                input.SkipLastField();
                                break;
                        }
                    }

                    result.Add(new Event(name, nanos, body, severity, attrs, resource, trace, span));
                }
            }
        }

        return result;
    }

    private static List<byte[]> Messages(byte[] bytes, uint field)
    {

        List<byte[]> result = new List<byte[]>();
        using CodedInputStream input = new CodedInputStream(bytes);

        for (uint tag; (tag = input.ReadTag()) != 0;)
        {
            if (tag >> 3 == field)
            {
                result.Add(input.ReadBytes().ToByteArray());
            }
            else
            {
                input.SkipLastField();
            }
        }

        return result;
    }

    private static void ReadAttribute(byte[] bytes, Dictionary<string, object> attributes)
    {

        using CodedInputStream input = new CodedInputStream(bytes);
        string? key = null;
        object? value = null;

        for (uint tag; (tag = input.ReadTag()) != 0;)
        {
            if (tag >> 3 == 1)
            {
                key = input.ReadString();
            }
            else if (tag >> 3 == 2)
            {
                value = Scalar(input.ReadBytes().ToByteArray());
            }
            else
            {
                input.SkipLastField();
            }
        }

        if (key is null || value is null || !attributes.TryAdd(key, value))
        {
            throw new InvalidDataException("Missing or duplicate OTLP attribute");
        }
    }

    private static object Scalar(byte[] bytes)
    {

        using CodedInputStream input = new CodedInputStream(bytes);

        return (input.ReadTag() >> 3) switch
        {
            1 => input.ReadString(),
            2 => input.ReadBool(),
            3 => input.ReadInt64(),
            4 => input.ReadDouble(),
            _ => throw new InvalidDataException("Unexpected OTLP scalar type")
        };
    }
}
