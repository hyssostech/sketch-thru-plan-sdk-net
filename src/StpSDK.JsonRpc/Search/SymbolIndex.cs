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

    /// <summary>The index's file name in an STP installation.</summary>
    public const string FileName = "symbol-index.json";

    /// <summary>The environment variable that names the index when no path is given.</summary>
    public const string PathVariable = "STP_SYMBOL_INDEX";

    /// <summary>
    /// Finds and reads the index: <paramref name="path"/> when given; else the file
    /// <see cref="PathVariable"/> names; else the one installed with STP - the highest STP version
    /// under Program Files (<c>STP-&lt;edition&gt;-&lt;version&gt;\Presenter\Tools\SicListIndex\symbol-index.json</c>),
    /// Desktop before Server on the same version. <paramref name="log"/> hears the path chosen.
    /// </summary>
    /// <remarks>
    /// A path given, or named by the variable, that does not hold an index is an error: it does not
    /// fall back to the installation, so a wrong setting is never silently replaced.
    /// </remarks>
    /// <exception cref="SymbolIndexUnavailableException">No index was found; the message names every path tried.</exception>
    public static SymbolIndex LoadDefault(string? path = null, Action<string>? log = null)
        => LoadDefault(path, Environment.GetEnvironmentVariable(PathVariable), ProgramFilesFolders(), log);

    internal static SymbolIndex LoadDefault(string? path, string? variablePath, IEnumerable<string> programFiles, Action<string>? log = null)
    {
        if (!string.IsNullOrWhiteSpace(path))
            return Chosen(path!, "given", log);
        if (!string.IsNullOrWhiteSpace(variablePath))
            return Chosen(variablePath!, PathVariable, log);

        var tried = new List<string>();
        foreach (var dir in Installations(programFiles))
        {
            string candidate = Path.Combine(dir, "Presenter", "Tools", "SicListIndex", FileName);
            tried.Add(candidate);
            if (File.Exists(candidate))
                return Chosen(candidate, "the STP installation", log);
        }
        string where = tried.Count > 0 ? string.Join("; ", tried) : "no STP installation under " + string.Join("; ", programFiles);
        throw new SymbolIndexUnavailableException(where,
            "not found - give the index's path, or set " + PathVariable + " to it");
    }

    private static SymbolIndex Chosen(string path, string how, Action<string>? log)
    {
        log?.Invoke("SearchSymbol: symbol index " + path + " (" + how + ")");
        return Load(path);
    }

    private static IEnumerable<string> ProgramFilesFolders()
        => new[] { Environment.GetEnvironmentVariable("ProgramW6432"), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) }
            .Where(d => !string.IsNullOrEmpty(d)).Select(d => d!).Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>STP installation folders (STP-&lt;edition&gt;-&lt;version&gt;), highest version first, Desktop before Server.</summary>
    private static IEnumerable<string> Installations(IEnumerable<string> programFiles)
    {
        var found = new List<(string Dir, Version Version, int Edition)>();
        foreach (var root in programFiles.Where(Directory.Exists))
        {
            foreach (var dir in Directory.GetDirectories(root, "STP-*"))
            {
                string name = Path.GetFileName(dir);
                int dash = name.LastIndexOf('-');
                if (dash <= 4 || !Version.TryParse(name.Substring(dash + 1), out var version))
                    continue;
                string edition = name.Substring(4, dash - 4);
                int rank = edition == "Desktop" ? 0 : edition == "Server" ? 1 : 2;
                found.Add((dir, version, rank));
            }
        }
        return found.OrderByDescending(f => f.Version).ThenBy(f => f.Edition).ThenBy(f => f.Dir, StringComparer.Ordinal).Select(f => f.Dir);
    }

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
