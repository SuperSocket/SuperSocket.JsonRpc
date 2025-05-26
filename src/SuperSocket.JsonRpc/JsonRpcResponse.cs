using NRPC.Abstractions;

namespace SuperSocket.JsonRpc
{
    public class JsonRpcResponse : RpcResponse
    {
        public string Version { get; set; }

        public JsonRpcResponse Next { get; set; }
    }
}