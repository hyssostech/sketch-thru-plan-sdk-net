#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StpSDK.Search;

/// <summary>One symbol of a <see cref="SymbolIndex"/>.</summary>
public sealed class SymbolIndexEntry
{
    /// <summary>Creates an entry. <paramref name="phrases"/> are the spoken phrases, as typed (any case).</summary>
    public SymbolIndexEntry(string key, string deltaSidc, string charlieSidc, string symbolType, string name,
        IReadOnlyList<string> phrases, string gesture, string defaultAffiliation)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        DeltaSidc = deltaSidc ?? "";
        CharlieSidc = charlieSidc ?? "";
        SymbolType = symbolType ?? "";
        Name = name ?? "";
        Phrases = phrases ?? throw new ArgumentNullException(nameof(phrases));
        Gesture = gesture ?? "point";
        DefaultAffiliation = defaultAffiliation ?? "friend";
    }

    /// <summary>The symbol's MilTable row key: unique in the index.</summary>
    public string Key { get; }
    /// <summary>The 20-digit 2525D code.</summary>
    public string DeltaSidc { get; }
    /// <summary>The 15-character 2525C code, or "" for a 2525D-only symbol.</summary>
    public string CharlieSidc { get; }
    /// <summary>The symbol class, e.g. UNIT.</summary>
    public string SymbolType { get; }
    /// <summary>The 2525D name.</summary>
    public string Name { get; }
    /// <summary>The spoken phrases a user says or types for the symbol.</summary>
    public IReadOnlyList<string> Phrases { get; }
    /// <summary>The gesture that places it: point, or a tactical graphic's line, area, arrow ...</summary>
    public string Gesture { get; }
    /// <summary>"hostile" for the symbols users place as enemy by default, else "friend".</summary>
    public string DefaultAffiliation { get; }
}

/// <summary>
/// The symbol index <see cref="SymbolSearch"/> searches (STP-1373): one entry per symbol with its
/// codes, name, spoken phrases, placing gesture and default affiliation. Format
/// <c>stp.symbol-index.v1</c>, json-api/sketch-thru-plan-symbol-index.schema.json of
/// hyssostech/sketch-thru-plan-sdk-js.
/// </summary>
/// <remarks>
/// The index is not part of this SDK: STP generates it from its symbol tables and ships it with STP.
/// <see cref="Load"/> reads it from a path; a missing or unreadable index is an error
/// (<see cref="SymbolIndexUnavailableException"/>), never an empty index - an empty result would read
/// as "nothing matches".
/// </remarks>
public sealed class SymbolIndex
{
    /// <summary>The <c>schema</c> value this SDK reads.</summary>
    public const string Schema = "stp.symbol-index.v1";

    private SymbolIndex(string source, string sets, IReadOnlyList<SymbolIndexEntry> entries)
    {
        Source = source;
        Sets = sets;
        Entries = entries;
    }

    /// <summary>Where the index was read from.</summary>
    public string Source { get; }
    /// <summary>The symbol-set profile it was built for.</summary>
    public string Sets { get; }
    /// <summary>The symbols, ordered by key.</summary>
    public IReadOnlyList<SymbolIndexEntry> Entries { get; }

    /// <summary>Reads the index at <paramref name="path"/>.</summary>
    /// <exception cref="SymbolIndexUnavailableException">The file is missing, unreadable, or not a <see cref="Schema"/> index.</exception>
    public static SymbolIndex Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new SymbolIndexUnavailableException(path ?? "", "no path given");
        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException || ex is ArgumentException)
        {
            throw new SymbolIndexUnavailableException(path, ex is FileNotFoundException || ex is DirectoryNotFoundException ? "not found" : ex.Message);
        }
        return Parse(text, path);
    }

    /// <summary>Reads an index from its JSON text; <paramref name="source"/> names it in errors.</summary>
    /// <exception cref="SymbolIndexUnavailableException">The text is not a <see cref="Schema"/> index.</exception>
    public static SymbolIndex Parse(string json, string source)
    {
        JObject root;
        try
        {
            root = JObject.Parse(json ?? "");
        }
        catch (JsonException ex)
        {
            throw new SymbolIndexUnavailableException(source, "not JSON: " + ex.Message);
        }

        string schema = (string?)root["schema"] ?? "";
        if (schema != Schema)
            throw new SymbolIndexUnavailableException(source, "schema is '" + schema + "', not '" + Schema + "'");
        if (root["entries"] is not JArray array)
            throw new SymbolIndexUnavailableException(source, "no entries array");

        var entries = new List<SymbolIndexEntry>(array.Count);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in array)
        {
            if (token is not JObject e || e["phrases"] is not JArray phrases || string.IsNullOrEmpty((string?)e["key"]))
                throw new SymbolIndexUnavailableException(source, "entry " + entries.Count + " has no key or no phrases");
            string key = (string)e["key"]!;
            if (!keys.Add(key))
                throw new SymbolIndexUnavailableException(source, "key '" + key + "' appears twice");
            entries.Add(new SymbolIndexEntry(
                key,
                (string?)e["deltaSidc"] ?? "",
                (string?)e["charlieSidc"] ?? "",
                (string?)e["symbolType"] ?? "",
                (string?)e["name"] ?? "",
                phrases.Select(p => (string?)p ?? "").Where(p => p.Trim().Length > 0).ToList(),
                (string?)e["gesture"] ?? "point",
                (string?)e["defaultAffiliation"] ?? "friend"));
        }

        return new SymbolIndex(source, (string?)root["sets"] ?? "", entries.OrderBy(x => x.Key, StringComparer.Ordinal).ToList());
    }
}

/// <summary>
/// The symbol index could not be read. The message is the contract's refusal for SearchSymbol:
/// <c>symbol index not available: &lt;path&gt; (&lt;reason&gt;)</c>.
/// </summary>
public sealed class SymbolIndexUnavailableException : Exception
{
    /// <summary>Creates the refusal for the index at <paramref name="path"/>.</summary>
    public SymbolIndexUnavailableException(string path, string reason)
        : base("symbol index not available: " + path + " (" + reason + ")")
    {
        IndexPath = path;
        Reason = reason;
    }

    /// <summary>The path that was tried.</summary>
    public string IndexPath { get; }
    /// <summary>Why it could not be read.</summary>
    public string Reason { get; }
}
