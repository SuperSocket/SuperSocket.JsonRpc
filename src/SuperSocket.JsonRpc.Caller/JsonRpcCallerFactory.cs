using System;
using System.Net;
using NRPC.Abstractions;
using NRPC.Client;

namespace SuperSocket.JsonRpc.Caller
{
    public class JsonRpcCallerFactory : IClientFactory<IRpcConnection>
    {
        public EndPoint EndPoint { get; }
        
        public JsonRpcCallerFactory(EndPoint endPoint)
        {
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint), "EndPoint cannot be null.");

            EndPoint = endPoint;
        }

        public async Task<IRpcConnection> CreateClient()
        {
            var connection = new JsonRpcCaller();
            await connection.AsClient().ConnectAsync(EndPoint);
            return connection;
        }
    }
}