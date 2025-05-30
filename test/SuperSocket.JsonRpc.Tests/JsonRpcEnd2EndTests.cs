using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NRPC.Abstractions.Metadata;
using NRPC.Caller;
using NRPC.Executor;
using SuperSocket;
using SuperSocket.Client;
using SuperSocket.Connection;
using SuperSocket.JsonRpc;
using SuperSocket.JsonRpc.Caller;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.Server.Host;

namespace SuperSocket.JsonRpc.Tests;

public class JsonRpcEnd2EndTests
{
    private readonly int _testPort = 4040;

    [Fact]
    public async Task TestJsonRpcRequestResponse_SingleRequest()
    {
        // Arrange
        using var host = SetupServer();

        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));

        var caller = await callerFactory.CreateCaller();

        Assert.NotNull(caller);

        // Act
        var result = await caller.Add(42, 23).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(65, result);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_ErrorResponse()
    {
        // Arrange
        using var host = SetupServer();

        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));

        var caller = await callerFactory.CreateCaller();

        var exception = await Assert.ThrowsAsync<RpcServerException>(() => caller.Fail("This is a test failure").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.NotNull(exception);
        Assert.NotNull(exception.ServerError);
        Assert.Equal(500, exception.ServerError.Code);
        Assert.Equal("This is a test failure", exception.ServerError.Message);
    }

    private IHost SetupServer()
    {
        var responseEncoder = new JsonRpcResponseEncoder();
        var serviceHandler = new CompiledServiceHandler<ITestService>(
            ServiceMetadata.Create<ITestService>(new JsonElementExpressionConverter()),
            new JsonRpcCallerAdapter());

        var hostBuilder = SuperSocketHostBuilder.Create<JsonRpcRequest, JsonRpcRequestPipelineFilter>()
            .UseHostedService<JsonRpcServerService>()
            .UsePipelineFilter<JsonRpcRequestPipelineFilter>()
            .UsePackageDecoder<JsonRpcRequestDecoder>()
            .UsePackageHandler<JsonRpcRequest>(async (session, request) =>
            {
                var response = await serviceHandler.HandleRequestAsync(session.Server as ITestService, request);
                await session.SendAsync(responseEncoder, response as JsonRpcResponse);
            })
            .ConfigureSuperSocket(options =>
            {
                options.Name = "JsonRpcServer";
                options.Listeners = new List<ListenOptions>
                    {
                        new ListenOptions
                        {
                            Ip = "127.0.0.1",
                            Port = _testPort
                        }
                    };
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

        return hostBuilder.Build();
    }
}

[ServiceContractAtribute]
public interface ITestService
{
    Task<int> Add(int a, int b);

    Task<int> Subtract(int a, int b);

    Task<int> Multiply(int a, int b);

    Task<string> Concatenate(string a, string b);

    Task Notify(string message);

    Task Trigger();

    Task<int> GetTriggerTimes();

    Task Fail(string message)
    {
        throw new InvalidOperationException(message);
    }
}

// Mock JSON-RPC server service for testing
public class JsonRpcServerService : SuperSocketService<JsonRpcRequest>, ITestService
{
    private int _triggerTimes;

    public string LastNotifyMessage { get; private set; }

    public JsonRpcServerService(IServiceProvider serviceProvider, IOptions<ServerOptions> serverOptions)
        : base(serviceProvider, serverOptions)
    {
    }

    public Task<int> Add(int a, int b)
    {
        return Task.FromResult(a + b);
    }

    public Task<string> Concatenate(string a, string b)
    {
        return Task.FromResult(a + b);
    }

    public Task<int> GetTriggerTimes()
    {
        return Task.FromResult(_triggerTimes);
    }

    public Task<int> Multiply(int a, int b)
    {
        return Task.FromResult(a * b);
    }

    public Task Notify(string message)
    {
        LastNotifyMessage = message;
        return Task.CompletedTask;
    }

    public Task<int> Subtract(int a, int b)
    {
        return Task.FromResult(a - b);
    }

    public Task Trigger()
    {
        Interlocked.Increment(ref _triggerTimes);
        return Task.CompletedTask;
    }
}
