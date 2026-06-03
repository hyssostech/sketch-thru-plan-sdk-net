using Newtonsoft.Json;
using PropertyChanged;
using System.Collections.Generic;
using System.ComponentModel;

namespace StpSDK;

public class StpItem : StpObject, INotifyPropertyChanged
{
#pragma warning disable CS0067
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    [JsonProperty("sidc")]
    public virtual string SymbolId { get; set; }

    [JsonProperty("creatorRole")]
    public string CreatorRole { get; set; }

    [JsonProperty("confidence")]
    public double Confidence { get; set; }

    [JsonProperty("alt")]
    public int Order { get; set; }

    [JsonProperty("parentCoa")]
    public string ParentCoa { get; set; }

    [JsonProperty("interval")]
    public object Interval { get; set; }

    public virtual string FullDescription { get; }

    public List<StpItem> Alternates { get; set; }

    public StpItem() : base() { }
    public StpItem(string fsType) : base(fsType) { }
}
