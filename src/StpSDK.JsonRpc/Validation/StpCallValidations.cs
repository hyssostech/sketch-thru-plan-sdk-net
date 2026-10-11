#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StpSDK.Validation;

/// <summary>
/// The shared call-validation rules (STP-1064), evaluated around a state-changing STP call: before
/// the call, refuse it with the reason; after it, explain what the engine did (V10, diff intended
/// vs actual). Never a silent failure.
/// </summary>
/// <remarks>
/// <para>
/// The rules are data: <c>json-api/sketch-thru-plan-validations.json</c> of
/// hyssostech/sketch-thru-plan-sdk-js, the file the stp-mcp live server evaluates too. This SDK
/// embeds a verbatim copy pinned to a commit (<see cref="Pin"/>); <see cref="Default"/> reads it.
/// The vocabulary is documented in that repository's json-api/README.md, "Call validations".
/// </para>
/// <para>
/// Pure: nothing here does I/O. The caller gathers the facts (whether the session has an active
/// scenario, what each poid names, the events that arrived after the call) and this class judges
/// them. <see cref="StpRecognizer"/> is not involved, so any client can use it.
/// </para>
/// <para>Before the call:</para>
/// <code>
/// var rules = StpCallValidations.Default;
/// var p = JObject.FromObject(new { poid });
/// var targets = rules.ReferenceTargets("DeleteSymbol", p);  // poids to look up (GetPoidObject)
/// var check = rules.Validate("DeleteSymbol", p, new StpCallContext { ActiveScenario = true, Lookups = lookups });
/// if (check.IsRefused) throw new InvalidOperationException(check.Reason);
/// if (check.RequiresConfirmation &amp;&amp; !AskUser(check.ConfirmationText)) return;
/// </code>
/// <para>After the call, feed every event that arrived (or use <see cref="Diff"/>):</para>
/// <code>
/// var watch = rules.Watch("DeleteSymbol", p);
/// // on each event: if (watch.Observe(name, eventParams, DateTimeOffset.UtcNow)) stop waiting
/// var report = watch.Report(StpCallEnd.Inform(waited, timeout));
/// if (report.IsError) Console.WriteLine(report.Summary);
/// </code>
/// </remarks>
public sealed class StpCallValidations
{
    /// <summary>The major version of the rule vocabulary this evaluator reads.</summary>
    public const int SupportedMajorVersion = 1;

    private const string RulesResource = "StpSDK.Validation.sketch-thru-plan-validations.json";
    private const string PinResource = "StpSDK.Validation.sketch-thru-plan-validations.pin.json";

