using Newtonsoft.Json;

namespace StpSDK;

public class PlanningScenario
{
    [JsonProperty("fsTYPE")]
    public string Type { get; set; } = "planning_scenario";

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("isValid")]
    public bool IsValid { get; set; }

    [JsonProperty("isLoaded")]
    public bool IsLoaded { get; set; }

    [JsonProperty("creatorRole")]
    public string CreatorRole { get; set; }

    [JsonProperty("taskOrgState")]
    public TaskOrgState TaskOrgState { get; set; }

    [JsonProperty("savePath")]
    public string SavePath { get; set; }

    [JsonProperty("networkPath")]
    public string NetworkPath { get; set; }
}
