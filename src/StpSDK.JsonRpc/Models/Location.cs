using Newtonsoft.Json;
using System.Collections.Generic;

namespace StpSDK;

public class LatLon
{
    [JsonProperty("lat")]
    public double Lat { get; set; }

    [JsonProperty("lon")]
    public double Lon { get; set; }

    public LatLon() { }

    public LatLon(double lat, double lon)
    {
        Lat = lat;
        Lon = lon;
    }

    // Exact equality is deliberate here, and an epsilon would be a WORSE defect
    // than the one S1244 is pointing at.
    //
    // Equals has to be an equivalence relation, and epsilon equality is not
    // transitive: two points 0.6 epsilon apart each compare equal to a point
    // between them and unequal to each other. Equals and GetHashCode also have
    // to agree - equal objects must hash equal - and there is no hash function
    // that is constant across an epsilon band and still discriminating. Under a
    // tolerant Equals, every HashSet<LatLon>, Dictionary<LatLon,_>, Distinct()
    // and GroupBy() in a consumer's code misbehaves silently.
    //
    // LatLon is a public type in a published package, so that is a contract
    // callers may already depend on. A caller who needs "close enough" should
    // compare with a tolerance appropriate to their own use; the SDK cannot
    // pick that number for them, and baking one in would hide the choice.
    //
    // LatLonEqualityContractTests pins this down, so an attempt to "fix" S1244
    // here turns the suite red rather than shipping.
    //
    // SONAR-DISPOSITION: S1244 exact comparison is required by the
    // Equals/GetHashCode contract; an epsilon breaks transitivity and hash
    // agreement on a public type. See the reasoning above.
    // REVIEW: 2027-03-14
#pragma warning disable S1244 // Floating point numbers should not be tested for equality
    public override bool Equals(object obj)
    {
        if (obj is not LatLon other)
            return false;
        return Lat == other.Lat && Lon == other.Lon;
    }
#pragma warning restore S1244

    public override int GetHashCode()
    {
        unchecked
        {
            return (Lat.GetHashCode() * 397) ^ Lon.GetHashCode();
        }
    }
}

public class Location
{
    [JsonProperty("fsTYPE")]
    public string Type { get; set; }

    [JsonProperty("width")]
    public double Width { get; set; }

    [JsonProperty("altitude")]
    public double Altitude { get; set; }

    [JsonProperty("shape")]
    public string Shape { get; set; }

    [JsonProperty("radius")]
    public double Radius { get; set; }

    [JsonProperty("coords")]
    public List<LatLon> Coords { get; set; } = new();

    [JsonProperty("centroid")]
    public LatLon Centroid { get; set; }

    [JsonProperty("candidatePoids")]
    public List<string> CandidatePoids { get; set; }
}
