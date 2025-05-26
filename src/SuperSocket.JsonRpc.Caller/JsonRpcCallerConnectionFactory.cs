using System;
using System.Net;
using NRPC.Abstractions;
using NRPC.Caller;
using SuperSocket.Connection;

namespace SuperSocket.JsonRpc.Caller
{
    public class JsonRpcCallerConnectionFactory : IRpcConnectionFactory
    {
        public EndPoint EndPoint { get; }
        
        public JsonRpcCallerConnectionFactory(EndPoint endPoint)
        {
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint), "EndPoint cannot be null.");

            EndPoint = endPoint;
        }

        public async Task<IRpcConnection> CreateConnection()
        {
            var connection = new JsonRpcCallerConnection();
            await connection.AsClient().ConnectAsync(EndPoint);
            return connection;
        }
    }
}