    private static readonly Lazy<StpCallValidations> DefaultRules = new(() => Load(ReadResource(RulesResource)));
    private static readonly Lazy<StpCallValidationsPin> DefaultPin = new(() => StpCallValidationsPin.Parse(ReadResource(PinResource)));
    private static readonly Regex Placeholder = new(@"\{([^}]+)\}", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private readonly Dictionary<string, StpMethodRules> _methods;

    private StpCallValidations(string version, string contractVersion, StpRefusalMessage refusalMessage, Dictionary<string, StpMethodRules> methods)
    {
        Version = version;
        ContractVersion = contractVersion;
        RefusalMessage = refusalMessage;
        _methods = methods;
    }

    /// <summary>The rules embedded in this SDK: the copy <see cref="Pin"/> describes.</summary>
    public static StpCallValidations Default => DefaultRules.Value;

    /// <summary>Where the embedded rules came from: repository, path, commit, version, hash.</summary>
    public static StpCallValidationsPin Pin => DefaultPin.Value;

    /// <summary>The rule file's version.</summary>
    public string Version { get; }

    /// <summary>The contract version the rules were checked against.</summary>
    public string ContractVersion { get; }

    /// <summary>How the engine announces that it refused a fire-and-forget call.</summary>
    public StpRefusalMessage RefusalMessage { get; }

    /// <summary>The methods that have rules: every write and destructive method the engine dispatches.</summary>
    public IReadOnlyCollection<string> Methods => _methods.Keys;

    /// <summary>
    /// Reads a rule file (sketch-thru-plan-validations.json text). Throws <see cref="FormatException"/>
    /// when it is not one this evaluator can apply: a major version other than
    /// <see cref="SupportedMajorVersion"/>, or a rule of the wrong shape (the message names its path).
    /// </summary>
    public static StpCallValidations Load(string json)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));
        JObject root;
        try
        {
            root = JObject.Parse(json);
        }
        catch (JsonReaderException e)
        {
            throw new FormatException($"call validation rules: not JSON ({e.Message})", e);
        }
        string version = StpValidationRulesReader.Str(root["version"] ?? throw StpValidationRulesReader.Missing("the file", "version"), "version");
        string major = version.Split('.')[0];
        if (major != SupportedMajorVersion.ToString(System.Globalization.CultureInfo.InvariantCulture))
        {
            throw new FormatException(
                $"call validation rules version {version}, but this evaluator reads major version {SupportedMajorVersion}; update the SDK");
        }
        string contractVersion = StpValidationRulesReader.Str(root["contractVersion"] ?? throw StpValidationRulesReader.Missing("the file", "contractVersion"), "contractVersion");
        var refusal = StpValidationRulesReader.Obj(root["refusalMessage"] ?? throw StpValidationRulesReader.Missing("the file", "refusalMessage"), "refusalMessage");
        var refusalMessage = new StpRefusalMessage(
            StpValidationRulesReader.Str(refusal["event"] ?? throw StpValidationRulesReader.Missing("refusalMessage", "event"), "refusalMessage.event"),
            StpValidationRulesReader.Str(refusal["contains"] ?? throw StpValidationRulesReader.Missing("refusalMessage", "contains"), "refusalMessage.contains"));
        var methods = StpValidationRulesReader.ReadMethods(
            StpValidationRulesReader.Obj(root["methods"] ?? throw StpValidationRulesReader.Missing("the file", "methods"), "methods"));
        return new StpCallValidations(version, contractVersion, refusalMessage, methods);
    }

    /// <summary>The rules for a method, or false when it has none.</summary>
    public bool TryGetRules(string method, out StpMethodRules rules)
    {
        if (method != null && _methods.TryGetValue(method, out var found))
        {
            rules = found;
            return true;
        }
        rules = null!;
        return false;
    }

    // ---- before the call ---------------------------------------------------------------------

    /// <summary>
    /// Every check before a call, in order: dispatchable, params, context, references. It stops at
    /// the first that fails; the result is then refused, with the reason and the fix. A destructive
    /// method that passes carries its <see cref="StpCallValidation.ConfirmationText"/>: ask the user.
    /// </summary>
    /// <param name="method">The contract method name.</param>
    /// <param name="params">The params the call would send (null for none).</param>
    /// <param name="context">What the caller knows of the session (null: nothing).</param>
    public StpCallValidation Validate(string method, JObject? @params, StpCallContext? context)
    {
        var p = @params ?? new JObject();
        var ctx = context ?? new StpCallContext();
        var steps = new List<StpCheckStep>();
        StpCallValidation Done(StpCheckStep step, string? confirmation = null)
        {
            steps.Add(step);
            return new StpCallValidation(method, steps, confirmation);
        }

        var dispatchable = CheckDispatchable(method);
        if (!dispatchable.Passed) return Done(dispatchable);
        steps.Add(dispatchable);
        var rules = _methods[method];

        var required = CheckRequired(method, p);
        if (!required.Passed) return Done(required);
        steps.Add(required);

        var preconditions = CheckPreconditions(method, ctx);
        if (!preconditions.Passed) return Done(preconditions);
        steps.Add(preconditions);

        var targets = ReferenceTargets(method, p);
        StpCheckStep references;
        if (ctx.Lookups != null) references = CheckReferences(method, targets, ctx.Lookups);
        else if (targets.Missing.Count > 0 || targets.Malformed.Count > 0)
        {
            var unlooked = new StpReferenceTargets(Array.Empty<StpReferenceTarget>(), targets.Missing, targets.Malformed);
            references = CheckReferences(method, unlooked, new Dictionary<string, StpPoidLookup>());
        }
        else if (targets.Targets.Count == 0) references = new StpCheckStep(StpCheck.References, true, $"{method} names no poid");
        else
        {
            references = new StpCheckStep(StpCheck.References, true,
                $"Not checked: no poid lookups were supplied, so whether {string.Join(", ", targets.Targets.Select(t => $"{t.Param} = {t.Poid}"))} " +
                "names an existing object of the right kind is unknown. STP ignores a call on an unknown poid and says nothing.",
                skipped: true);
        }
        return Done(references, rules.RequiresConfirmation ? ConfirmationText(method, p) : null);
    }

    /// <summary>The method must have rules: the evaluator refuses rather than let an unchecked write through.</summary>
    public StpCheckStep CheckDispatchable(string method)
    {
        if (method != null && _methods.ContainsKey(method))
        {
            return new StpCheckStep(StpCheck.Dispatchable, true, $"{method} is a write or destructive method the engine dispatches (rules {Version})");
        }
        return new StpCheckStep(StpCheck.Dispatchable, false,
            $"{method} has no entry in the call validation rules (version {Version}, contract {ContractVersion}): it is not a write or " +
            "destructive method the engine dispatches, so it cannot be checked. Refused rather than sent unchecked.");
    }

    /// <summary>The params the engine silently ignores a call without (the rule file's <c>required</c>).</summary>
    public StpCheckStep CheckRequired(string method, JObject? @params)
    {
        var rules = RulesOf(method);
        var missing = rules.Required.Where(path => StpJsonPath.Expand(@params, path).Count == 0).ToList();
        if (missing.Count == 0) return new StpCheckStep(StpCheck.Params, true, "the params carry every param STP silently needs");
        string names = string.Join(" and ", missing);
        return new StpCheckStep(StpCheck.Params, false,
            $"Refused by the params check: {method} needs {names}. Without {(missing.Count == 1 ? "it" : "them")} STP ignores the call " +
            $"and says nothing, so it would look done when nothing happened. Nothing was sent; pass {names} (see the method's params in the contract).");
    }

    /// <summary>The session state the rules say the call needs (an active scenario).</summary>
    public StpCheckStep CheckPreconditions(string method, StpCallContext? context)
    {
        var rules = RulesOf(method);
        var facts = context ?? new StpCallContext();
        var unknown = rules.Preconditions.Where(p => p != "activeScenario").ToList();
        if (unknown.Count > 0)
        {
            return new StpCheckStep(StpCheck.Context, false,
                $"Refused by the context check: {method} needs {string.Join(" and ", unknown)}, a precondition this evaluator does not know " +
                $"(rules {Version}). Nothing was sent; update the SDK.");
        }
        if (!rules.Preconditions.Contains("activeScenario")) return new StpCheckStep(StpCheck.Context, true, $"{method} needs no active scenario");
        if (facts.ActiveScenario == true) return new StpCheckStep(StpCheck.Context, true, "the session has an active scenario");
        if (facts.ActiveScenario == null)
        {
            return new StpCheckStep(StpCheck.Context, false,
                $"Refused by the context check: could not tell whether the session has an active scenario ({facts.Problem ?? "no answer"}). " +
                "Nothing was sent; check the connection and retry.");
        }
        return new StpCheckStep(StpCheck.Context, false,
            $"Refused by the context check: the session has no active scenario. STP would accept {method} anyway and create " +
            "objects that belong to no scenario (HasActiveScenario stays false), so nothing was sent. Fix: have the user " +
            "create or load a scenario in their STP client (or call CreateNewScenario / LoadNewScenario), then retry.");
    }

    /// <summary>The poids a call's params name, as the rules list them; the required ones absent, and the values that are not poids.</summary>
    public StpReferenceTargets ReferenceTargets(string method, JObject? @params)
    {
        var rules = RulesOf(method);
        var targets = new List<StpReferenceTarget>();
        var missing = new List<string>();
        var malformed = new List<string>();
        foreach (var reference in rules.References)
        {
            var found = StpJsonPath.Expand(@params, reference.Param);
            if (found.Count == 0 && reference.Required) missing.Add(reference.Param);
            foreach (var pair in found)
            {
                string? poid = StpJsonPath.AsString(pair.Value);
                if (poid == null || poid.Trim().Length == 0) malformed.Add($"{pair.Key} = {StpJsonPath.Show(pair.Value)}");
                else targets.Add(new StpReferenceTarget(pair.Key, poid, reference.Kinds, reference.Required));
            }
        }
        return new StpReferenceTargets(targets, missing, malformed);
    }

    /// <summary>
    /// Every referenced poid must name an existing object of the right kind. The engine does not
    /// check most of them: it ignores the call, or (UpdateTask) creates an object, and says nothing.
    /// </summary>
    public StpCheckStep CheckReferences(string method, StpReferenceTargets targets, IReadOnlyDictionary<string, StpPoidLookup> lookups)
    {
        if (targets == null) throw new ArgumentNullException(nameof(targets));
        if (lookups == null) throw new ArgumentNullException(nameof(lookups));
        return StpReferenceCheck.Run(method, targets, lookups);
    }

    /// <summary>A destructive call's description for the user's confirmation: the rule's text with the params filled in.</summary>
    public string ConfirmationText(string method, JObject? @params)
    {
        var rules = RulesOf(method);
        return Placeholder.Replace(rules.Confirm ?? "", m =>
        {
            string path = m.Groups[1].Value;
            var value = StpJsonPath.Get(@params, path);
            return StpJsonPath.IsAbsent(value) ? $"(no {path})" : StpJsonPath.Text(value!);
        });
    }

    // ---- after the call ----------------------------------------------------------------------

    /// <summary>
    /// Starts V10 for a call: feed it every event that arrives after the call is sent
    /// (<see cref="StpCallWatch.Observe(string, JToken, DateTimeOffset)"/>), then ask for the report.
    /// </summary>
    /// <param name="method">The contract method name.</param>
    /// <param name="params">The params the call sent.</param>
    /// <param name="knownIssues">
    /// The method's <c>x-stpKnownIssues</c> in the contract of the engine in use: a rule's
    /// <c>unverifiable</c> applies only while its issue is listed. Null means the contract the rules
    /// were checked against (<see cref="ContractVersion"/>), where every such issue is listed.
    /// </param>
    public StpCallWatch Watch(string method, JObject? @params, IEnumerable<string>? knownIssues = null)
    {
        if (method == null || !_methods.TryGetValue(method, out var rules))
        {
            throw new ArgumentException($"{method} has no entry in the call validation rules (version {Version}), so its result cannot be explained", nameof(method));
        }
        return new StpCallWatch(method, rules, @params ?? new JObject(), RefusalMessage, knownIssues);
    }

    /// <summary>V10 in one step: the events that arrived after the call, in order, and how the call ended.</summary>
    public StpCallReport Diff(string method, JObject? @params, IEnumerable<StpEvent> events, StpCallEnd end, IEnumerable<string>? knownIssues = null)
    {
        if (events == null) throw new ArgumentNullException(nameof(events));
        var watch = Watch(method, @params, knownIssues);
        foreach (var e in events) watch.Observe(e);
        return watch.Report(end);
    }

    private StpMethodRules RulesOf(string method)
    {
        if (method != null && _methods.TryGetValue(method, out var rules)) return rules;
        throw new ArgumentException($"{method} has no entry in the call validation rules (version {Version}); check CheckDispatchable first", nameof(method));
    }

    private static string ReadResource(string name)
    {
        using var stream = typeof(StpCallValidations).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"the SDK assembly carries no {name} resource");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}

