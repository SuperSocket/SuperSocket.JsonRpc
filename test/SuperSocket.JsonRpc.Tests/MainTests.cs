using System.Text;
using System.Threading.Tasks;
using SuperSocket.Connection;

namespace SuperSocket.JsonRpc.Tests;

public class MainTests
{
    [Fact]
    public async Task TestNormalPipelineFilter()
    {
        var connection = new TestConnection(new ConnectionOptions());
        var packageStream = connection.RunAsync<JsonRpcPackageInfo>(new JsonPipelineFilter
        {
            Decoder = new JsonRPCPackageDecoder()
        });

        var packageReader = packageStream.GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // Test single request
        await connection.WritePipeDataAsync(Encoding.UTF8.GetBytes("{ \"jsonrpc\": \"2.0\", \"method\": \"subtract\", \"params\": [42, 23], \"id\": 1 }"), TestContext.Current.CancellationToken);

        Assert.True(await packageReader.MoveNextAsync());

        var package = packageReader.Current;

        Assert.NotNull(package);
        Assert.Equal("2.0", package.Version);
        Assert.Equal("subtract", package.Method);
        Assert.Equal(2, package.Parameters.GetArrayLength());
        Assert.Equal(42, package.Parameters[0].TryGetInt32(out var value1) ? value1 : 0);
        Assert.Equal(23, package.Parameters[1].TryGetInt32(out var value2) ? value2 : 0);
        Assert.Equal("1", package.Id);

        // Test batch requests
        await connection.WritePipeDataAsync(Encoding.UTF8.GetBytes("[{ \"jsonrpc\": \"2.0\", \"method\": \"subtract1\", \"params\": [42, 23], \"id\": 1 }, { \"jsonrpc\": \"2.0\", \"method\": \"subtract2\", \"params\": [52, 33], \"id\": 2 }]"), TestContext.Current.CancellationToken);

        Assert.True(await packageReader.MoveNextAsync());

        var firstPackage = packageReader.Current;

        Assert.NotNull(firstPackage);
        Assert.Equal("2.0", firstPackage.Version);
        Assert.Equal("subtract1", firstPackage.Method);
        Assert.Equal(2, firstPackage.Parameters.GetArrayLength());
        Assert.Equal(42, firstPackage.Parameters[0].TryGetInt32(out var value21) ? value21 : 0);
        Assert.Equal(23, firstPackage.Parameters[1].TryGetInt32(out var value22) ? value22 : 0);
        Assert.Equal("1", package.Id);

        var secondPackage = firstPackage.Next;

        Assert.NotNull(secondPackage);
        Assert.Equal("2.0", secondPackage.Version);
        Assert.Equal("subtract2", secondPackage.Method);
        Assert.Equal(2, secondPackage.Parameters.GetArrayLength());
        Assert.Equal(52, secondPackage.Parameters[0].TryGetInt32(out var value31) ? value31 : 0);
        Assert.Equal(33, secondPackage.Parameters[1].TryGetInt32(out var value32) ? value32 : 0);
        Assert.Equal("2", secondPackage.Id);
    }
}
