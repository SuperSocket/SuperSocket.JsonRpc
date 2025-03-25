using System.Buffers;
using System.Text.Json;
using SuperSocket.ProtoBase;

namespace SuperSocket.JsonRpc;

public class JsonRPCPackageDecoder : IPackageDecoder<JsonRpcPackageInfo>
{
    public JsonRpcPackageInfo Decode(ref ReadOnlySequence<byte> buffer, object context)
    {
        var jsonReader = new Utf8JsonReader(buffer);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);


        if (jsonElement.ValueKind != JsonValueKind.Array)
        {
            return DecodeElement(ref jsonElement);
        }

        // Decode batch requests
        var enumerator = jsonElement.EnumerateArray();

        var firstPackage = default(JsonRpcPackageInfo);
        var prevPackage = default(JsonRpcPackageInfo);

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

    private JsonRpcPackageInfo DecodeElement(ref JsonElement jsonElement)
    {
        return new JsonRpcPackageInfo
        {
            Version = jsonElement.GetProperty("jsonrpc").GetString(),
            Id = jsonElement.GetProperty("id").ToString(),
            Method = jsonElement.GetProperty("method").GetString(),
            Parameters = jsonElement.GetProperty("params")
        };
    }
}