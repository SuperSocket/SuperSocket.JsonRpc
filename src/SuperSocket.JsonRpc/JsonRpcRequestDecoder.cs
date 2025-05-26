using System.Buffers;
using System.Text.Json;
using SuperSocket.ProtoBase;

namespace SuperSocket.JsonRpc;

public class JsonRpcRequestDecoder : IPackageDecoder<JsonRpcRequest>
{
    public JsonRpcRequest Decode(ref ReadOnlySequence<byte> buffer, object context)
    {
        var jsonReader = new Utf8JsonReader(buffer);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);


        if (jsonElement.ValueKind != JsonValueKind.Array)
        {
            return DecodeElement(ref jsonElement);
        }

        // Decode batch requests
        var enumerator = jsonElement.EnumerateArray();

        var firstPackage = default(JsonRpcRequest);
        var prevPackage = default(JsonRpcRequest);

        while (enumerator.MoveNext())
        {
            var element = enumerator.Current;

            var package = DecodeElement(ref element);

            if (firstPackage == null)
            {
                firstPackage = package;
            }
            else
            {
                prevPackage.Next = package;
            }

            prevPackage = package;
        }

        return firstPackage;
    }

    private JsonRpcRequest DecodeElement(ref JsonElement jsonElement)
    {
        var package = new JsonRpcRequest
        {
            Version = jsonElement.GetProperty("jsonrpc").GetString(),
            Id = jsonElement.GetProperty("id").ToString(),
            Method = jsonElement.GetProperty("method").GetString(),
        };

        package.Parameters = jsonElement.TryGetProperty("params", out var paramsElement)
            ? paramsElement.ValueKind == JsonValueKind.Null
                ? null
                : paramsElement.EnumerateArray().Select(p => p as object).ToArray()
            : null;

        return package;
    }
}