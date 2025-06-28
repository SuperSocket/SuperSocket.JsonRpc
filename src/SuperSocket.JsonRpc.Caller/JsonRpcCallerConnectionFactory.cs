using System;
using System.Net;
using NRPC.Abstractions;
using NRPC.Caller.Connection;

namespace SuperSocket.JsonRpc.Caller
{
    class JsonRpcCallerConnectionFactory : IRpcConnectionFactory
    {
        public EndPoint EndPoint { get; }
        
        public JsonRpcCallerConnectionFactory(EndPoint endPoint)
        {
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint), "EndPoint cannot be null.");

            EndPoint = endPoint;
        }

        public async Task<IRpcConnection> CreateConnectionAsync(CancellationToken cancellationToken)
        {
            var connection = new JsonRpcCallerConnection();
            await connection.AsClient().ConnectAsync(EndPoint, cancellationToken);
            return connection;
        }
    }
}