using System.Text.Json.Nodes;

namespace SuperSocket.JsonRPC
{
    public class JsonRPCPackageInfo
    {
        public string Version { get; set; }

        public string Id { get; set; }

        public string Method { get; set; }
        
        public JsonNode Parameters { get; set; }
    }
}