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

    // The wire 'sidc' is a structured object, bound to StpSymbol.Sidc. SymbolId is a
    // convenience string (the legacy/2525C id) that StpSymbol computes from its Sidc;
    // it is not JSON-mapped here. Non-symbol items (e.g. tasks) carry no sidc.
    [JsonIgnore]
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
