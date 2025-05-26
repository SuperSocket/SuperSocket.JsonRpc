using System;
using System.Reflection;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Client;

namespace SuperSocket.JsonRpc.Caller
{
    class JsonRpcCallerDispatchProxy : ClientDispatchProxy
    {
        protected override RpcRequest CreateRequest(MethodInfo targetMethod, object[] args)
        {
            return new JsonRpcRequest
            {
                Method = targetMethod.Name,
                Parameters = args,
                Version = "2.0",
                Next = null // This can be set to a next request if needed
            };
        }
    }
}