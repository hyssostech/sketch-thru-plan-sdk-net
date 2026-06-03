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

    public override bool Equals(object obj)
    {
        if (obj is not LatLon other)
            return false;
        return Lat == other.Lat && Lon == other.Lon;
    }

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
