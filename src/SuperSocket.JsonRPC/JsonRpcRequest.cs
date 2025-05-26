namespace SuperSocket.JsonRpc;

using System.Text.Json;
using NRPC.Abstractions;

public class JsonRpcRequest : RpcRequest
{
    public string Version { get; set; }

    public JsonRpcRequest Next { get; set; }
}