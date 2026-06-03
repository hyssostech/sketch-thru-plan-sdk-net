using Newtonsoft.Json;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace StpSDK;

public class StpTask : StpItem, INotifyPropertyChanged
{
    private string _fsType = "task";

    [JsonProperty("fsTYPE")]
    public override string Type
    {
        get => _fsType;
        set => _fsType = value;
    }

    [JsonProperty("description")]
    internal string TaskDescription { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("how")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public TaskHow How { get; set; }

    [JsonProperty("what")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public TaskWhat What { get; set; }

    [JsonProperty("why")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public TaskWhy Why { get; set; }

    [JsonProperty("roe")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public ROE Roe { get; set; }

    [JsonProperty("prob")]
    public double Prob { get; set; }

    [JsonProperty("movementFeatures")]
    public MovementFeatures MovementFeatures { get; set; }

    [JsonProperty("taskStatus")]
    public string TaskStatus { get; set; }

    [JsonProperty("uiStatus")]
    public string UiStatus { get; set; }

    [JsonProperty("startTime")]
    public int StartTime { get; set; }

    [JsonProperty("endTime")]
    public int EndTime { get; set; }

    [JsonProperty("speech")]
    public string Speech { get; set; }

    [JsonProperty("language")]
    public string Language { get; set; }

    [JsonProperty("who")]
    public string Who { get; set; }

    [JsonProperty("supported")]
    public string Supported { get; set; }

    [JsonProperty("tgs")]
    public List<string> Tgs { get; set; }

    #region Computed properties
    [JsonIgnore]
    public override string FullDescription => Description;

    [JsonIgnore]
    public bool IsConfirmed => UiStatus == "confirmed";

    [JsonIgnore]
    public override string Description
    {
        get
        {
            string description = TaskDescription?.Replace("_", " ") ?? string.Empty;
            int unkOrderIdx = description.IndexOf("in order to unknown", StringComparison.InvariantCultureIgnoreCase);
            if (unkOrderIdx >= 0)
                description = description.Remove(unkOrderIdx, "in order to unknown".Length).Trim();
            return description;
        }
        set => TaskDescription = value;
    }
    #endregion

    public StpTask() : base("task") { }
}

public class MovementFeatures
{
    [JsonProperty("fsTYPE")]
    public string Type { get; set; } = "movement_features";

    [JsonProperty("movement")]
    public bool Movement { get; set; }

    [JsonProperty("movesTo")]
    public string MovesTo { get; set; }
}
