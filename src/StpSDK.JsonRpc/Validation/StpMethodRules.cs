#nullable enable
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace StpSDK.Validation;

/// <summary>The kind of object a poid param must name (the rule file's <c>kind</c>).</summary>
public enum StpReferenceKind
{
    /// <summary>Any symbol: unit, tactical graphic, equipment...</summary>
    Symbol,
    /// <summary>A symbol whose fsTYPE is <c>unit</c>.</summary>
    Unit,
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
    /// <summary>An object of any kind.</summary>
    Any,
}

/// <summary>
/// The rules for one write or destructive contract method, as the pinned rule file states them.
/// Read-only; <see cref="StpCallValidations"/> evaluates them.
/// </summary>
public sealed class StpMethodRules
{
    internal StpMethodRules(
        string method,
        IReadOnlyList<string> preconditions,
        IReadOnlyList<string> required,
        IReadOnlyList<StpReferenceRule> references,
        StpIntentRule intent,
        string? confirm)
    {
        Method = method;
        Preconditions = preconditions;
        Required = required;
        References = references;
        Intent = intent;
        Confirm = confirm;
    }

    /// <summary>The contract method name.</summary>
    public string Method { get; }

    /// <summary>Session state that must hold before the call (today only <c>activeScenario</c>).</summary>
    public IReadOnlyList<string> Preconditions { get; }

    /// <summary>Params without which the engine ignores the call and says nothing (dotted paths).</summary>
    public IReadOnlyList<string> Required { get; }

    /// <summary>
    /// The destructive method's confirmation text, with <c>{path}</c> placeholders; null for a
    /// method that is not destructive. Fill it with <see cref="StpCallValidations.ConfirmationText"/>.
    /// </summary>
    public string? Confirm { get; }

    /// <summary>Whether the method is destructive: a client asks the user to confirm each call.</summary>
    public bool RequiresConfirmation => Confirm != null;

    /// <summary>The filed engine issue that makes the method's expected events unverifiable, if any.</summary>
    public string? UnverifiableIssue => Intent.Unverifiable?.Issue;

    internal IReadOnlyList<StpReferenceRule> References { get; }

    internal StpIntentRule Intent { get; }
}

/// <summary>A param whose value is a poid that must name an existing object of one of <see cref="Kinds"/>.</summary>
internal sealed class StpReferenceRule
{
    public StpReferenceRule(string param, IReadOnlyList<StpReferenceKind> kinds, bool required)
    {
        Param = param;
        Kinds = kinds;
        Required = required;
    }

    public string Param { get; }
    public IReadOnlyList<StpReferenceKind> Kinds { get; }
    public bool Required { get; }
}

/// <summary>How to tell from the event stream that a call did what it asked (V10).</summary>
internal sealed class StpIntentRule
{
    public StpIntentRule(
        IReadOnlyList<StpExpectationRule> expect,
        IReadOnlyList<StpEventMatchRule> other,
        IReadOnlyCollection<string> acks,
        StpUnverifiableRule? unverifiable)
    {
        Expect = expect;
        Other = other;
        Acks = acks;
        Unverifiable = unverifiable;
    }

    public IReadOnlyList<StpExpectationRule> Expect { get; }
    public IReadOnlyList<StpEventMatchRule> Other { get; }
    public IReadOnlyCollection<string> Acks { get; }
    public StpUnverifiableRule? Unverifiable { get; }
}

internal sealed class StpUnverifiableRule
{
    public StpUnverifiableRule(string issue, string reason)
    {
        Issue = issue;
        Reason = reason;
    }

    public string Issue { get; }
    public string Reason { get; }
}

/// <summary>One expected event, or one of several (anyOf); with forEach, one per item of a param array.</summary>
internal sealed class StpExpectationRule
{
    public StpExpectationRule(IReadOnlyList<StpEventMatchRule> alternatives, string? forEach, IReadOnlyList<StpItemFilter> where)
    {
        Alternatives = alternatives;
        ForEach = forEach;
        Where = where;
    }

    public IReadOnlyList<StpEventMatchRule> Alternatives { get; }
    public string? ForEach { get; }
    public IReadOnlyList<StpItemFilter> Where { get; }
}

/// <summary>A forEach item filter: the item's value at <see cref="Path"/> must be in, or not in, a list.</summary>
internal sealed class StpItemFilter
{
    public StpItemFilter(string path, IReadOnlyList<JToken>? @in, IReadOnlyList<JToken>? notIn)
    {
        Path = path;
        In = @in;
        NotIn = notIn;
    }

    public string Path { get; }
    public IReadOnlyList<JToken>? In { get; }
    public IReadOnlyList<JToken>? NotIn { get; }
}

/// <summary>An event, and the values its paths must hold.</summary>
internal sealed class StpEventMatchRule
{
    public StpEventMatchRule(string @event, IReadOnlyList<KeyValuePair<string, StpValueRef>> match)
    {
        Event = @event;
        Match = match;
    }

    public string Event { get; }
    public IReadOnlyList<KeyValuePair<string, StpValueRef>> Match { get; }
}

internal enum StpValueSource
{
    Param,
    Item,
    Value,
}

/// <summary>What one matched path must equal: a param, a field of the forEach item, or a literal.</summary>
internal sealed class StpValueRef
{
    public StpValueRef(StpValueSource source, string? path, JToken? value)
    {
        Source = source;
        Path = path;
        Value = value;
    }

    public StpValueSource Source { get; }
    public string? Path { get; }
    public JToken? Value { get; }
}
