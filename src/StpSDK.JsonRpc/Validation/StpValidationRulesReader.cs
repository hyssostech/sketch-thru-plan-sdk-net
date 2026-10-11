#nullable enable
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StpSDK.Validation;

/// <summary>
/// Reads sketch-thru-plan-validations.json into <see cref="StpMethodRules"/>. It checks the shape
/// it evaluates (types, required keys, the kind vocabulary, exactly one of event/anyOf) and fails
/// with the path of the first problem, so a rule this evaluator cannot read is never half applied.
/// Keys it does not know are ignored: by the rule file's versioning, an addition an evaluator must
/// understand raises the major version, which <see cref="StpCallValidations.Load"/> refuses.
/// </summary>
internal static class StpValidationRulesReader
{
    private static readonly Dictionary<string, StpReferenceKind> Kinds = new(StringComparer.Ordinal)
    {
        ["symbol"] = StpReferenceKind.Symbol,
        ["unit"] = StpReferenceKind.Unit,
        ["task"] = StpReferenceKind.Task,
        ["coa"] = StpReferenceKind.Coa,
        ["taskOrg"] = StpReferenceKind.TaskOrg,
        ["taskOrgUnit"] = StpReferenceKind.TaskOrgUnit,
        ["taskOrgRelationship"] = StpReferenceKind.TaskOrgRelationship,
        ["any"] = StpReferenceKind.Any,
    };

    /// <summary>The kind names of the rule file, by kind.</summary>
    public static string KindName(StpReferenceKind kind) => Kinds.First(k => k.Value == kind).Key;

    public static Dictionary<string, StpMethodRules> ReadMethods(JObject methods)
    {
        var result = new Dictionary<string, StpMethodRules>(StringComparer.Ordinal);
        foreach (var property in methods.Properties())
        {
            result[property.Name] = ReadMethod(property.Name, Obj(property.Value, $"methods.{property.Name}"));
        }
        return result;
    }

    private static StpMethodRules ReadMethod(string name, JObject rule)
    {
        string at = $"methods.{name}";
        var preconditions = Strings(rule["preconditions"] ?? throw Missing(at, "preconditions"), $"{at}.preconditions");
        var required = rule["required"] == null ? Array.Empty<string>() : Strings(rule["required"]!, $"{at}.required");
        var references = rule["references"] == null
            ? Array.Empty<StpReferenceRule>()
            : Arr(rule["references"]!, $"{at}.references").Select((r, i) => ReadReference(r, $"{at}.references[{i}]")).ToArray();
        var intent = ReadIntent(Obj(rule["intent"] ?? throw Missing(at, "intent"), $"{at}.intent"), $"{at}.intent");
        string? confirm = rule["confirm"] == null ? null : Str(rule["confirm"]!, $"{at}.confirm");
        return new StpMethodRules(name, preconditions, required, references, intent, confirm);
    }

    private static StpReferenceRule ReadReference(JToken token, string at)
    {
        var reference = Obj(token, at);
        string param = Str(reference["param"] ?? throw Missing(at, "param"), $"{at}.param");
        var kindToken = reference["kind"] ?? throw Missing(at, "kind");
        var kinds = kindToken.Type == JTokenType.Array
            ? Strings(kindToken, $"{at}.kind").Select(k => Kind(k, $"{at}.kind")).ToArray()
            : new[] { Kind(Str(kindToken, $"{at}.kind"), $"{at}.kind") };
        if (kinds.Length == 0) throw Invalid($"{at}.kind", "names no kind");
        bool required = reference["required"] != null && Bool(reference["required"]!, $"{at}.required");
        return new StpReferenceRule(param, kinds, required);
    }

    private static StpIntentRule ReadIntent(JObject intent, string at)
    {
        var expect = Arr(intent["expect"] ?? throw Missing(at, "expect"), $"{at}.expect")
            .Select((e, i) => ReadExpectation(e, $"{at}.expect[{i}]")).ToArray();
        var other = intent["other"] == null
            ? Array.Empty<StpEventMatchRule>()
            : Arr(intent["other"]!, $"{at}.other").Select((o, i) => ReadEventMatch(o, $"{at}.other[{i}]")).ToArray();
        var acks = intent["acks"] == null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(Strings(intent["acks"]!, $"{at}.acks"), StringComparer.Ordinal);
        StpUnverifiableRule? unverifiable = null;
        if (intent["unverifiable"] != null)
        {
            var u = Obj(intent["unverifiable"]!, $"{at}.unverifiable");
            unverifiable = new StpUnverifiableRule(
                Str(u["issue"] ?? throw Missing($"{at}.unverifiable", "issue"), $"{at}.unverifiable.issue"),
                Str(u["reason"] ?? throw Missing($"{at}.unverifiable", "reason"), $"{at}.unverifiable.reason"));
        }
        return new StpIntentRule(expect, other, acks, unverifiable);
    }

