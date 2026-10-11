#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace StpSDK.Validation;

/// <summary>A check run before a call, in the order <see cref="StpCallValidations.Validate"/> runs them.</summary>
public enum StpCheck
{
    /// <summary>The method has rules: the engine dispatches it and this evaluator can check it.</summary>
    Dispatchable,
    /// <summary>The params the engine silently ignores the call without are present.</summary>
    Params,
    /// <summary>The session state the call needs holds (an active scenario).</summary>
    Context,
    /// <summary>Every poid the call names exists and is of the right kind.</summary>
    References,
}

/// <summary>One pre-call check, passed or failed, with what it found.</summary>
public sealed class StpCheckStep
{
    internal StpCheckStep(StpCheck check, bool passed, string detail, bool skipped = false)
    {
        Check = check;
        Passed = passed;
        Detail = detail;
        Skipped = skipped;
    }

    /// <summary>Which check.</summary>
    public StpCheck Check { get; }

    /// <summary>Whether the call may go on as far as this check is concerned.</summary>
    public bool Passed { get; }

    /// <summary>
    /// True when the check could not run because the caller supplied nothing to run it on (no
    /// poid lookups). <see cref="Passed"/> is then true and <see cref="Detail"/> says what was not checked.
    /// </summary>
    public bool Skipped { get; }

    /// <summary>What the check found; for a failed check, the reason and the fix.</summary>
    public string Detail { get; }

    /// <inheritdoc/>
    public override string ToString() => $"{Check}: {(Passed ? "passed" : "failed")} - {Detail}";
}

/// <summary>What the caller knows of the session before a call.</summary>
public sealed class StpCallContext
{
    /// <summary>
    /// Whether the session has an active scenario (<c>HasActiveScenarioAsync</c>, asked just before
    /// the call); null when it could not be asked. A call that needs one is then refused.
    /// </summary>
    public bool? ActiveScenario { get; set; }

    /// <summary>Why <see cref="ActiveScenario"/> is null.</summary>
    public string? Problem { get; set; }

    /// <summary>
    /// What each poid the call names refers to, keyed by poid: from the caller's own record of
    /// events, or from <c>GetPoidObject</c> (<see cref="StpPoidLookup.FromEngineObject"/>).
    /// <see cref="StpCallValidations.ReferenceTargets"/> lists the poids to look up. When null, the
    /// references check is skipped and says so.
    /// </summary>
    public IReadOnlyDictionary<string, StpPoidLookup>? Lookups { get; set; }
}

/// <summary>The kind of an object, as its fsTYPE names it.</summary>
public enum StpObjectKind
{
    /// <summary>A symbol: unit, tactical graphic, equipment...</summary>
    Symbol,
    /// <summary>A task.</summary>
    Task,
    /// <summary>A course of action.</summary>
    Coa,
    /// <summary>A task org.</summary>
    TaskOrg,
    /// <summary>A task org unit.</summary>
    TaskOrgUnit,
    /// <summary>A task org relationship.</summary>
    TaskOrgRelationship,
    /// <summary>A planning scenario.</summary>
    Scenario,
    /// <summary>A system state object.</summary>
    SystemState,
    /// <summary>An object that carries no fsTYPE.</summary>
    Unknown,
}

/// <summary>Where a poid lookup came from.</summary>
public enum StpLookupSource
{
    /// <summary>The caller's own record of the session (the events it has seen).</summary>
    Context,
    /// <summary>The engine (<c>GetPoidObject</c>).</summary>
    Engine,
}

/// <summary>What a poid names: an object of some kind, nothing (never existed, or deleted), or unknown (the lookup failed).</summary>
public sealed class StpPoidLookup
{
    private static readonly Dictionary<string, StpObjectKind> FsTypes = new(StringComparer.Ordinal)
    {
        ["task"] = StpObjectKind.Task,
        ["coa"] = StpObjectKind.Coa,
        ["task_org"] = StpObjectKind.TaskOrg,
        ["task_org_unit"] = StpObjectKind.TaskOrgUnit,
        ["task_org_relationship"] = StpObjectKind.TaskOrgRelationship,
        ["planning_scenario"] = StpObjectKind.Scenario,
        ["system_state"] = StpObjectKind.SystemState,
        ["unit"] = StpObjectKind.Symbol,
        ["tg"] = StpObjectKind.Symbol,
        ["equipment"] = StpObjectKind.Symbol,
    };

    private StpPoidLookup(bool found, StpLookupSource source)
    {
        IsFound = found;
        Source = source;
    }

    /// <summary>Whether the poid names an existing object.</summary>
    public bool IsFound { get; private set; }

    /// <summary>Where the answer came from.</summary>
    public StpLookupSource Source { get; }

    /// <summary>The object's kind (when found).</summary>
    public StpObjectKind Kind { get; private set; } = StpObjectKind.Unknown;

    /// <summary>The object's fsTYPE, when known.</summary>
    public string? FsType { get; private set; }

    /// <summary>A short description of the object, for messages.</summary>
    public string? Summary { get; private set; }

    /// <summary>Set when <see cref="Kind"/> is a guess, saying why.</summary>
    public string? Inferred { get; private set; }

    /// <summary>The event that deleted the object, when known (not found).</summary>
    public string? DeletedBy { get; private set; }

    /// <summary>When it was deleted (with <see cref="DeletedBy"/>).</summary>
    public DateTimeOffset? DeletedAt { get; private set; }

