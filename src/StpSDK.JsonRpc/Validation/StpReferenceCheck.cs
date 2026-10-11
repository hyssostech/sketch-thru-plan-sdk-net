#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace StpSDK.Validation;

/// <summary>The references check: every poid a call names must name an existing object of the right kind.</summary>
internal static class StpReferenceCheck
{
    private static readonly Dictionary<StpReferenceKind, string> ReferenceKindNames = new()
    {
        [StpReferenceKind.Symbol] = "symbol",
        [StpReferenceKind.Unit] = "unit",
        [StpReferenceKind.Task] = "task",
        [StpReferenceKind.Coa] = "COA",
        [StpReferenceKind.TaskOrg] = "task org",
        [StpReferenceKind.TaskOrgUnit] = "task org unit",
        [StpReferenceKind.TaskOrgRelationship] = "task org relationship",
        [StpReferenceKind.Any] = "object",
    };

    private static readonly Dictionary<StpObjectKind, string> ObjectKindNames = new()
    {
        [StpObjectKind.Symbol] = "symbol",
        [StpObjectKind.Task] = "task",
        [StpObjectKind.Coa] = "COA",
        [StpObjectKind.TaskOrg] = "task org",
        [StpObjectKind.TaskOrgUnit] = "task org unit",
        [StpObjectKind.TaskOrgRelationship] = "task org relationship",
        [StpObjectKind.Scenario] = "scenario",
        [StpObjectKind.SystemState] = "system state object",
        [StpObjectKind.Unknown] = "object of unknown kind",
    };

    public static StpCheckStep Run(string method, StpReferenceTargets refs, IReadOnlyDictionary<string, StpPoidLookup> lookups)
    {
        var problems = new List<string>();
        var found = new List<string>();
        foreach (string p in refs.Missing)
        {
            problems.Add($"{p} is missing: it names the object {method} acts on, and without it STP ignores the call and says nothing");
        }
        foreach (string m in refs.Malformed) problems.Add($"{m} is not a poid");
        foreach (var t in refs.Targets)
        {
            lookups.TryGetValue(t.Poid, out var lookup);
            string? problem = Problem(t, lookup);
            if (problem != null) problems.Add(problem);
            else found.Add(Found(t, lookup!));
        }
        if (problems.Count == 0)
        {
            return new StpCheckStep(StpCheck.References, true, found.Count > 0 ? string.Join("; ", found) : $"{method} names no poid");
        }
        return new StpCheckStep(StpCheck.References, false,
            $"Refused by the references check: {string.Join("; ", problems)}. STP does not check these itself: it would ignore the call " +
            "(or apply it to the wrong object) and say nothing. Nothing was sent. Fix: take the poid from an event or a read " +
            "(GetCoaContent, GetScenarioContent, GetPoidObject), then retry.");
    }

    private static string? Problem(StpReferenceTarget t, StpPoidLookup? lookup)
    {
        string want = KindList(t.Kinds);
        if (lookup == null) return $"{t.Param} = {t.Poid} could not be looked up";
        if (!lookup.IsFound)
        {
            if (lookup.Error != null)
            {
                return $"{t.Param} = {t.Poid} could not be checked: it is not in the live context and GetPoidObject failed ({lookup.Error})";
            }
            string why = lookup.DeletedBy != null
                ? $"it was deleted ({lookup.DeletedBy} at {lookup.DeletedAtText})"
                : "it is not in the live context, and GetPoidObject answers null for it";
            return $"{t.Param} = {t.Poid} names no existing {want}: {why}";
        }
        if (t.Kinds.Any(k => KindMatches(lookup, k))) return null;
        return $"{t.Param} = {t.Poid} names a {Describe(lookup)}" +
            $"{(lookup.Summary != null ? $" ({lookup.Summary})" : "")}, not a {want}{(lookup.Inferred != null ? $" ({lookup.Inferred})" : "")}";
    }

    private static string Found(StpReferenceTarget t, StpPoidLookup lookup)
    {
        var notes = new List<string>();
        if (lookup.Inferred != null) notes.Add(lookup.Inferred);
        if (t.Kinds.Contains(StpReferenceKind.Unit) && lookup.FsType == null)
        {
            notes.Add("kind inferred: a symbol whose fsTYPE was not reported, taken to be a unit");
        }
        string name = lookup.FsType != null && lookup.Kind == StpObjectKind.Symbol ? lookup.FsType : ObjectKindNames[lookup.Kind];
        string source = lookup.Source == StpLookupSource.Context ? "live context" : "GetPoidObject";
        return $"{t.Param} = {t.Poid}: {name}{(lookup.Summary != null ? $" {lookup.Summary}" : "")} ({source})" +
            $"{(notes.Count > 0 ? $" - {string.Join("; ", notes)}" : "")}";
    }

    private static string Describe(StpPoidLookup lookup) =>
        lookup.FsType != null && lookup.Kind == StpObjectKind.Symbol ? $"{lookup.FsType} symbol" : ObjectKindNames[lookup.Kind];

    private static string KindList(IEnumerable<StpReferenceKind> kinds) => string.Join(" or ", kinds.Select(k => ReferenceKindNames[k]));

    /// <summary>Whether an object of this kind satisfies a reference.</summary>
    private static bool KindMatches(StpPoidLookup found, StpReferenceKind wanted) => wanted switch
    {
        StpReferenceKind.Any => true,
        StpReferenceKind.Unit => found.Kind == StpObjectKind.Symbol && (found.FsType == "unit" || found.FsType == null),
        StpReferenceKind.Symbol => found.Kind == StpObjectKind.Symbol,
        StpReferenceKind.Task => found.Kind == StpObjectKind.Task,
        StpReferenceKind.Coa => found.Kind == StpObjectKind.Coa,
        StpReferenceKind.TaskOrg => found.Kind == StpObjectKind.TaskOrg,
        StpReferenceKind.TaskOrgUnit => found.Kind == StpObjectKind.TaskOrgUnit,
        StpReferenceKind.TaskOrgRelationship => found.Kind == StpObjectKind.TaskOrgRelationship,
        _ => false,
    };
}
