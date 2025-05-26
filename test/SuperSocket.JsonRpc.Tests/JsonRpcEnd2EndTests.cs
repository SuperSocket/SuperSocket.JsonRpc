using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SuperSocket;
using SuperSocket.Client;
using SuperSocket.Connection;
using SuperSocket.JsonRpc;
using SuperSocket.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Session;
using SuperSocket.Server.Host;

namespace SuperSocket.JsonRpc.Tests;

public class JsonRpcEnd2EndTests : IDisposable
{
    private IHost? _serverHost;
    private readonly int _testPort = 4040;

    public void Dispose()
    {
        _serverHost?.StopAsync().Wait();
        _serverHost?.Dispose();
    }

    //[Fact]
    public async Task TestJsonRpcRequestResponse_SingleRequest()
    {
        // Arrange
        await StartServerAsync();
        
        var client = new EasyClient<JsonRpcResponse>(new JsonRpcResponsePipelineFilter()
        {
            Decoder = new JsonRpcResponseDecoder()
        }).AsClient();

        await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, _testPort));

        // Act - Send a JSON-RPC request
        var requestJson = """{"jsonrpc": "2.0", "method": "add", "params": [42, 23], "id": 1}""";
        await client.SendAsync(Encoding.UTF8.GetBytes(requestJson));

        // Assert - Receive the response
        var response = await client.ReceiveAsync();
        
        Assert.NotNull(response);
        Assert.Equal("2.0", response.Version);
        Assert.Equal("1", response.Id);
        Assert.NotNull(response.Result);
        Assert.Null(response.Error);
        
        // Verify the result value (65 = 42 + 23)
        var resultValue = ((JsonElement)response.Result).GetInt32();
        Assert.Equal(65, resultValue);
    }

    //[Fact]
    public async Task TestJsonRpcRequestResponse_BatchRequest()
    {
        // Arrange
        await StartServerAsync();
        
        var client = new EasyClient<JsonRpcResponse>(new JsonRpcResponsePipelineFilter()
        {
            Decoder = new JsonRpcResponseDecoder()
        }).AsClient();

        await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, _testPort));

        // Act - Send a batch JSON-RPC request
        var batchRequestJson = """[{"jsonrpc": "2.0", "method": "add", "params": [10, 5], "id": 1}, {"jsonrpc": "2.0", "method": "subtract", "params": [20, 8], "id": 2}]""";
        await client.SendAsync(Encoding.UTF8.GetBytes(batchRequestJson));

        // Assert - Receive the batch response
        var firstResponse = await client.ReceiveAsync();
        
        Assert.NotNull(firstResponse);
        Assert.Equal("2.0", firstResponse.Version);
        Assert.Equal("1", firstResponse.Id);
        Assert.NotNull(firstResponse.Result);
        Assert.Null(firstResponse.Error);
        
        // Verify first result (15 = 10 + 5)
        var firstResultValue = ((JsonElement)firstResponse.Result).GetInt32();
        Assert.Equal(15, firstResultValue);

        // Check second response in the batch
        var secondResponse = firstResponse.Next;
        Assert.NotNull(secondResponse);
        Assert.Equal("2.0", secondResponse.Version);
        Assert.Equal("2", secondResponse.Id);
        Assert.NotNull(secondResponse.Result);
        Assert.Null(secondResponse.Error);

        // Verify second result (12 = 20 - 8)
        var secondResultValue = ((JsonElement)secondResponse.Result).GetInt32();
        Assert.Equal(12, secondResultValue);
    }

    //[Fact]
    public async Task TestJsonRpcRequestResponse_ErrorResponse()
    {
        // Arrange
        await StartServerAsync();
        
        var client = new EasyClient<JsonRpcResponse>(new JsonRpcResponsePipelineFilter()
        {
            Decoder = new JsonRpcResponseDecoder()
        }).AsClient();

        await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, _testPort));

        // Act - Send a JSON-RPC request with unknown method
        var requestJson = """{"jsonrpc": "2.0", "method": "unknown_method", "params": [1, 2], "id": 1}""";
        await client.SendAsync(Encoding.UTF8.GetBytes(requestJson));

        // Assert - Receive the error response
        var response = await client.ReceiveAsync();
        
        Assert.NotNull(response);
        Assert.Equal("2.0", response.Version);
        Assert.Equal("1", response.Id);
        Assert.Null(response.Result);
        Assert.NotNull(response.Error);
        
        // Verify error details
        Assert.Equal(-32601, response.Error.Code); // Method not found
        Assert.Equal("Method not found", response.Error.Message);
    }

    //[Fact]
    public async Task TestJsonRpcRequestResponse_Notification()
    {
        // Arrange
        await StartServerAsync();
        
        var client = new EasyClient<JsonRpcResponse>(new JsonRpcResponsePipelineFilter()
        {
            Decoder = new JsonRpcResponseDecoder()
        }).AsClient();

        await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, _testPort));

        // Act - Send a JSON-RPC notification (no id)
        var notificationJson = """{"jsonrpc": "2.0", "method": "notify", "params": ["hello"]}""";
        await client.SendAsync(Encoding.UTF8.GetBytes(notificationJson));

        // Assert - Notifications should not receive responses
        // Wait a short time to ensure no response comes
        await Task.Delay(100);
        
        // Try to receive with a short timeout - should not receive anything
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        
        try
        {
            var response = await client.ReceiveAsync();
            // If we reach here, we unexpectedly received a response
            Assert.True(false, "Should not receive response for notification");
        }
        catch (OperationCanceledException)
        {
            // Expected - no response for notifications
            Assert.True(true);
        }
    }

    private async Task StartServerAsync()
    {
        if (_serverHost != null)
            return;

        var hostBuilder = SuperSocketHostBuilder.Create<JsonRpcRequest, JsonRpcRequestPipelineFilter>()
            .UseHostedService<JsonRpcServerService>()
            .UsePipelineFilter<JsonRpcRequestPipelineFilter>()
            .UsePackageDecoder<JsonRpcRequestDecoder>()
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

        _serverHost = hostBuilder.Build();
        await _serverHost.StartAsync();
        
        // Wait a bit for server to fully start
        await Task.Delay(100);
    }
}

// Mock JSON-RPC server service for testing
public class JsonRpcServerService : SuperSocketService<JsonRpcRequest>
{
    public JsonRpcServerService(IServiceProvider serviceProvider, IOptions<ServerOptions> serverOptions)
        : base(serviceProvider, serverOptions)
    {
    }

    protected override async ValueTask OnSessionConnectedAsync(IAppSession session)
    {
        Logger.LogInformation($"Session {session.SessionID} connected");
        await base.OnSessionConnectedAsync(session);
    }

    protected override async ValueTask OnSessionClosedAsync(IAppSession session, CloseEventArgs e)
    {
        Logger.LogInformation($"Session {session.SessionID} closed: {e.Reason}");
        await base.OnSessionClosedAsync(session, e);
    }
}
