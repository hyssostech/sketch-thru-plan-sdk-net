using Newtonsoft.Json;

namespace StpSDK;

public class StpCoa : StpObject
{
    [JsonProperty("fsTYPE")]
    public override string Type { get => "coa"; set { } }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("affiliation")]
    public string Affiliation { get; set; }

    [JsonProperty("creatorRole")]
    public string CreatorRole { get; set; }

    [JsonProperty("state")]
    public TaskOrgState State { get; set; }

    [JsonIgnore]
    public override string Description => Name;
}

public class TaskOrgState
{
    [JsonProperty("fsTYPE")]
    public string Type { get; set; } = "task_org_state";

    [JsonProperty("date")]
    public string Date { get; set; }

    [JsonProperty("userRole")]
    public string UserRole { get; set; }
}
