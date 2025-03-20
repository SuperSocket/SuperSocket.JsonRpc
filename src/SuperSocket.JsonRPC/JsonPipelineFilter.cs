namespace SuperSocket.JsonRPC;

using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using SuperSocket.ProtoBase;

public class JsonPipelineFilter : IPipelineFilter<JsonRPCPackageInfo>
{
    private int nodeStartCount = 0;

    private static readonly char nodeStart = '{';

    private static readonly char nodeEnd = '{';

    private long? consumed;

    private static byte[] delimiters = Encoding.UTF8.GetBytes(new[] { nodeStart, nodeEnd });    

    public IPackageDecoder<JsonRPCPackageInfo> Decoder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public IPipelineFilter<JsonRPCPackageInfo> NextFilter { get; } = null;

    public object Context { get; set; }

    public JsonRPCPackageInfo Filter(ref SequenceReader<byte> reader)
    {
        var sequence = reader.Sequence;

        var detectReader = consumed != null
            ? new SequenceReader<byte>(sequence.Slice(consumed.Value))
            : reader;

        if (!detectReader.TryAdvanceToAny(delimiters, advancePastDelimiter: false))
        {
            return default;
        }

        reader.TryRead(out var nodeFound);

        if (nodeFound == delimiters[0])
        {
            nodeStartCount++;
        }
        else
        {
            nodeStartCount--;
        }

        if (nodeStartCount != 0)
        {
            consumed = detectReader.Consumed;

            if (reader.Consumed == detectReader.Consumed)
            {
                reader.Advance((int)detectReader.Consumed);
                return default;
            }
        }

        var jsonReader = new Utf8JsonReader(sequence.Slice(0, reader.Consumed));

        var jsonElement = JsonElement.ParseValue(ref jsonReader);

        return new JsonRPCPackageInfo
        {
            Version = jsonElement.GetProperty("jsonrpc").GetString(),
            Id = jsonElement.GetProperty("id").GetString(),
            Method = jsonElement.GetProperty("method").GetString(),
            //Parameters = JsonNode.Parse(jsonElement.GetProperty("params"));
        };
    }

    public void Reset()
    {
        nodeStartCount = 0;
        consumed = null;
    }
}
