using Microsoft.Extensions.DependencyInjection;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
using NRPC.Executor;
using NRPC.SuperSocket.Server;
using SuperSocket.ProtoBase;
using SuperSocket.Server.Abstractions.Host;

namespace SuperSocket.JsonRpc.Server;

public static class Extensions
{
    public static ISuperSocketHostBuilder<JsonRpcRequest> UseJsonRpcServer<TServiceContract, TService>(this ISuperSocketHostBuilder<JsonRpcRequest> hostBuilder)
        where TServiceContract : class
        where TService : class, TServiceContract
    {
        return hostBuilder
            .UseNRPCService<JsonRpcRequest, JsonRpcResponse, TServiceContract, TService>()
            .UsePipelineFilter<JsonRpcRequestPipelineFilter>()
            .UsePackageDecoder<JsonRpcRequestDecoder>()
            .ConfigureServices((ctx, services) =>
            {
                services.AddSingleton<IPackageEncoder<JsonRpcResponse>, JsonRpcResponseEncoder>();
                services.AddSingleton<IExpressionConverter, JsonElementExpressionConverter>();
                services.AddSingleton<IRpcCallingAdapter, JsonRpcCallerAdapter>();
                services.AddSingleton<CompiledServiceHandler<TServiceContract>>(provider => new CompiledServiceHandler<TServiceContract>(ServiceMetadata.Create<TServiceContract>(provider.GetRequiredService<IExpressionConverter>()), new JsonRpcCallerAdapter()));
            });
    }
}
