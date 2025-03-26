namespace SuperSocket.JsonRpc;

using System.Text.Json;

public class JsonRpcPackageInfo
{
    public string Version { get; set; }

    public string Id { get; set; }
    
    public string Method { get; set; }

    public JsonElement Parameters { get; set; }

    public JsonRpcPackageInfo Next { get; set; }
}