    private static StpExpectationRule ReadExpectation(JToken token, string at)
    {
        var e = Obj(token, at);
        bool hasEvent = e["event"] != null;
        bool hasAnyOf = e["anyOf"] != null;
        if (hasEvent == hasAnyOf) throw Invalid(at, "needs exactly one of event and anyOf");
        var alternatives = hasAnyOf
            ? Arr(e["anyOf"]!, $"{at}.anyOf").Select((a, i) => ReadEventMatch(a, $"{at}.anyOf[{i}]")).ToArray()
            : new[] { ReadEventMatch(e, at) };
        string? forEach = e["forEach"] == null ? null : Str(e["forEach"]!, $"{at}.forEach");
        var where = new List<StpItemFilter>();
        if (e["where"] != null)
        {
            if (forEach == null) throw Invalid($"{at}.where", "needs forEach");
            foreach (var filter in Obj(e["where"]!, $"{at}.where").Properties())
            {
                var cond = Obj(filter.Value, $"{at}.where.{filter.Name}");
                where.Add(new StpItemFilter(
                    filter.Name,
                    cond["in"] == null ? null : Arr(cond["in"]!, $"{at}.where.{filter.Name}.in").ToArray(),
                    cond["notIn"] == null ? null : Arr(cond["notIn"]!, $"{at}.where.{filter.Name}.notIn").ToArray()));
            }
        }
        return new StpExpectationRule(alternatives, forEach, where);
    }

    private static StpEventMatchRule ReadEventMatch(JToken token, string at)
    {
        var m = Obj(token, at);
        string ev = Str(m["event"] ?? throw Missing(at, "event"), $"{at}.event");
        var match = new List<KeyValuePair<string, StpValueRef>>();
        if (m["match"] != null)
        {
            foreach (var p in Obj(m["match"]!, $"{at}.match").Properties())
            {
                match.Add(new KeyValuePair<string, StpValueRef>(p.Name, ReadValueRef(p.Value, $"{at}.match.{p.Name}")));
            }
        }
        return new StpEventMatchRule(ev, match);
    }

    private static StpValueRef ReadValueRef(JToken token, string at)
    {
        var r = Obj(token, at);
        if (r.Count != 1) throw Invalid(at, "needs exactly one of param, item and value");
        var p = r.Properties().First();
        return p.Name switch
        {
            "param" => new StpValueRef(StpValueSource.Param, Str(p.Value, $"{at}.param"), null),
            "item" => new StpValueRef(StpValueSource.Item, Str(p.Value, $"{at}.item"), null),
            "value" => new StpValueRef(StpValueSource.Value, null, p.Value),
            _ => throw Invalid(at, $"'{p.Name}' is not param, item or value"),
        };
    }

    private static StpReferenceKind Kind(string name, string at) =>
        Kinds.TryGetValue(name, out var kind) ? kind : throw Invalid(at, $"'{name}' is not a kind this evaluator knows");

    internal static JObject Obj(JToken token, string at) =>
        token as JObject ?? throw Invalid(at, "is not an object");

    private static JArray Arr(JToken token, string at) =>
        token as JArray ?? throw Invalid(at, "is not an array");

    internal static string Str(JToken token, string at) =>
        token.Type == JTokenType.String ? (string)token! : throw Invalid(at, "is not a string");

    private static bool Bool(JToken token, string at) =>
        token.Type == JTokenType.Boolean ? (bool)token : throw Invalid(at, "is not a boolean");

    private static string[] Strings(JToken token, string at) =>
        Arr(token, at).Select((t, i) => Str(t, $"{at}[{i}]")).ToArray();

    internal static FormatException Missing(string at, string key) => Invalid(at, $"has no '{key}'");

    internal static FormatException Invalid(string at, string problem) =>
        new($"call validation rules: {at} {problem}");
}