    /// <summary>Why the lookup failed: the poid could not be checked (not found, and not known to be absent).</summary>
    public string? Error { get; private set; }

    /// <summary>An existing object.</summary>
    public static StpPoidLookup Found(StpLookupSource source, StpObjectKind kind, string? fsType = null, string? summary = null, string? inferred = null) =>
        new(true, source) { Kind = kind, FsType = fsType, Summary = summary, Inferred = inferred };

    /// <summary>A poid that names nothing; with <paramref name="deletedBy"/>, one whose object was deleted.</summary>
    public static StpPoidLookup NotFound(StpLookupSource source, string? deletedBy = null, DateTimeOffset? deletedAt = null) =>
        new(false, source) { DeletedBy = deletedBy, DeletedAt = deletedAt };

    /// <summary>A lookup that failed (for example <c>GetPoidObject</c> timed out): the poid could not be checked.</summary>
    public static StpPoidLookup Failed(StpLookupSource source, string error) =>
        new(false, source) { Error = error };

    /// <summary>
    /// The lookup a <c>GetPoidObject</c> answer gives: not found for null, otherwise the object's
    /// kind from its fsTYPE (see <see cref="KindOfFsType"/>) and its name as the summary.
    /// </summary>
    public static StpPoidLookup FromEngineObject(JToken? answer)
    {
        if (answer is not JObject obj) return NotFound(StpLookupSource.Engine);
        string? fsType = StpJsonPath.AsString(obj["fsTYPE"]);
        var kind = KindOfFsType(fsType, out string? inferred);
        string? summary = null;
        foreach (string key in new[] { "fullDescription", "name", "Name" })
        {
            string? text = StpJsonPath.AsString(obj[key]);
            if (!string.IsNullOrEmpty(text))
            {
                summary = text;
                break;
            }
        }
        return Found(StpLookupSource.Engine, kind, fsType, summary, inferred);
    }

    /// <summary>
    /// The kind an fsTYPE names. <paramref name="inferred"/> is set when that is a guess: an fsTYPE
    /// this code does not know is taken to be a symbol, and an object without one is Unknown.
    /// </summary>
    public static StpObjectKind KindOfFsType(string? fsType, out string? inferred)
    {
        inferred = null;
        if (string.IsNullOrEmpty(fsType))
        {
            inferred = "the object carries no fsTYPE";
            return StpObjectKind.Unknown;
        }
        if (FsTypes.TryGetValue(fsType!, out var kind)) return kind;
        inferred = $"kind inferred: fsTYPE '{fsType}' is not a known object type, so it is taken to be a symbol";
        return StpObjectKind.Symbol;
    }

    internal string DeletedAtText =>
        DeletedAt?.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture) ?? "";
}

/// <summary>One poid a call's params name, as the rules list it.</summary>
public sealed class StpReferenceTarget
{
    internal StpReferenceTarget(string param, string poid, IReadOnlyList<StpReferenceKind> kinds, bool required)
    {
        Param = param;
        Poid = poid;
        Kinds = kinds;
        Required = required;
    }

    /// <summary>The concrete param path, for example <c>task.tgs.1</c>.</summary>
    public string Param { get; }

    /// <summary>The poid.</summary>
    public string Poid { get; }

    /// <summary>The kinds of object it may name.</summary>
    public IReadOnlyList<StpReferenceKind> Kinds { get; }

    /// <summary>Whether it names the call's target.</summary>
    public bool Required { get; }
}

/// <summary>The poids a call names, the required ones that are absent, and the values that are not poids.</summary>
public sealed class StpReferenceTargets
{
    internal StpReferenceTargets(IReadOnlyList<StpReferenceTarget> targets, IReadOnlyList<string> missing, IReadOnlyList<string> malformed)
    {
        Targets = targets;
        Missing = missing;
        Malformed = malformed;
    }

    /// <summary>Every poid to look up.</summary>
    public IReadOnlyList<StpReferenceTarget> Targets { get; }

    /// <summary>Required poid params that are absent.</summary>
    public IReadOnlyList<string> Missing { get; }

    /// <summary>Values where a poid belongs that are not one (<c>path = value</c>).</summary>
    public IReadOnlyList<string> Malformed { get; }
}

/// <summary>The result of <see cref="StpCallValidations.Validate"/>: the call is refused, with the reason, or it may go.</summary>
public sealed class StpCallValidation
{
    internal StpCallValidation(string method, IReadOnlyList<StpCheckStep> steps, string? confirmationText)
    {
        Method = method;
        Steps = steps;
        ConfirmationText = confirmationText;
        foreach (var step in steps)
        {
            if (!step.Passed)
            {
                FailedCheck = step;
                break;
            }
        }
    }

    /// <summary>The method checked.</summary>
    public string Method { get; }

    /// <summary>Every check that ran, in order; a refusal ends with the failed one.</summary>
    public IReadOnlyList<StpCheckStep> Steps { get; }

    /// <summary>Whether the call must not be sent.</summary>
    public bool IsRefused => FailedCheck != null;

    /// <summary>The check that refused the call, or null.</summary>
    public StpCheckStep? FailedCheck { get; }

    /// <summary>Why the call is refused, and the fix; null when it may go.</summary>
    public string? Reason => FailedCheck?.Detail;

    /// <summary>Whether the method is destructive: ask the user to confirm this call before sending it.</summary>
    public bool RequiresConfirmation => ConfirmationText != null;

    /// <summary>For a destructive method, what the call destroys, with its params filled in.</summary>
    public string? ConfirmationText { get; }
}
