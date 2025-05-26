using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SuperSocket.Connection;

namespace SuperSocket.JsonRpc.Tests;

public class MainTests
{
    [Fact]
    public async Task TestRpcRequestPipelineFilter()
    {
        var connection = new TestConnection(new ConnectionOptions());
        var packageStream = connection.RunAsync<JsonRpcRequest>(new JsonRpcRequestPipelineFilter()
        {
            Decoder = new JsonRpcRequestDecoder()
        });

        var packageReader = packageStream.GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // Test single request
        await connection.WritePipeDataAsync(Encoding.UTF8.GetBytes("{ \"jsonrpc\": \"2.0\", \"method\": \"subtract\", \"params\": [42, 23], \"id\": 1 }"), TestContext.Current.CancellationToken);

        Assert.True(await packageReader.MoveNextAsync());

        var package = packageReader.Current;

        Assert.NotNull(package);
        Assert.Equal("2.0", package.Version);
        Assert.Equal("subtract", package.Method);
        Assert.Equal(2, package.Parameters.Length);
        Assert.Equal(42, ((JsonElement)package.Parameters[0]).TryGetInt32(out var value1) ? value1 : 0);
        Assert.Equal(23, ((JsonElement)package.Parameters[1]).TryGetInt32(out var value2) ? value2 : 0);
        Assert.Equal("1", package.Id);

        // Test batch requests
        await connection.WritePipeDataAsync(Encoding.UTF8.GetBytes("[{ \"jsonrpc\": \"2.0\", \"method\": \"subtract1\", \"params\": [42, 23], \"id\": 1 }, { \"jsonrpc\": \"2.0\", \"method\": \"subtract2\", \"params\": [52, 33], \"id\": 2 }]"), TestContext.Current.CancellationToken);

        Assert.True(await packageReader.MoveNextAsync());

        var firstPackage = packageReader.Current;

        Assert.NotNull(firstPackage);
        Assert.Equal("2.0", firstPackage.Version);
        Assert.Equal("subtract1", firstPackage.Method);
        Assert.Equal(2, firstPackage.Parameters.Length);
        Assert.Equal(42, ((JsonElement)firstPackage.Parameters[0]).TryGetInt32(out var value21) ? value21 : 0);
        Assert.Equal(23, ((JsonElement)firstPackage.Parameters[1]).TryGetInt32(out var value22) ? value22 : 0);
        Assert.Equal("1", package.Id);

        var secondPackage = firstPackage.Next;

        Assert.NotNull(secondPackage);
        Assert.Equal("2.0", secondPackage.Version);
        Assert.Equal("subtract2", secondPackage.Method);
        Assert.Equal(2, secondPackage.Parameters.Length);
        Assert.Equal(52, ((JsonElement)secondPackage.Parameters[0]).TryGetInt32(out var value31) ? value31 : 0);
        Assert.Equal(33, ((JsonElement)secondPackage.Parameters[1]).TryGetInt32(out var value32) ? value32 : 0);
        Assert.Equal("2", secondPackage.Id);
    }

    [Fact]
    public async Task TestRpcResponsePipelineFilter()
    {
        var connection = new TestConnection(new ConnectionOptions());
        var packageStream = connection.RunAsync<JsonRpcResponse>(new JsonRpcResponsePipelineFilter()
        {
            Decoder = new JsonRpcResponseDecoder()
        });

        var packageReader = packageStream.GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // Test single response with result
        await connection.WritePipeDataAsync(Encoding.UTF8.GetBytes("{ \"jsonrpc\": \"2.0\", \"result\": 19, \"id\": 1 }"), TestContext.Current.CancellationToken);

        Assert.True(await packageReader.MoveNextAsync());

        var package = packageReader.Current;

        Assert.NotNull(package);
        Assert.Equal("2.0", package.Version);
        Assert.Equal("1", package.Id);
        Assert.NotNull(package.Result);
        Assert.Equal(19, ((JsonElement)package.Result).TryGetInt32(out var resultValue) ? resultValue : 0);
        Assert.Null(package.Error);

        // Test single response with error
        await connection.WritePipeDataAsync(Encoding.UTF8.GetBytes("{ \"jsonrpc\": \"2.0\", \"error\": { \"code\": -32601, \"message\": \"Method not found\" }, \"id\": 2 }"), TestContext.Current.CancellationToken);

        Assert.True(await packageReader.MoveNextAsync());

        var errorPackage = packageReader.Current;

        Assert.NotNull(errorPackage);
        Assert.Equal("2.0", errorPackage.Version);
        Assert.Equal("2", errorPackage.Id);
        Assert.Null(errorPackage.Result);
        Assert.NotNull(errorPackage.Error);
        Assert.Equal(-32601, errorPackage.Error.Code);
        Assert.Equal("Method not found", errorPackage.Error.Message);

        // Test batch responses
        await connection.WritePipeDataAsync(Encoding.UTF8.GetBytes("[{ \"jsonrpc\": \"2.0\", \"result\": 19, \"id\": 1 }, { \"jsonrpc\": \"2.0\", \"error\": { \"code\": -32601, \"message\": \"Method not found\" }, \"id\": 2 }]"), TestContext.Current.CancellationToken);

        Assert.True(await packageReader.MoveNextAsync());

        var firstResponse = packageReader.Current;

        Assert.NotNull(firstResponse);
        Assert.Equal("2.0", firstResponse.Version);
        Assert.Equal("1", firstResponse.Id);
        Assert.NotNull(firstResponse.Result);
        Assert.Equal(19, ((JsonElement)firstResponse.Result).TryGetInt32(out var batchResultValue) ? batchResultValue : 0);
        Assert.Null(firstResponse.Error);

        var secondResponse = firstResponse.Next;

        Assert.NotNull(secondResponse);
        Assert.Equal("2.0", secondResponse.Version);
        Assert.Equal("2", secondResponse.Id);
        Assert.Null(secondResponse.Result);
        Assert.NotNull(secondResponse.Error);
        Assert.Equal(-32601, secondResponse.Error.Code);
        Assert.Equal("Method not found", secondResponse.Error.Message);
    }
}