/// <summary>How the engine announces that it refused a fire-and-forget call: an event whose message contains a text.</summary>
public sealed class StpRefusalMessage
{
    internal StpRefusalMessage(string @event, string contains)
    {
        Event = @event;
        Contains = contains;
    }

    /// <summary>The event (StpMessage).</summary>
    public string Event { get; }

    /// <summary>The text its message contains, with <c>{method}</c> standing for the method name.</summary>
    public string Contains { get; }

    /// <summary>The text for one method.</summary>
    public string For(string method) => Contains.Replace("{method}", method);
}

/// <summary>Where the embedded rule file came from (sketch-thru-plan-validations.pin.json).</summary>
public sealed class StpCallValidationsPin
{
    private StpCallValidationsPin() { }

    /// <summary>The source repository (hyssostech/sketch-thru-plan-sdk-js).</summary>
    public string Repo { get; private set; } = "";

    /// <summary>The rule file's path in it.</summary>
    public string Path { get; private set; } = "";

    /// <summary>The rule file's schema's path in it.</summary>
    public string SchemaPath { get; private set; } = "";

    /// <summary>The commit the copy was taken from.</summary>
    public string Ref { get; private set; } = "";

    /// <summary>The rule file's version at that commit.</summary>
    public string Version { get; private set; } = "";

    /// <summary>The contract version the rules were checked against.</summary>
    public string ContractVersion { get; private set; } = "";

    /// <summary>SHA-256 of the rule file (LF line endings), hex.</summary>
    public string Sha256 { get; private set; } = "";

    /// <summary>SHA-256 of the schema (LF line endings), hex.</summary>
    public string SchemaSha256 { get; private set; } = "";

    internal static StpCallValidationsPin Parse(string json)
    {
        var o = JObject.Parse(json);
        string Field(string key) => StpValidationRulesReader.Str(o[key] ?? throw StpValidationRulesReader.Missing("the pin", key), key);
        return new StpCallValidationsPin
        {
            Repo = Field("repo"),
            Path = Field("path"),
            SchemaPath = Field("schemaPath"),
            Ref = Field("ref"),
            Version = Field("version"),
            ContractVersion = Field("contractVersion"),
            Sha256 = Field("sha256"),
            SchemaSha256 = Field("schemaSha256"),
        };
    }
}
