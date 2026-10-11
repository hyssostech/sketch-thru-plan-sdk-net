using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK.Validation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StpSDK.Tests;

/// <summary>
/// STP-1064 acceptance: "the same rule file drives the TypeScript evaluator and the sdk-net
/// evaluator", and "parity tests run the same cases through both". The cases are
/// TestData/CallValidations/call-validation-cases.json, one per assertion group of stp-mcp's
/// packages/live/test/validate.test.ts (named after it), run here through
/// <see cref="StpCallValidations.Default"/>, the rule file the stp-mcp server pins too.
/// </summary>
[TestFixture]
public class CallValidationParityTests
{
    private static readonly StpCallValidations Rules = StpCallValidations.Default;

    private static JObject Fixture()
    {
        // Dates stay strings, as they are in the fixture (and on the wire).
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "CallValidations", "call-validation-cases.json");
        using var reader = new JsonTextReader(new StringReader(File.ReadAllText(path))) { DateParseHandling = DateParseHandling.None };
        return JObject.Load(reader);
    }

    public static IEnumerable<TestCaseData> Cases() =>
        ((JArray)Fixture()["cases"]!).Cast<JObject>().Select(c => new TestCaseData(c).SetName((string)c["name"]!));

    [Test]
    public void TheCasesUseTheRuleFileThisSdkPins()
    {
        var rules = (JObject)Fixture()["rules"]!;
        Assert.That((string?)rules["ref"], Is.EqualTo(StpCallValidations.Pin.Ref));
        Assert.That((string?)rules["version"], Is.EqualTo(Rules.Version));
    }

    [Test]
    public void EveryCaseIsUniquelyNamed_AndEverySkipSaysWhy()
    {
        var cases = ((JArray)Fixture()["cases"]!).Cast<JObject>().ToList();
        Assert.That(cases.Select(c => (string?)c["name"]), Is.Unique);
        Assert.That(cases.Count, Is.GreaterThanOrEqualTo(60));
        foreach (var c in cases.Where(c => c["notInRuleFile"] != null))
        {
            Assert.That((string?)c["notInRuleFile"], Is.Not.Empty, (string?)c["name"]);
        }
    }

    [TestCaseSource(nameof(Cases))]
    public void Case(JObject c)
    {
        if (c["notInRuleFile"] != null)
        {
            Assert.Pass("not in the rule file: " + (string?)c["notInRuleFile"]);
        }
        var expect = (JObject)c["expect"]!;
        switch ((string?)c["op"])
        {
            case "loadRules": LoadRules(c, expect); break;
            case "getPath": GetPath(c, expect); break;
            case "expandPath": ExpandPath(c, expect); break;
            case "sameValue":
                Assert.That(StpJsonPath.Same(c["a"], c["b"]), Is.EqualTo((bool)expect["same"]!));
                break;
            case "eventPoid":
                Assert.That(StpCallWatch.EventPoid(c["params"]), Is.EqualTo((string?)expect["poid"]));
                break;
            case "kindOfFsType": KindOfFsType(c, expect); break;
            case "required": AssertStep(Rules.CheckRequired(Method(c), Params(c)), expect); break;
            case "preconditions": AssertStep(Rules.CheckPreconditions(Method(c), Context((JObject)c["facts"]!)), expect); break;
            case "referenceTargets": ReferenceTargets(c, expect); break;
            case "references":
                AssertStep(Rules.CheckReferences(Method(c), Rules.ReferenceTargets(Method(c), Params(c)), Lookups((JObject)c["lookups"]!)), expect);
                break;
            case "confirmation":
                string text = Rules.ConfirmationText(Method(c), Params(c));
                if (expect["text"] != null) Assert.That(text, Is.EqualTo((string?)expect["text"]));
                AssertMatches(text, expect["textMatches"]);
                break;
            case "expectedItems":
                var labels = Rules.Watch(Method(c), Params(c)).Items.Select(i => i.Label.Split(':')[0]);
                Assert.That(labels, Is.EqualTo(Strings(expect["labelsBeforeColon"])));
                break;
            case "v10": V10(c, expect); break;
            default: Assert.Fail($"unknown op {(string?)c["op"]}"); break;
        }
    }

    private static void LoadRules(JObject c, JObject expect)
    {
        var rules = JObject.Parse(ReadEmbeddedRules());
        foreach (var m in (c["mutate"] as JArray ?? new JArray()).Cast<JObject>())
        {
            var path = Strings(m["path"]);
            JToken target = rules;
            foreach (string seg in path.Take(path.Count - 1)) target = target[seg]!;
            target[path[path.Count - 1]] = m["value"]!.DeepClone();
        }
        if ((bool)expect["valid"]!)
        {
            Assert.That(() => StpCallValidations.Load(rules.ToString()), Throws.Nothing);
        }
        else
        {
            var ex = Assert.Throws<FormatException>(() => StpCallValidations.Load(rules.ToString()));
            TestContext.Out.WriteLine(ex!.Message);
        }
    }

    private static void GetPath(JObject c, JObject expect)
    {
        var value = StpJsonPath.Get(c["value"], (string)c["path"]!);
        if (expect["absent"] != null) Assert.That(StpJsonPath.IsAbsent(value), Is.True);
        else Assert.That(JToken.DeepEquals(value, expect["value"]), Is.True, $"got {value}");
    }

    private static void ExpandPath(JObject c, JObject expect)
    {
        var found = StpJsonPath.Expand(c["value"], (string)c["path"]!);
        if (expect["found"] is JArray all)
        {
            Assert.That(found.Select(f => f.Key), Is.EqualTo(all.Select(a => (string?)a["path"])));
            Assert.That(found.Select(f => f.Value.ToString()), Is.EqualTo(all.Select(a => a["value"]!.ToString())));
        }
        if (expect["paths"] != null) Assert.That(found.Select(f => f.Key), Is.EqualTo(Strings(expect["paths"])));
    }

    private static void KindOfFsType(JObject c, JObject expect)
    {
        var kind = StpPoidLookup.KindOfFsType((string?)c["fsTYPE"], out string? inferred);
        Assert.That(kind, Is.EqualTo(ObjectKind((string)expect["kind"]!)));
        if (expect.ContainsKey("inferred")) Assert.That(inferred, Is.EqualTo((string?)expect["inferred"]));
        if (expect["inferredMatches"] != null) AssertMatches(inferred ?? "", expect["inferredMatches"]);
    }

    private static void ReferenceTargets(JObject c, JObject expect)
    {
        var refs = Rules.ReferenceTargets(Method(c), Params(c));
        if (expect["targets"] != null)
        {
            var targets = refs.Targets.Select(t => $"{t.Param}={t.Poid}:{string.Join("|", t.Kinds.Select(KindName))}");
            Assert.That(targets, Is.EqualTo(Strings(expect["targets"])));
        }
        if (expect["missing"] != null) Assert.That(refs.Missing, Is.EqualTo(Strings(expect["missing"])));
        if (expect["malformed"] != null) Assert.That(refs.Malformed, Is.EqualTo(Strings(expect["malformed"])));
    }

    private static void V10(JObject c, JObject expect)
    {
        var watch = Rules.Watch(Method(c), Params(c), Strings(c["knownIssues"]));
        if (expect["labels"] != null) Assert.That(watch.Items.Select(i => i.Label), Is.EqualTo(Strings(expect["labels"])));
        foreach (var e in ((JArray)c["events"]!).Cast<JObject>())
        {
            bool settled = watch.Observe((string)e["name"]!, e["params"], EventTime(c));
            if (e["settles"] != null) Assert.That(settled, Is.EqualTo((bool)e["settles"]!), $"settled after {(string?)e["name"]}");
        }
        if (c["end"] is not JObject end) return;

        var r = watch.Report(End(end));
        TestContext.Out.WriteLine($"{r.Outcome}: {r.Summary}");
        if (expect["outcome"] != null) Assert.That(OutcomeName(r.Outcome), Is.EqualTo((string?)expect["outcome"]));
        if (expect["summary"] != null) Assert.That(r.Summary, Is.EqualTo((string?)expect["summary"]));
        AssertMatches(r.Summary, expect["summaryMatches"]);
        foreach (string pattern in Strings(expect["summaryNotMatches"]))
        {
            Assert.That(Regex.IsMatch(r.Summary, pattern), Is.False, $"/{pattern}/ must not match: {r.Summary}");
        }
        if (expect["missing"] != null) Assert.That(r.Missing, Is.EqualTo(Strings(expect["missing"])));
        if (expect["expected"] != null) Assert.That(r.Expected, Is.EqualTo(Strings(expect["expected"])));
        if (expect["observedAs"] != null) Assert.That(r.Observed.Select(o => RoleName(o.As)), Is.EqualTo(Strings(expect["observedAs"])));
        if (expect["unexpected"] is JArray unexpected)
        {
            Assert.That(r.Unexpected.Select(u => $"{u.Event}|{u.Poid}"), Is.EqualTo(unexpected.Select(u => $"{(string?)u["event"]}|{(string?)u["poid"]}")));
        }
        if (expect.ContainsKey("refusal")) Assert.That(r.Refusal, Is.EqualTo((string?)expect["refusal"]));
        if (expect.ContainsKey("knownIssue")) Assert.That(r.KnownIssue, Is.EqualTo((string?)expect["knownIssue"]));
        if (expect["unrelatedEvents"] != null) Assert.That(r.UnrelatedEvents, Is.EqualTo((int)expect["unrelatedEvents"]!));
        if (expect["observed"] is JArray observed)
        {
            var got = r.Observed.Select(o => new JObject
            {
                ["event"] = o.Event,
                ["poid"] = o.Poid,
                ["at"] = o.At.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
                ["as"] = RoleName(o.As),
                ["fulfils"] = o.Fulfils,
            });
            Assert.That(JToken.DeepEquals(new JArray(got), observed), Is.True, new JArray(got).ToString());
        }
    }

    // ---- fixture vocabulary -> the C# API --------------------------------------------------------

    private static string Method(JObject c) => (string)c["method"]!;

    private static JObject Params(JObject c) => c["params"] as JObject ?? new JObject();

    private static DateTimeOffset EventTime(JObject c) =>
        DateTimeOffset.Parse((string)Fixture()["eventTime"]!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    private static List<string> Strings(JToken? token) =>
        token is JArray a ? a.Select(t => (string)t!).ToList() : new List<string>();

    private static void AssertMatches(string text, JToken? patterns)
    {
        foreach (string pattern in Strings(patterns))
        {
            Assert.That(Regex.IsMatch(text, pattern), Is.True, $"/{pattern}/ does not match: {text}");
        }
    }

    private static void AssertStep(StpCheckStep step, JObject expect)
    {
        TestContext.Out.WriteLine(step);
        if (expect["check"] != null) Assert.That(step.Check.ToString(), Is.EqualTo((string)expect["check"]!).IgnoreCase);
        if (expect["passed"] != null) Assert.That(step.Passed, Is.EqualTo((bool)expect["passed"]!), step.Detail);
        if (expect["detail"] != null) Assert.That(step.Detail, Is.EqualTo((string?)expect["detail"]));
        AssertMatches(step.Detail, expect["detailMatches"]);
    }

    private static StpCallContext Context(JObject facts) => new()
    {
        ActiveScenario = facts["activeScenario"]?.Type == JTokenType.Boolean ? (bool)facts["activeScenario"]! : null,
        Problem = (string?)facts["problem"],
    };

    private static Dictionary<string, StpPoidLookup> Lookups(JObject lookups) =>
        lookups.Properties().ToDictionary(p => p.Name, p => Lookup((JObject)p.Value));

    private static StpPoidLookup Lookup(JObject l)
    {
        var source = (string)l["source"]! == "context" ? StpLookupSource.Context : StpLookupSource.Engine;
        if ((bool)l["found"]!)
        {
            return StpPoidLookup.Found(source, ObjectKind((string)l["kind"]!), (string?)l["fsTYPE"], (string?)l["summary"], (string?)l["inferred"]);
        }
        if (l["error"] != null) return StpPoidLookup.Failed(source, (string)l["error"]!);
        var deleted = l["deleted"] as JObject;
        return StpPoidLookup.NotFound(source, (string?)deleted?["event"],
            deleted == null ? null : DateTimeOffset.Parse((string)deleted["at"]!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal));
    }

    private static StpCallEnd End(JObject end)
    {
        var waited = TimeSpan.FromMilliseconds((double)end["waitedMs"]!);
        var timeout = TimeSpan.FromSeconds((double)end["timeoutS"]!);
        if ((string)end["wire"]! == "inform") return StpCallEnd.Inform(waited, timeout);
        if (end["refused"] != null) return StpCallEnd.RefusedRequest((string)end["refused"]!, waited, timeout);
        if (end["failure"] != null) return StpCallEnd.FailedRequest((string)end["failure"]!, waited, timeout);
        Assert.That((bool?)end["answered"], Is.True, "a request ends answered, refused or failed");
        return StpCallEnd.AnsweredRequest(waited, timeout);
    }

    private static StpObjectKind ObjectKind(string name) => (StpObjectKind)Enum.Parse(typeof(StpObjectKind), name, ignoreCase: true);

    private static string KindName(StpReferenceKind kind) => char.ToLowerInvariant(kind.ToString()[0]) + kind.ToString().Substring(1);

    internal static string OutcomeName(StpCallOutcome outcome) => outcome == StpCallOutcome.NotDone ? "not_done" : outcome.ToString().ToLowerInvariant();

    private static string RoleName(StpEventRole role) => role == StpEventRole.FollowOn ? "follow-on" : role.ToString().ToLowerInvariant();

    internal static string ReadEmbeddedRules()
    {
        using var stream = typeof(StpCallValidations).Assembly.GetManifestResourceStream("StpSDK.Validation.sketch-thru-plan-validations.json")!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
