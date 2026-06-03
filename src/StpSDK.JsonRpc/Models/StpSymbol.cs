using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;

namespace StpSDK;

public class StpSymbol : StpItem, INotifyPropertyChanged
{
    private string _fsType;

    #region JSON-serialized properties
    [JsonProperty("fsTYPE")]
    public override string Type
    {
        get => _fsType;
        set => _fsType = value;
    }

    /// <summary>
    /// 2525D and 2525C identification codes for this symbol, as sent by the engine
    /// (the JSON-RPC <c>sidc</c> object).
    /// </summary>
    [JsonProperty("sidc")]
    public Sidc Sidc { get; set; }

    /// <summary>
    /// 2525C (legacy) identifier - a convenience accessor preserved for source compatibility
    /// with prior SDK versions. Not JSON-mapped; the wire <c>sidc</c> is <see cref="Sidc"/>.
    /// Setting it stores the value as the legacy code.
    /// </summary>
    [JsonIgnore]
    public override string SymbolId
    {
        get => Sidc?.Legacy ?? Sidc?.Delta;
        set => (Sidc ??= new Sidc()).Legacy = value;
    }

    /// <summary>2525D identifier: Part A + Part B (+ Part C), or the full delta code.</summary>
    [JsonIgnore]
    public string DeltaSIDC => Sidc?.Delta;

    /// <summary>2525C (legacy) identifier.</summary>
    [JsonIgnore]
    public string CharlieSIDC => Sidc?.Legacy;

    /// <summary>2525D symbol set (2-character code).</summary>
    [JsonIgnore]
    public string SymbolSet => Sidc?.SymbolSet;

