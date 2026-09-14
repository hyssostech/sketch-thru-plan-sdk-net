using Newtonsoft.Json;
using System;

namespace StpSDK;

// S101 wants DISCode renamed to DisCode to match pascal-case conventions. DIS is
// an acronym - Distributed Interactive Simulation - and the type is public,
// shipped API. A rename breaks every consumer that names the type.
//
// Unlike the S3875 case below, this break would at least be LOUD: consumers get
// a compile error rather than silently different behaviour. It is still a break
// with no benefit to anyone but the naming convention, on a type whose name
// matches the standard it models.
//
// SONAR-DISPOSITION: S101 public API type named after an acronym from the
// standard it models; renaming is a source-breaking change for every consumer.
// REVIEW: 2027-03-14
#pragma warning disable S101 // Types should be named in PascalCase
public class DISCode
{
    [JsonProperty("category")]
    public sbyte Category { get; set; }

    [JsonProperty("country")]
    public string Country { get; set; }

    [JsonProperty("domain")]
    public sbyte Domain { get; set; }

    [JsonProperty("extra")]
    public sbyte Extra { get; set; }

    [JsonProperty("kind")]
    public sbyte Kind { get; set; }

    [JsonProperty("specific")]
    public sbyte Specific { get; set; }

    [JsonProperty("subcategory")]
    public sbyte SubCategory { get; set; }

    public DISCode() { }

    public override bool Equals(object obj)
    {
        if (obj is not DISCode other)
            return false;
        return Category == other.Category &&
               Country == other.Country &&
               Domain == other.Domain &&
               Extra == other.Extra &&
               Kind == other.Kind &&
               Specific == other.Specific &&
               SubCategory == other.SubCategory;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)2166136261;
            hash = (hash * 16777619) ^ Category.GetHashCode();
            hash = (hash * 16777619) ^ (Country?.GetHashCode() ?? 0);
            hash = (hash * 16777619) ^ Domain.GetHashCode();
            hash = (hash * 16777619) ^ Extra.GetHashCode();
            hash = (hash * 16777619) ^ Kind.GetHashCode();
            hash = (hash * 16777619) ^ Specific.GetHashCode();
            hash = (hash * 16777619) ^ SubCategory.GetHashCode();
            return hash;
        }
    }

    // S3875 objects to overloading == on a reference type, on the grounds that
    // callers expect reference equality. Suppressed rather than obeyed, for two
    // reasons.
    //
    // First, DISCode is a value-like data holder - seven scalar fields off the
    // wire - and value semantics are what a caller comparing two of them means.
    // Equals, GetHashCode, == and != are all implemented and consistent, and the
    // null handling is correct, so the surprise the rule guards against is not
    // present here.
    //
    // Second, and decisively: DISCode is public, shipped API. Deleting this
    // operator does not tidy anything - it silently changes `a == b` from value
    // comparison to reference comparison in code that already compiles, which is
    // the worst kind of breaking change. A consumer would get no error, just
    // different answers.
    //
    // If this is ever revisited, the move is a record or a struct, not removal.
    //
    // SONAR-DISPOSITION: S3875 value-like public type; removing the operator
    // silently changes == from value to reference comparison in code that
    // already compiles. See the reasoning above.
    // REVIEW: 2027-03-14
#pragma warning disable S3875 // Operator == should not be overloaded on reference types
    public static bool operator ==(DISCode a, DISCode b)
    {
        if (a is null) return b is null;
        return a.Equals(b);
    }
#pragma warning restore S3875

    public static bool operator !=(DISCode a, DISCode b) => !(a == b);
}
#pragma warning restore S101

public class Resource
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("operationalQuantity")]
    public double OperationalQuantity { get; set; }

    [JsonProperty("disCode")]
    public DISCode DisCode { get; set; }

    [JsonProperty("onHandQuantity")]
    public double? OnHandQuantity { get; set; }

    [JsonProperty("requiredOnHandQuantity")]
    public double? RequiredOnHandQuantity { get; set; }
}
