using Newtonsoft.Json;

namespace StpSDK;

public class Auth
{
    [JsonProperty("fsTYPE")]
    public string Type { get; set; } = "auth";

    [JsonProperty("identity")]
    public string Identity { get; set; }

    [JsonProperty("info", NullValueHandling = NullValueHandling.Ignore)]
    public AuthInfo Info { get; set; }

    public Auth() { }

    public Auth(string identity)
    {
        Identity = identity;
    }
}

public class AuthInfo
{
    [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
    public string Source { get; set; }

    [JsonProperty("is_task_org_search", NullValueHandling = NullValueHandling.Ignore)]
    public bool? IsTaskOrgSearch { get; set; }
}
