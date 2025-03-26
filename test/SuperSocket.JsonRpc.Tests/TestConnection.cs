using System.Buffers;
using SuperSocket.Connection;

namespace SuperSocket.JsonRpc.Tests;

public class TestConnection : VirtualConnection
{
    public TestConnection(ConnectionOptions options)
        : base(options)
    {
    }

    protected override void Close()
    {
    }

    protected override ValueTask<int> FillPipeWithDataAsync(Memory<byte> memory, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    protected override ValueTask<int> SendOverIOAsync(ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}