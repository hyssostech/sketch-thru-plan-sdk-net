using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK;
using StpSDK.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace StpSDK.Tests;

/// <summary>
/// STP-1799: every method and event in the pinned public contract (TestData/Contract, see its README)
/// maps to a public SDK member, by the default naming rule or by the documented exceptions in
/// docs/articles/contract-name-mapping.md. A contract that gains a method or event the SDK lacks fails
/// here, naming each one.
/// </summary>
[TestFixture]
public class ContractCoverageTests
{
    // The pinned copy. Change these only together with the vendored file and its README.
    private const string ContractVersion = "0.9.11";
    private const string ContractSha256 = "d58b82fa6918ed1aa6875d270614a2832f408b9b984ecb3a6a92c80078566b57";

    /// <summary>
    /// A contract name and the SDK member that carries it: a public method (or event) of
    /// <see cref="Type"/> named <see cref="Member"/>, with a parameter of <see cref="ParamType"/> when set.
    /// </summary>
    internal sealed record SdkMember(Type Type, string Member, Type ParamType = null)
    {
        public override string ToString() =>
            $"{Type.Name}.{Member}" + (ParamType == null ? "" : $"({ParamType.Name})");
    }

    /// <summary>
    /// The contract methods whose .NET name does not follow the default rule (name + "Async", or the
    /// name itself for a fire-and-forget command). Mirrors the tables in contract-name-mapping.md;
    /// <see cref="MappingDoc_ListsEveryMethodException"/> keeps the two in step.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, SdkMember> MethodExceptions = new Dictionary<string, SdkMember>
    {
        ["GetPoidObject"] = new(typeof(StpRecognizer), "RequestPoidObjectAsync"),
        ["GetScenarioObjectSet"] = new(typeof(StpRecognizer), "GetScenarioObjectSetContentAsync"),
        ["GetScenarioTaskOrgList"] = new(typeof(StpRecognizer), "GetScenarioTaskOrgsAsync"),
        ["SetCurrentCoa"] = new(typeof(StpRecognizer), "SetCoaAsync"),
        ["PullC2SIMInitialization"] = new(typeof(StpRecognizer), "PullC2SIMInitializationContentAsync"),
        ["GetDeletedObjects"] = new(typeof(StpRecognizer), "RequestDeletedStpObjectsAsync"),
        ["GetAllObjects"] = new(typeof(StpRecognizer), "RequestAllStpObjectsAsync"),
        ["ImportPlanData"] = new(typeof(StpRecognizer), "ImportSTPDataAsync", typeof(string)),
        ["LoadNewScenarioFromObjectSet"] = new(typeof(StpRecognizer), "LoadNewScenarioAsync", typeof(ObjectSet)),
        ["SyncScenarioSessionFromObjectSet"] = new(typeof(StpRecognizer), "SyncScenarioSessionAsync", typeof(ObjectSet)),
        ["ImportPlanDataFromObjectSet"] = new(typeof(StpRecognizer), "ImportSTPDataAsync", typeof(ObjectSet)),
        ["ImportTaskOrgFromObjectSet"] = new(typeof(StpRecognizer), "ImportTaskOrgAsync", typeof(ObjectSet)),
        ["ImportCoaFromObjectSet"] = new(typeof(StpRecognizer), "ImportCoaAsync", typeof(ObjectSet)),
        // Answered in the client (x-layer "client"); the engine never receives it.
        ["SearchSymbol"] = new(typeof(SymbolSearch), "Search"),
        // Transport (x-layer "transport"): the request/response wrapper every *Async call goes through.
        ["Request"] = new(typeof(IStpConnector), "SendRequestAsync"),
    };

    /// <summary>
    /// The contract events whose .NET name is not "On" + the name.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, SdkMember> EventExceptions = new Dictionary<string, SdkMember>
    {
        // Transport: the answer to Request. The connector completes the pending *Async call with it.
        ["RequestResponse"] = new(typeof(IStpConnector), "SendRequestAsync"),
    };

