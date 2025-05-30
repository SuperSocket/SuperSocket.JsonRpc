using System;
using System.Net;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
using NRPC.Caller;

namespace SuperSocket.JsonRpc.Caller
{
    public class JsonRpcCallerFactory<TService> : RpcCallerFactory<TService>
        where TService : class
    {
        public JsonRpcCallerFactory(EndPoint endPoint)
            : base(new JsonRpcCallerConnectionFactory(endPoint), new JsonRpcCallerAdapter(), new JsonElementExpressionConverter())
        {
        }
    }
}