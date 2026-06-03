using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace StpSDK;

public class StpObject : IStpObject
{
    [JsonProperty("fsTYPE")]
    public virtual string Type { get; set; }

    [JsonProperty("poid")]
    public virtual string Poid { get; set; }

    [JsonProperty("dbTimestamp")]
    public virtual string UpdateTimestamp { get; set; }

    [JsonProperty("dbCreationTimestamp")]
    public virtual string CreationTimestamp { get; set; }

    [JsonProperty("dbVersion")]
    public virtual string DbVersion { get; set; }

    [JsonProperty("extensions", NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, object> Extensions { get; set; }

    public virtual string Description { get; set; } = string.Empty;

    public StpObject() { }

    internal StpObject(string fsType)
    {
        Type = fsType;
    }

    public StpObject AsTypedObject()
    {
        return Type switch
        {
            "unit" or "tg" or "mootw" => JsonConvert.DeserializeObject<StpSymbol>(JsonConvert.SerializeObject(this)),
            "task" => JsonConvert.DeserializeObject<StpTask>(JsonConvert.SerializeObject(this)),
            "task_org" => JsonConvert.DeserializeObject<StpTaskOrg>(JsonConvert.SerializeObject(this)),
            "task_org_unit" => JsonConvert.DeserializeObject<StpTaskOrgUnit>(JsonConvert.SerializeObject(this)),
            "task_org_relationship" => JsonConvert.DeserializeObject<StpTaskOrgRelationship>(JsonConvert.SerializeObject(this)),
            "coa" => JsonConvert.DeserializeObject<StpCoa>(JsonConvert.SerializeObject(this)),
            _ => this,
        };
    }
}