    /// <summary>
    /// SDK-only wire methods: calls the SDK sends that the contract does not list, documented in the
    /// mapping doc's "Not in the contract" table. Each must still exist in the SDK and still be absent
    /// from the contract; one the contract gains must move to the mapped methods.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, SdkMember> SdkOnlyWireMethods = new Dictionary<string, SdkMember>
    {
        ["GetActiveCoaList"] = new(typeof(StpRecognizer), "RequestActiveCoasAsync"),
        ["GetScenarioCoaList"] = new(typeof(StpRecognizer), "GetScenarioCoasAsync"),
        ["GetCoaObjects"] = new(typeof(StpRecognizer), "GetCoaObjectsAsync"),
        ["GetCurrentCoaPoid"] = new(typeof(StpRecognizer), "GetCurrentCoaPoidAsync"),
        ["CreateCoa"] = new(typeof(StpRecognizer), "CreateCoaAsync"),
        ["CreateDefaultCoa"] = new(typeof(StpRecognizer), "CreateDefaultCoaAsync"),
        ["SwitchTaskConfirmation"] = new(typeof(StpRecognizer), "SwitchTaskConfirmationAsync"),
    };

    /// <summary>
    /// SDK conveniences that send contract calls under a name the contract does not have.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, SdkMember> SdkConveniences = new Dictionary<string, SdkMember>
    {
        ["SwitchRoleAndCoa"] = new(typeof(StpRecognizer), "SwitchRoleAndCoaAsync"),
    };

