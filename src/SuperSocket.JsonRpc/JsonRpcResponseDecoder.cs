using System.Buffers;
using System.Text.Json;
using SuperSocket.ProtoBase;
using NRPC.Abstractions;

namespace SuperSocket.JsonRpc;

public class JsonRpcResponseDecoder : IPackageDecoder<JsonRpcResponse>
{
    public JsonRpcResponse Decode(ref ReadOnlySequence<byte> buffer, object context)
    {
        var jsonReader = new Utf8JsonReader(buffer);
        var jsonElement = JsonElement.ParseValue(ref jsonReader);

        if (jsonElement.ValueKind != JsonValueKind.Array)
        {
            return DecodeElement(ref jsonElement);
        }

        // Decode batch responses
        var enumerator = jsonElement.EnumerateArray();

        var firstPackage = default(JsonRpcResponse);
        var prevPackage = default(JsonRpcResponse);

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

    private JsonRpcResponse DecodeElement(ref JsonElement jsonElement)
    {
        var package = new JsonRpcResponse
        {
            Version = jsonElement.GetProperty("jsonrpc").GetString(),
        };

        // Set the ID if present  
        if (jsonElement.TryGetProperty("id", out var idElement))
        {
            package.Id = idElement.ToString();
        }

        // A response has either 'result' or 'error', but not both
        if (jsonElement.TryGetProperty("result", out var resultElement))
        {
            // Store the result - assuming Result property accepts object or JsonElement
            package.Result = resultElement;
        }
        else if (jsonElement.TryGetProperty("error", out var errorElement))
        {
            // Create an RpcError from the JSON-RPC error structure
            var code = errorElement.TryGetProperty("code", out var codeElement) && codeElement.TryGetInt32(out var c) ? c : 0;
            var message = errorElement.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : string.Empty;
            var data = errorElement.TryGetProperty("data", out var dataElement) ? dataElement : (object)null;
            
            var error = new RpcError(code, message, data);
            package.Error = error;
        }

        return package;
    }
}
