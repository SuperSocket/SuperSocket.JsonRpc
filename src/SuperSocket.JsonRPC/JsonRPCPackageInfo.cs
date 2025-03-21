namespace SuperSocket.JsonRPC;

using System.Text.Json;

public class JsonRPCPackageInfo
{
    public string Version { get; set; }

    public string Id { get; set; }
    
    public string Method { get; set; }

    public JsonElement Parameters { get; set; }

    public JsonRPCPackageInfo Next { get; set; }
}