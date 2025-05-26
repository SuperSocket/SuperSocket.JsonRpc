using System.Net;
using System.Text;
using System.Text.Json;
using NRPC.Abstractions;
using SuperSocket.Client;
using SuperSocket.JsonRpc;

namespace SuperSocket.JsonRpc.Caller;

/// <summary>
/// A JSON-RPC client that uses SuperSocket.Client to communicate with JSON-RPC servers
/// </summary>
class JsonRpcCaller : EasyClient<JsonRpcResponse>, IRpcConnection
{
    private readonly JsonRpcRequestEncoder _encoder = new JsonRpcRequestEncoder();

    public JsonRpcCaller()
        : base(new JsonRpcResponsePipelineFilter()
        {
            Decoder = new JsonRpcResponseDecoder()
        })
    {
    }

    public async Task<RpcResponse> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        return await base.ReceiveAsync();
    }

    public async Task SendAsync(RpcRequest request)
    {
        await this.SendAsync<JsonRpcRequest>(_encoder, (JsonRpcRequest)request);
    }
}
