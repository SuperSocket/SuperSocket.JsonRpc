using SuperSocket.Server.Abstractions.Host;
using SuperSocket.Server;
using SuperSocket.Server.Host;

namespace SuperSocket.JsonRpc.Server
{
    public static class JsonRpcHostBuilder
    {
        public static ISuperSocketHostBuilder<JsonRpcRequest> Create<TServiceContract, TService>()
            where TService : class, TServiceContract
            where TServiceContract : class
        {
            return SuperSocketHostBuilder
                .Create<JsonRpcRequest>()
                .UseJsonRpcServer<TServiceContract, TService>();
        }
    }
}