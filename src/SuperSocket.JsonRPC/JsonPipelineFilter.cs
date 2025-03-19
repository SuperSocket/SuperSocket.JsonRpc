namespace SuperSocket.JsonRPC;

using System.Buffers;
using SuperSocket.ProtoBase;

public class JsonPipelineFilter : IPipelineFilter<JsonRPCPackageInfo>
{
    public IPackageDecoder<JsonRPCPackageInfo> Decoder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public IPipelineFilter<JsonRPCPackageInfo> NextFilter => throw new NotImplementedException();

    public object Context { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public JsonRPCPackageInfo Filter(ref SequenceReader<byte> reader)
    {
        throw new NotImplementedException();
    }

    public void Reset()
    {
        throw new NotImplementedException();
    }
}