    [JsonProperty("codingScheme")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public CodingScheme? CodingScheme { get; set; }

    [JsonProperty("affiliation")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public Affiliation? Affiliation { get; set; }

    [JsonProperty("battleDimension")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public BattleDimension? BattleDimension { get; set; }

    [JsonProperty("status")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public Status? Status { get; set; }

    [JsonProperty("modifier")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public Modifier? Modifier { get; set; }

    [JsonProperty("echelon")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public Echelon? Echelon { get; set; }

    [JsonProperty("mobility")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public Mobility? Mobility { get; set; }

    [JsonProperty("countryCode")]
    public string CountryCode { get; set; }

    [JsonProperty("orderBattle")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public OrderOfBattle? OrderOfBattle { get; set; }

    [JsonProperty("branch")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public Branch? Branch { get; set; }

    [JsonProperty("capability")]
    public string Capability { get; set; }

    [JsonProperty("groundRole")]
    public string GroundRole { get; set; }

    [JsonProperty("role")]
    public string Role { get; set; }

    [JsonProperty("defense")]
    public string Defense { get; set; }

    [JsonProperty("weight")]
    public string Weight { get; set; }

    [JsonProperty("designator1")]
    public string Designator1 { get; set; }

    [JsonProperty("designator2")]
    public string Designator2 { get; set; }

    [JsonProperty("strength")]
    [JsonConverter(typeof(NullSafeStringEnumConverter))]
    public Strength Strength { get; set; }

    [JsonProperty("location")]
    public Location Location { get; set; }

    [JsonProperty("info")]
    public string Info { get; set; }

    [JsonProperty("geometry")]
    public string Geometry { get; set; }

    [JsonProperty("timeFrom")]
    public string TimeFrom { get; set; }

    [JsonProperty("timeTo")]
    public string TimeTo { get; set; }

    [JsonProperty("altitude")]
    public string Altitude { get; set; }

    [JsonProperty("minAlt")]
    public string MinAlt { get; set; }

    [JsonProperty("maxAlt")]
    public string MaxAlt { get; set; }

    [JsonProperty("fullDescription")]
    public string CompleteLanguage { get; set; }

    [JsonProperty("unitParent")]
    public string UnitParent { get; set; }

    [JsonProperty("toUnitPoid")]
    public string TaskOrgUnitPoid { get; set; }

    [JsonProperty("uiStatus")]
    internal string UiStatus { get; set; }

    [JsonProperty("placed")]
    internal bool Placed { get; set; }

    [JsonProperty("disCode")]
    public DISCode DisCode { get; set; }

    [JsonProperty("federate")]
    public string Federate { get; set; }

    [JsonProperty("resources")]
    public List<Resource> Resources { get; set; }

    public CommandRelationship Relationship { get; set; }
    #endregion

    [JsonIgnore]
    internal string PoidSuperior { get; set; }

    #region Description properties
    [JsonIgnore]
    public string DesignatorDescription
    {
        get
        {
            string desc = string.Empty;
            if (!string.IsNullOrWhiteSpace(Designator1))
                desc += Designator1;
            if (!string.IsNullOrWhiteSpace(UnitParent))
                desc += $"/{UnitParent}";
            return desc;
        }
    }

    [JsonIgnore]
    public override string FullDescription
    {
        get
        {
            string fullDescription = CompleteLanguage;
            if (string.IsNullOrWhiteSpace(fullDescription))
                return string.Empty;
            if (fullDescription.StartsWith("present", StringComparison.InvariantCultureIgnoreCase))
                fullDescription = fullDescription.Substring("present".Length);
            if (!fullDescription.Contains(DesignatorDescription))
                fullDescription += " " + DesignatorDescription;
            return fullDescription.Trim();
        }
    }

    [JsonIgnore]
    public override string Description
    {
        get
        {
            string desc = FullDescription;
            if (string.IsNullOrWhiteSpace(desc))
                return string.Empty;
            if (desc.EndsWith($" {DesignatorDescription}", StringComparison.InvariantCultureIgnoreCase))
                desc = desc.Substring(0, desc.Length - DesignatorDescription.Length).Trim();
            int friendlyIdx = desc.IndexOf("friendly", StringComparison.InvariantCultureIgnoreCase);
            if (friendlyIdx >= 0)
                desc = desc.Remove(friendlyIdx, "friendly".Length).Trim();
            return desc.Trim();
        }
        set { }
    }

    [JsonIgnore]
    public string ShortDescription
    {
        get
        {
            return !string.IsNullOrWhiteSpace(DesignatorDescription) ? DesignatorDescription : Description.ToUpperInvariant();
        }
    }

    [JsonIgnore]
    public string DesigPlusDescription
    {
        get
        {
            string desc = Description;
            if (!string.IsNullOrWhiteSpace(desc))
            {
                foreach (var s in Enum.GetNames(typeof(Status)))
                {
                    if (desc.StartsWith(s, StringComparison.InvariantCultureIgnoreCase))
                    {
                        desc = desc.Length > s.Length ? desc.Substring(s.Length + 1) : string.Empty;
                    }
                }
            }
            return $"{DesignatorDescription.Trim()} {desc?.Trim() ?? string.Empty}".Trim();
        }
    }

    [JsonIgnore]
    public bool IsHq => Modifier is global::StpSDK.Modifier.hq or global::StpSDK.Modifier.dummy_hq or global::StpSDK.Modifier.task_force_hq or global::StpSDK.Modifier.dummytask_force_hq;

    [JsonIgnore]
    public virtual string SymbolDesignation
    {
        get
        {
            if (Type == "unit")
                return ShortDescription;
            if (!string.IsNullOrWhiteSpace(Designator1))
                return Designator1;
            return null;
        }
    }
    #endregion

    #region JMSML-backed properties

    [JsonIgnore]
    public GeometryTypeEnum GeometryType
    {
        get
        {
            if (Location?.Type is not null && Enum.TryParse(Location.Type.ToUpperInvariant(), out GeometryTypeEnum gte))
                return gte;
            return GeometryTypeEnum.NA;
        }
    }

    [JsonIgnore]
    private string _renderedSidc;
    [JsonIgnore]
    private JmsSymbol _jms;

    /// <summary>
    /// Renders the symbol from its SIDC using the bundled Joint Military Symbology
    /// Library, or returns <c>null</c> when the SIDC is empty or JMSML rendering
    /// is unavailable (hosts such as SimpleMapPlugin fall back to drawing a point).
    /// Requires <see cref="StpRecognizer.JMSSVGPath"/> to point at the SVG graphics.
    /// </summary>
    public Bitmap Bitmap(int width, int height)
    {
        // Rebuild the JMSML symbol only when the SIDC changes.
        if (!string.Equals(_renderedSidc, SymbolId, StringComparison.Ordinal))
        {
            _jms = JmsSymbol.FromSidc(SymbolId);
            _renderedSidc = SymbolId;
        }
        return _jms?.Bitmap(width, height);
    }

    #endregion

    #region Geometry helpers

    public List<LatLon> GetLinearSymbolCoords()
    {
        if (Location is null || Location.Coords is null || Location.Shape is null)
            return null;

        if (Location.Shape == "point")
            return Location.Coords;

        List<LatLon> coords;

        if (Location.Shape is not null && Location.Shape.Contains("arrowfat"))
        {
            coords = Location.Coords.GetRange(0, Location.Coords.Count - 1);
            coords.Reverse();
            coords.Add(Location.Coords.Last());
        }
        else if (Location.Shape == "ubend")
        {
            coords = new() { Location.Coords[0], Location.Coords[2], Location.Coords[3], Location.Coords[1] };
        }
        else if (Location.Shape == "ubendthreepoints")
        {
            var fourthPt = new LatLon(Location.Coords[1].Lat, Location.Coords[2].Lon);
            coords = new() { Location.Coords[0], Location.Coords[2], fourthPt, Location.Coords[1] };
        }
        else if (Location.Shape == "vee")
        {
            coords = new() { Location.Coords[1], Location.Coords[0], Location.Coords[2] };
        }
        else
        {
            coords = Location.Coords;
        }

        return coords;
    }

    #endregion

    #region Construction
    public StpSymbol() : base() { }
    public StpSymbol(string fsType) : base(fsType) { }
    #endregion
}
