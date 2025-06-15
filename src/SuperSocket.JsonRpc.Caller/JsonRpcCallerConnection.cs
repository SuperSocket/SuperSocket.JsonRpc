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
class JsonRpcCallerConnection : EasyClient<JsonRpcResponse>, IRpcConnection
{
    private readonly JsonRpcRequestEncoder _encoder = new JsonRpcRequestEncoder();

    public JsonRpcCallerConnection()
        : base(new JsonRpcResponsePipelineFilter()
        {
            Decoder = new JsonRpcResponseDecoder()
        })
    {
    }

    public bool IsConnected => this.Connection != null && !this.Connection.IsClosed;

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public ValueTask DisposeAsync()
    {
        return CloseAsync();
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