    private static string DataPath(string file) =>
        Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "Contract", file);

    private static JObject _contract;

    private static JObject Contract => _contract ??= JObject.Parse(File.ReadAllText(DataPath("sketch-thru-plan-api.json")));

    private static IEnumerable<string> ContractMethods() => Contract["methods"]!.Select(m => (string)m["name"]);

    private static IEnumerable<string> ContractEvents() => Contract["x-stpEvents"]!.Select(e => (string)e["name"]);

    private static bool Exists(SdkMember m)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        if (m.Type.GetEvent(m.Member, flags) != null) return m.ParamType == null;
        return m.Type.GetMethods(flags).Any(mi => mi.Name == m.Member
            && (m.ParamType == null || mi.GetParameters().Any(p => p.ParameterType == m.ParamType)));
    }

    /// <summary>The SDK member a contract method maps to, or the candidates tried when none exists.</summary>
    private static (SdkMember Found, string Tried) ResolveMethod(string name)
    {
        if (MethodExceptions.TryGetValue(name, out var mapped))
            return (Exists(mapped) ? mapped : null, mapped.ToString());
        var candidates = new[] { new SdkMember(typeof(StpRecognizer), name + "Async"), new SdkMember(typeof(StpRecognizer), name) };
        return (candidates.FirstOrDefault(Exists), string.Join(" or ", candidates.Select(c => c.ToString())));
    }

    private static (SdkMember Found, string Tried) ResolveEvent(string name)
    {
        if (EventExceptions.TryGetValue(name, out var mapped))
            return (Exists(mapped) ? mapped : null, mapped.ToString());
        var candidate = new SdkMember(typeof(StpRecognizer), "On" + name);
        return (Exists(candidate) ? candidate : null, candidate.ToString());
    }

    [Test]
    public void VendoredContract_IsThePinnedCopy()
    {
        Assert.That((string)Contract["info"]?["version"], Is.EqualTo(ContractVersion));

        // Hash with LF line endings, so a checkout that converts them still matches.
        string text = File.ReadAllText(DataPath("sketch-thru-plan-api.json")).Replace("\r\n", "\n");
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
        Assert.That(hash, Is.EqualTo(ContractSha256),
            "The vendored contract changed. Bump it deliberately: update TestData/Contract/README.md and the constants here.");

        string readme = File.ReadAllText(DataPath("README.md"));
        Assert.That(readme, Does.Contain(ContractVersion).And.Contain(ContractSha256));
    }

    [Test]
    public void EveryContractMethod_MapsToAnSdkMember()
    {
        var missing = ContractMethods()
            .Select(name => (name, r: ResolveMethod(name)))
            .Where(x => x.r.Found == null)
            .Select(x => $"{x.name} (expected {x.r.Tried})")
            .ToList();

        Assert.That(missing, Is.Empty,
            $"Contract {ContractVersion} methods with no SDK member:\n  " + string.Join("\n  ", missing)
            + "\nAdd the member, or record the .NET name in docs/articles/contract-name-mapping.md and MethodExceptions.");
    }

    [Test]
    public void EveryContractEvent_MapsToAnSdkEvent()
    {
        var missing = ContractEvents()
            .Select(name => (name, r: ResolveEvent(name)))
            .Where(x => x.r.Found == null)
            .Select(x => $"{x.name} (expected {x.r.Tried})")
            .ToList();

        Assert.That(missing, Is.Empty,
            $"Contract {ContractVersion} events with no SDK event:\n  " + string.Join("\n  ", missing)
            + "\nAdd the event, or record the .NET name in docs/articles/contract-name-mapping.md and EventExceptions.");
    }

    [Test]
    public void SearchSymbol_IsClientSide_AndMapsToSymbolSearch()
    {
        var method = Contract["methods"]!.Single(m => (string)m["name"] == "SearchSymbol");
        Assert.That((string)method["x-layer"], Is.EqualTo("client"));
        Assert.That(ResolveMethod("SearchSymbol").Found, Is.EqualTo(new SdkMember(typeof(SymbolSearch), "Search")));
    }

    [Test]
    public void MappingTables_NameOnlyContractEntries()
    {
        var methods = ContractMethods().ToHashSet();
        var events = ContractEvents().ToHashSet();

        Assert.That(MethodExceptions.Keys.Where(k => !methods.Contains(k)), Is.Empty, "MethodExceptions names a method the contract lacks");
        Assert.That(EventExceptions.Keys.Where(k => !events.Contains(k)), Is.Empty, "EventExceptions names an event the contract lacks");
    }

    [Test]
    public void SdkOnlyExtras_StillExist_AndAreStillOutsideTheContract()
    {
        var methods = ContractMethods().ToHashSet();
        var extras = SdkOnlyWireMethods.Concat(SdkConveniences).ToList();

        var nowInContract = extras.Where(e => methods.Contains(e.Key)).Select(e => e.Key).ToList();
        Assert.That(nowInContract, Is.Empty,
            "The contract now lists these SDK-only calls; map them as contract methods and align their params:\n  "
            + string.Join("\n  ", nowInContract));

        var gone = extras.Where(e => !Exists(e.Value)).Select(e => $"{e.Key} ({e.Value})").ToList();
        Assert.That(gone, Is.Empty, "Documented SDK extras with no SDK member:\n  " + string.Join("\n  ", gone));
    }

    [Test]
    public void MappingDoc_ListsEveryMethodException_AndEveryExtra()
    {
        string doc = File.ReadAllText(DataPath("contract-name-mapping.md"));

        var undocumented = MethodExceptions.Concat(EventExceptions).Concat(SdkOnlyWireMethods).Concat(SdkConveniences)
            .Where(e => !doc.Contains($"`{e.Key}`") || !doc.Contains(e.Value.Member))
            .Select(e => $"{e.Key} -> {e.Value}")
            .ToList();

        Assert.That(undocumented, Is.Empty,
            "docs/articles/contract-name-mapping.md does not name these mappings:\n  " + string.Join("\n  ", undocumented));
    }

    /// <summary>A name with no SDK member resolves to nothing and reports the member it expected (invented names).</summary>
    [Test]
    public void Resolve_ReportsTheMissingMember()
    {
        Assert.That(ResolveMethod("NoSuchContractCall").Found, Is.Null);
        Assert.That(ResolveMethod("NoSuchContractCall").Tried, Does.Contain("StpRecognizer.NoSuchContractCallAsync"));
        Assert.That(ResolveEvent("NoSuchContractEvent").Found, Is.Null);
        Assert.That(ResolveEvent("NoSuchContractEvent").Tried, Is.EqualTo("StpRecognizer.OnNoSuchContractEvent"));
    }
}
