using NRPC.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperSocket.JsonRpc
{
    public class JsonRpcCallerAdapter : IRpcCallingAdapter
    {
        public RpcRequest CreateRequest()
        {
            return new JsonRpcRequest
            {
                Version = "2.0"
            };
        }

        public RpcResponse CreateResponse()
        {
            return new JsonRpcResponse
            {
                Version = "2.0"
            };
        }
    }
}