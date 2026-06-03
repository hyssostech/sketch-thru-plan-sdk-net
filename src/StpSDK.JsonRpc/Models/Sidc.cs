using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StpSDK;

/// <summary>
/// 2525D and 2525C military symbol identification codes for a symbol, as exchanged
/// with the STP engine over JSON-RPC.
/// </summary>
/// <remarks>
/// Mirrors the JavaScript SDK's <c>Sidc</c> type. The engine sends the SIDC as an
/// object - typically the 2525D parts (<c>partA</c>, <c>partB</c>, optionally
/// <c>partC</c>), the 2525D <c>symbolSet</c>, and the 2525C <c>legacy</c> code; the
/// full 2525D <see cref="Delta"/> is reconstructed from the parts when not supplied.
/// </remarks>
[JsonConverter(typeof(SidcConverter))]
public class Sidc
{
    /// <summary>Full 2525D code (20 or 30 characters). Reconstructed from Part A/B/C when the engine sends parts.</summary>
    public string Delta { get; set; }

    /// <summary>2525D symbol set (2-character code).</summary>
    public string SymbolSet { get; set; }

    /// <summary>2525C legacy SIDC.</summary>
    public string Legacy { get; set; }

    /// <summary>Part A of the 2525D id (first 10 chars of <see cref="Delta"/>).</summary>
    [JsonIgnore]
    public string PartA => Delta != null && Delta.Length >= 10 ? Delta.Substring(0, 10) : null;

    /// <summary>Part B of the 2525D id (chars 10-19 of <see cref="Delta"/>).</summary>
    [JsonIgnore]
    public string PartB => Delta != null && Delta.Length >= 20 ? Delta.Substring(10, 10) : null;

    /// <summary>Part C of the 2525D id (chars 20-29 of <see cref="Delta"/>), when present.</summary>
    [JsonIgnore]
    public string PartC => Delta != null && Delta.Length >= 30 ? Delta.Substring(20, 10) : null;

    /// <summary>2525C identifier - the legacy code when available.</summary>
    [JsonIgnore]
    public string Charlie => Legacy;
}

/// <summary>
/// Reads the SIDC object the engine sends (<c>delta</c> or <c>partA</c>/<c>partB</c>/<c>partC</c>,
/// plus <c>legacy</c> and <c>symbolSet</c>), reconstructing <see cref="Sidc.Delta"/> from parts
/// when needed; writes back <c>delta</c>/<c>legacy</c>/<c>symbolSet</c> (matching the JavaScript SDK).
/// </summary>
public class SidcConverter : JsonConverter<Sidc>
{
    public override Sidc ReadJson(JsonReader reader, Type objectType, Sidc existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        // Tolerate a bare string id (legacy/older payloads): treat it as the 2525C legacy code.
        if (reader.TokenType == JsonToken.String)
            return new Sidc { Legacy = (string)reader.Value };

        var jo = JObject.Load(reader);
        var sidc = new Sidc
        {
            Delta = (string)jo["delta"],
            Legacy = (string)jo["legacy"],
            SymbolSet = (string)jo["symbolSet"],
        };

        if (string.IsNullOrEmpty(sidc.Delta))
        {
            string combined = $"{(string)jo["partA"]}{(string)jo["partB"]}{(string)jo["partC"]}";
            if (combined.Length > 0)
                sidc.Delta = combined;
        }

        return sidc;
    }

    public override void WriteJson(JsonWriter writer, Sidc value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        if (!string.IsNullOrEmpty(value.Delta))
        {
            writer.WritePropertyName("delta");
            writer.WriteValue(value.Delta);
        }
        if (!string.IsNullOrEmpty(value.Legacy))
        {
            writer.WritePropertyName("legacy");
            writer.WriteValue(value.Legacy);
        }
        if (!string.IsNullOrEmpty(value.SymbolSet))
        {
            writer.WritePropertyName("symbolSet");
            writer.WriteValue(value.SymbolSet);
        }
        writer.WriteEndObject();
    }
}
