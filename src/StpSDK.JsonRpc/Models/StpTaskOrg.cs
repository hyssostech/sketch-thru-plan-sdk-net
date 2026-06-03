using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace StpSDK;

public class StpTaskOrg : StpObject
{
    [JsonProperty("fsTYPE")]
    public override string Type { get => "task_org"; set { } }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("affiliation")]
    public string Affiliation { get; set; }

    [JsonProperty("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonIgnore]
    public override string Description => Name;
}

public class StpTaskOrgUnit : StpSymbol, INotifyPropertyChanged
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("speechPhrases")]
    public List<string> SpeechPhrases { get; set; }

    [JsonProperty("unitType")]
    public string UnitType { get; set; }

    [JsonProperty("parentTO")]
    public string ParentTO { get; set; }

    [JsonIgnore]
    public override string SymbolDesignation => ShortDescription;

    public StpTaskOrgUnit() : base("task_org_unit") { }
}

public class StpTaskOrgRelationship : StpObject, INotifyPropertyChanged
{
#pragma warning disable CS0067
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    [JsonProperty("fsTYPE")]
    public override string Type { get => "task_org_relationship"; set { } }

    [JsonProperty("parent")]
    public string Parent { get; set; }

    [JsonProperty("child")]
    public string Child { get; set; }

    [JsonProperty("relationship")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public CommandRelationship Relationship { get; set; }

    [JsonProperty("isMTOE")]
    public bool IsMTOE { get; set; }

    [JsonProperty("parentTO")]
    public string ParentTO { get; set; }

    [JsonIgnore]
    public override string Description => "TO Relationship";
}
