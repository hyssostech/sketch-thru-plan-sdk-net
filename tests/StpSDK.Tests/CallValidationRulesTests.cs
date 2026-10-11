using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK.Validation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace StpSDK.Tests;

/// <summary>
/// STP-1064: the rule file this SDK embeds is the pinned, verbatim copy its pin record names, this
/// evaluator reads every keyword of the pinned schema, and the parts of the C# API the parity
/// cases do not reach (Validate's order, Diff, the default for known issues) do what they say.
/// </summary>
[TestFixture]
public class CallValidationRulesTests
{
    private static readonly StpCallValidations Rules = StpCallValidations.Default;
    private static readonly DateTimeOffset At = new(2026, 10, 10, 12, 0, 0, TimeSpan.Zero);

    private static string Sha256(string text)
    {
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n")));
        return string.Concat(hash.Select(b => b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)));
    }

    private static JObject Schema() =>
        JObject.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "CallValidations", "sketch-thru-plan-validations.schema.json")));

    // ---- the pin -----------------------------------------------------------------------------

    [Test]
    public void TheEmbeddedRuleFile_IsTheVersionItsPinNames()
    {
        var pin = StpCallValidations.Pin;
        Assert.That(pin.Repo, Is.EqualTo("hyssostech/sketch-thru-plan-sdk-js"));
        Assert.That(pin.Path, Is.EqualTo("json-api/sketch-thru-plan-validations.json"));
        Assert.That(pin.Ref, Does.Match("^[0-9a-f]{40}$"), "pin a full commit sha");
        Assert.That(Rules.Version, Is.EqualTo(pin.Version));
        Assert.That(Rules.ContractVersion, Is.EqualTo(pin.ContractVersion));
    }

    [Test]
    public void TheEmbeddedRuleFile_IsAVerbatimCopy()
    {
        Assert.That(Sha256(CallValidationParityTests.ReadEmbeddedRules()), Is.EqualTo(StpCallValidations.Pin.Sha256),
            "the rule file is never hand-edited: re-pin it with scripts/update-call-validations.mjs");
    }

    [Test]
    public void ThePinnedSchema_IsAVerbatimCopy()
    {
        string text = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "CallValidations", "sketch-thru-plan-validations.schema.json"));
        Assert.That(Sha256(text), Is.EqualTo(StpCallValidations.Pin.SchemaSha256));
    }

    [Test]
    public void EveryWriteAndDestructiveMethodHasRules()
    {
        Assert.That(Rules.Methods.Count, Is.GreaterThanOrEqualTo(50));
        Assert.That(Rules.Methods, Does.Contain("AddCoa").And.Contain("DeleteSymbol").And.Contain("ImportPlanDataFromObjectSet"));
        Assert.That(Rules.TryGetRules("DeleteSymbol", out var delete), Is.True);
        Assert.That(delete.RequiresConfirmation, Is.True);
        Assert.That(Rules.TryGetRules("AddCoa", out var add) && !add.RequiresConfirmation, Is.True);
        Assert.That(Rules.TryGetRules("GetPoidObject", out _), Is.False, "a read has no rules");
    }

    // ---- the schema's vocabulary -------------------------------------------------------------

    private static IEnumerable<string> PropertiesOf(JObject schema, string definition) =>
        ((JObject)schema["definitions"]![definition]!["properties"]!).Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal);

    private static string[] Sorted(params string[] names) => names.OrderBy(n => n, StringComparer.Ordinal).ToArray();

    [Test]
    public void TheEvaluatorReadsEveryKeywordThePinnedSchemaDefines()
    {
        // A keyword the schema gains, and this evaluator ignores, would be a check silently not
        // made. Re-pinning a schema with a new keyword fails here until the evaluator reads it.
        var schema = Schema();
        Assert.That(((JObject)schema["properties"]!).Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal),
            Is.EqualTo(Sorted("$schema", "title", "description", "version", "contractVersion", "refusalMessage", "methods")));
        Assert.That(PropertiesOf(schema, "method"), Is.EqualTo(Sorted("preconditions", "required", "references", "intent", "confirm")));
        Assert.That(PropertiesOf(schema, "reference"), Is.EqualTo(Sorted("param", "kind", "required")));
        Assert.That(PropertiesOf(schema, "intent"), Is.EqualTo(Sorted("expect", "other", "acks", "unverifiable")));
        Assert.That(PropertiesOf(schema, "expectation"), Is.EqualTo(Sorted("event", "match", "anyOf", "forEach", "where")));
        Assert.That(PropertiesOf(schema, "eventMatch"), Is.EqualTo(Sorted("event", "match")));
        Assert.That(PropertiesOf(schema, "valueRef"), Is.EqualTo(Sorted("param", "item", "value")));
        Assert.That(schema["definitions"]!["method"]!["properties"]!["preconditions"]!["items"]!["enum"]!.Select(t => (string?)t),
            Is.EqualTo(new[] { "activeScenario" }));
        var kinds = schema["definitions"]!["kind"]!["enum"]!.Select(t => (string)t!).ToList();
        Assert.That(kinds.Select(k => StpValidationRulesReader.KindName(Enum.Parse<StpReferenceKind>(k, ignoreCase: true))), Is.EqualTo(kinds));
        Assert.That(Enum.GetValues<StpReferenceKind>().Length, Is.EqualTo(kinds.Count));
    }

    [Test]
    public void Load_RefusesAnotherMajorVersion()
    {
        var rules = JObject.Parse(CallValidationParityTests.ReadEmbeddedRules());
        rules["version"] = "2.0.0";
        var ex = Assert.Throws<FormatException>(() => StpCallValidations.Load(rules.ToString()));
        Assert.That(ex!.Message, Does.Contain("2.0.0").And.Contain("major version 1"));
        rules["version"] = "1.7.0";
        rules["methods"]!["AddCoa"]!["someFutureOptionalKey"] = true;
        Assert.That(() => StpCallValidations.Load(rules.ToString()), Throws.Nothing, "a minor version may add what an evaluator can ignore");
    }

    [Test]
    public void Load_NamesThePathOfAMalformedRule()
    {
        var rules = JObject.Parse(CallValidationParityTests.ReadEmbeddedRules());
        rules["methods"]!["AddTask"]!["references"]![1]!["param"] = 7;
        var ex = Assert.Throws<FormatException>(() => StpCallValidations.Load(rules.ToString()));
        Assert.That(ex!.Message, Does.Contain("methods.AddTask.references[1].param"));
    }

    // ---- Validate: every check, in order -----------------------------------------------------

    [Test]
    public void Validate_RefusesAMethodWithoutRules()
    {
        var v = Rules.Validate("GetPoidObject", new JObject(), null);
        Assert.That(v.IsRefused, Is.True);
        Assert.That(v.FailedCheck!.Check, Is.EqualTo(StpCheck.Dispatchable));
        Assert.That(v.Reason, Does.Contain("cannot be checked"));
    }

    [Test]
    public void Validate_StopsAtTheFirstFailedCheck()
    {
        // no symbol (params) AND no scenario (context): the params check refuses first
        var v = Rules.Validate("UpdateSymbol", JObject.FromObject(new { poid = "idS" }), new StpCallContext { ActiveScenario = false });
        Assert.That(v.Steps.Select(s => s.Check), Is.EqualTo(new[] { StpCheck.Dispatchable, StpCheck.Params }));
        Assert.That(v.FailedCheck!.Check, Is.EqualTo(StpCheck.Params));

        var noScenario = Rules.Validate("AddCoa", JObject.FromObject(new { coa = new { name = "C" } }), new StpCallContext { ActiveScenario = false });
        Assert.That(noScenario.FailedCheck!.Check, Is.EqualTo(StpCheck.Context));
        Assert.That(noScenario.Reason, Does.Contain("no active scenario"));
    }

    [Test]
    public void Validate_PassesAWellFormedCall_AndAsksForConfirmationOfADestructiveOne()
    {
        var lookups = new Dictionary<string, StpPoidLookup>
        {
            ["idA"] = StpPoidLookup.Found(StpLookupSource.Context, StpObjectKind.Symbol, "unit", "FRIENDLY INFANTRY COMPANY A"),
        };
        var v = Rules.Validate("DeleteSymbol", JObject.FromObject(new { poid = "idA" }), new StpCallContext { ActiveScenario = true, Lookups = lookups });
        Assert.That(v.IsRefused, Is.False, v.Reason);
        Assert.That(v.Steps.Select(s => s.Check), Is.EqualTo(new[] { StpCheck.Dispatchable, StpCheck.Params, StpCheck.Context, StpCheck.References }));
        Assert.That(v.RequiresConfirmation, Is.True);
        Assert.That(v.ConfirmationText, Is.EqualTo("Deletes the symbol idA from the scenario."));

        var add = Rules.Validate("AddCoa", JObject.FromObject(new { coa = new { name = "C" } }), new StpCallContext { ActiveScenario = true });
        Assert.That(add.IsRefused || add.RequiresConfirmation, Is.False);
    }

    [Test]
    public void Validate_WithoutLookups_SaysTheReferencesWereNotChecked()
    {
        var v = Rules.Validate("DeleteSymbol", JObject.FromObject(new { poid = "idA" }), new StpCallContext { ActiveScenario = true });
        var references = v.Steps.Last();
        Assert.That(v.IsRefused, Is.False);
        Assert.That(references.Skipped, Is.True);
        Assert.That(references.Detail, Does.StartWith("Not checked:").And.Contain("poid = idA"));

        // ...but a missing target poid needs no lookup to be refused
        var missing = Rules.Validate("DeleteSymbol", new JObject(), new StpCallContext { ActiveScenario = true });
        Assert.That(missing.FailedCheck!.Check, Is.EqualTo(StpCheck.References));
        Assert.That(missing.Reason, Does.Contain("poid is missing"));
    }

    [Test]
    public void FromEngineObject_ReadsAGetPoidObjectAnswer()
    {
        var unit = StpPoidLookup.FromEngineObject(JObject.Parse("{\"fsTYPE\":\"unit\",\"poid\":\"idU\",\"fullDescription\":\"FRIENDLY INFANTRY COMPANY A\"}"));
        Assert.That(unit.IsFound && unit.Kind == StpObjectKind.Symbol && unit.FsType == "unit", Is.True);
        Assert.That(unit.Summary, Is.EqualTo("FRIENDLY INFANTRY COMPANY A"));
        Assert.That(StpPoidLookup.FromEngineObject(JValue.CreateNull()).IsFound, Is.False);
        Assert.That(StpPoidLookup.FromEngineObject(null).IsFound, Is.False);
    }

    // ---- after the call ----------------------------------------------------------------------

    [Test]
    public void KnownIssuesDefault_IsTheContractTheRulesWereCheckedAgainst()
    {
        // At contract 0.9.1 every rule's unverifiable issue is listed in x-stpKnownIssues (sdk-js
        // checks that), so with no list given the absence of the events is unverifiable, not unknown.
        var p = JObject.FromObject(new { toRelationship = new { parent = "idU1", child = "idU2" } });
        var end = StpCallEnd.Inform(TimeSpan.FromMilliseconds(300), TimeSpan.FromSeconds(0.3));
        var report = Rules.Diff("AddTaskOrgRelationship", p, Array.Empty<StpEvent>(), end);
        Assert.That(report.Outcome, Is.EqualTo(StpCallOutcome.Unverifiable));
        Assert.That(report.KnownIssue, Is.EqualTo("STP-1089"));
        Assert.That(report.IsError, Is.False);
        Assert.That(Rules.Diff("AddTaskOrgRelationship", p, Array.Empty<StpEvent>(), end, Array.Empty<string>()).Outcome, Is.EqualTo(StpCallOutcome.Unknown));
    }

    [Test]
    public void Diff_FeedsEveryEvent_AndATimedOutRequestIsUnknown()
    {
        var p = JObject.FromObject(new { poid = "idTO" });
        var timeout = StpCallEnd.FailedRequest("STP did not answer SetDefaultTaskOrg within 7 s.", TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(8));
        var report = Rules.Diff("SetDefaultTaskOrg", p, new[] { new StpEvent("SymbolAdded", JObject.Parse("{\"alternates\":[{\"poid\":\"idX\"}]}"), At) }, timeout);
        Assert.That(report.Outcome, Is.EqualTo(StpCallOutcome.Unknown));
        Assert.That(report.IsError, Is.True);
        Assert.That(report.UnrelatedEvents, Is.EqualTo(1));
        Assert.That(report.AlsoSeen.Single().ToString(), Is.EqualTo("SymbolAdded for idX"));
    }

    [Test]
    public void Watch_RefusesAMethodWithoutRules_RatherThanReportNothing()
    {
        var ex = Assert.Throws<ArgumentException>(() => Rules.Watch("GetPoidObject", new JObject()));
        Assert.That(ex!.Message, Does.Contain("GetPoidObject has no entry"));
    }

    [Test]
    public void AlsoSeen_IsCapped()
    {
        var watch = Rules.Watch("DeleteSymbol", JObject.FromObject(new { poid = "idA" }));
        watch.Observe("SymbolDeleted", JObject.FromObject(new { poid = "idA" }), At);
        for (int i = 0; i < StpCallWatch.MaxAlsoSeen + 5; i++) watch.Observe("TaskDeleted", JObject.FromObject(new { poid = $"idT{i}" }), At);
        var report = watch.Report(StpCallEnd.Inform(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(8)));
        Assert.That(report.Outcome, Is.EqualTo(StpCallOutcome.Done));
        Assert.That(report.UnrelatedEvents, Is.EqualTo(StpCallWatch.MaxAlsoSeen + 5));
        Assert.That(report.AlsoSeen.Count, Is.EqualTo(StpCallWatch.MaxAlsoSeen));
    }
}
