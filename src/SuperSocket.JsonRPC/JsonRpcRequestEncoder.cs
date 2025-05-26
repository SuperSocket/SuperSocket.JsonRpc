using System.Buffers;
using System.Text;
using System.Text.Json;
using SuperSocket.ProtoBase;

namespace SuperSocket.JsonRpc;

public class JsonRpcRequestEncoder : IPackageEncoder<JsonRpcRequest>
{
    public int Encode(IBufferWriter<byte> writer, JsonRpcRequest package)
    {
        if (package == null)
            throw new ArgumentNullException(nameof(package));

        // Check if this is a batch request (has Next packages)
        var packages = new List<JsonRpcRequest>();
        var current = package;
        
        while (current != null)
        {
            packages.Add(current);
            current = current.Next;
        }

        var jsonBytes = packages.Count == 1
            ? EncodeElement(packages[0])
            : EncodeBatch(packages);

        writer.Write(jsonBytes);
        return jsonBytes.Length;
    }

    private byte[] EncodeElement(JsonRpcRequest package)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartObject();
        
        // JSON-RPC version
        writer.WriteString("jsonrpc", package.Version ?? "2.0");
        
        // Method
        if (!string.IsNullOrEmpty(package.Method))
        {
            writer.WriteString("method", package.Method);
        }
        
        // Parameters
        if (package.Parameters != null && package.Parameters.Length > 0)
        {
            writer.WritePropertyName("params");
            writer.WriteStartArray();
            
            foreach (var param in package.Parameters)
            {
                WriteParameterValue(writer, param);
            }
            
            writer.WriteEndArray();
        }
        
        // ID
        if (package.Id != null)
        {
            // Try to parse as number first, then fall back to string
            if (int.TryParse(package.Id, out var intId))
            {
                writer.WriteNumber("id", intId);
            }
            else if (long.TryParse(package.Id, out var longId))
            {
                writer.WriteNumber("id", longId);
            }
            else
            {
                writer.WriteString("id", package.Id);
            }
        }
        
        writer.WriteEndObject();
        writer.Flush();

        return stream.ToArray();
    }

    private byte[] EncodeBatch(List<JsonRpcRequest> packages)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        writer.WriteStartArray();
        
        foreach (var package in packages)
        {
            writer.WriteStartObject();
            
            // JSON-RPC version
            writer.WriteString("jsonrpc", package.Version ?? "2.0");
            
            // Method
            if (!string.IsNullOrEmpty(package.Method))
            {
                writer.WriteString("method", package.Method);
            }
            
            // Parameters
            if (package.Parameters != null && package.Parameters.Length > 0)
            {
                writer.WritePropertyName("params");
                writer.WriteStartArray();
                
                foreach (var param in package.Parameters)
                {
                    WriteParameterValue(writer, param);
                }
                
                writer.WriteEndArray();
            }
            
            // ID
            if (package.Id != null)
            {
                // Try to parse as number first, then fall back to string
                if (int.TryParse(package.Id, out var intId))
                {
                    writer.WriteNumber("id", intId);
                }
                else if (long.TryParse(package.Id, out var longId))
                {
                    writer.WriteNumber("id", longId);
                }
                else
                {
                    writer.WriteString("id", package.Id);
                }
            }
            
            writer.WriteEndObject();
        }
        
        writer.WriteEndArray();
        writer.Flush();

        return stream.ToArray();
    }

    private void WriteParameterValue(Utf8JsonWriter writer, object value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case JsonElement jsonElement:
                jsonElement.WriteTo(writer);
                break;
            case string stringValue:
                writer.WriteStringValue(stringValue);
                break;
            case int intValue:
                writer.WriteNumberValue(intValue);
                break;
            case long longValue:
                writer.WriteNumberValue(longValue);
                break;
            case double doubleValue:
                writer.WriteNumberValue(doubleValue);
                break;
            case float floatValue:
                writer.WriteNumberValue(floatValue);
                break;
            case decimal decimalValue:
                writer.WriteNumberValue(decimalValue);
                break;
            case bool boolValue:
                writer.WriteBooleanValue(boolValue);
                break;
            default:
                // For complex objects, serialize as JSON string
                var json = JsonSerializer.Serialize(value);
                var jsonDoc = JsonDocument.Parse(json);
                jsonDoc.RootElement.WriteTo(writer);
                break;
        }
    }
}
