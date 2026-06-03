using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StpSDK;

internal class JsonRpcMessage
{
    [JsonProperty("method")]
    public string Method { get; set; }

    [JsonProperty("params")]
    public JToken Params { get; set; }
}

internal class JsonRpcRegisterParams
{
    [JsonProperty("serviceName")]
    public string ServiceName { get; set; }

    [JsonProperty("language")]
    public string Language { get; set; } = "csharp";

    [JsonProperty("solvables")]
    public List<string> Solvables { get; set; }

    [JsonProperty("machineId")]
    public string MachineId { get; set; }

    [JsonProperty("sessionId")]
    public string SessionId { get; set; }
}

internal class JsonRpcRequestWrapper
{
    [JsonProperty("method")]
    public string Method { get; set; } = "Request";

    [JsonProperty("params")]
    public JsonRpcRequestParams Params { get; set; }
}

internal class JsonRpcRequestParams
{
    [JsonProperty("jsonRequest")]
    public string JsonRequest { get; set; }

    [JsonProperty("cookie")]
    public int Cookie { get; set; }

    [JsonProperty("timeout")]
    public int Timeout { get; set; }
}

internal class JsonRpcResponseParams
{
    [JsonProperty("cookie")]
    public int Cookie { get; set; }

    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("result")]
    public JToken Result { get; set; }
}
