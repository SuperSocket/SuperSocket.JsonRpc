namespace SuperSocket.JsonRpc;

using System.Buffers;
using System.Text;
using System.Text.Json;
using SuperSocket.ProtoBase;

public abstract class JsonPipelineFilter<TJsonRpcPackage> : IPipelineFilter<TJsonRpcPackage>
{
    private static readonly char nodeStart = '{';

    private static readonly char nodeEnd = '}';

    private static byte[] delimiters = Encoding.UTF8.GetBytes(new[] { nodeStart, nodeEnd });

    private static readonly char arrayStart = '[';

    private static readonly char arrayEnd = ']';

    private static byte[] arrayDelimiters = Encoding.UTF8.GetBytes(new[] { arrayStart, arrayEnd });

    private int nodeStartCount = 0;

    private byte[] currentDelimiters;

    private long? consumed;

    public IPackageDecoder<TJsonRpcPackage> Decoder { get; set; }

    public IPipelineFilter<TJsonRpcPackage> NextFilter { get; } = null;

    public object Context { get; set; }

    public TJsonRpcPackage Filter(ref SequenceReader<byte> reader)
    {
        var sequence = reader.Sequence;

        var attempReader = consumed != null
            ? new SequenceReader<byte>(sequence.Slice(consumed.Value))
            : reader;

        if (consumed == null)
        {
            attempReader.TryRead(out var nodeFound);

            if (nodeFound == nodeStart)
            {
                currentDelimiters = delimiters;
                nodeStartCount++;
            }
            else if (nodeFound == arrayStart)
            {
                currentDelimiters = arrayDelimiters;
                nodeStartCount++;
            }
            else
            {
                throw new ProtocolException("Invalid JSON-RPC package.");
            }
        }

        while (attempReader.TryAdvanceToAny(currentDelimiters, advancePastDelimiter: true))
        {
            attempReader.Rewind(1);
            attempReader.TryRead(out var nodeFound);

            if (nodeFound == currentDelimiters[0])
            {
                nodeStartCount++;
            }
            else
            {
                nodeStartCount--;
            }

            if (nodeStartCount < 0)
            {
                throw new ProtocolException("Invalid JSON-RPC package.");
            }

            if (nodeStartCount == 0)
            {
                break;
            }
        }

        var totalConsumed = attempReader.Consumed + (consumed ?? 0);

        if (nodeStartCount != 0)
        {
            consumed = totalConsumed;
            return default;
        }

        var sequenceToDecode = sequence.Slice(0, totalConsumed);

        try
        {
            return Decoder.Decode(ref sequenceToDecode, Context);
        }
        finally
        {
            reader.Advance(totalConsumed);
        }
    }

    public void Reset()
    {
        nodeStartCount = 0;
        currentDelimiters = null;
        consumed = null;
    }
}
