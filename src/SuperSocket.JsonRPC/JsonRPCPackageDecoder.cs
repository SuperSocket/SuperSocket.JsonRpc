using System.Buffers;
using System.Text.Json;
using SuperSocket.ProtoBase;

namespace SuperSocket.JsonRPC;

public class JsonRPCPackageDecoder : IPackageDecoder<JsonRPCPackageInfo>
{
    public JsonRPCPackageInfo Decode(ref ReadOnlySequence<byte> buffer, object context)
    {
        var jsonReader = new Utf8JsonReader(buffer);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);


        if (jsonElement.ValueKind != JsonValueKind.Array)
        {
            return DecodeElement(ref jsonElement);
        }

        // Decode batch requests
        var enumerator = jsonElement.EnumerateArray();

        var firstPackage = default(JsonRPCPackageInfo);
        var prevPackage = default(JsonRPCPackageInfo);

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

    private JsonRPCPackageInfo DecodeElement(ref JsonElement jsonElement)
    {
        return new JsonRPCPackageInfo
        {
            Version = jsonElement.GetProperty("jsonrpc").GetString(),
            Id = jsonElement.GetProperty("id").ToString(),
            Method = jsonElement.GetProperty("method").GetString(),
            Parameters = jsonElement.GetProperty("params")
        };
    }
}