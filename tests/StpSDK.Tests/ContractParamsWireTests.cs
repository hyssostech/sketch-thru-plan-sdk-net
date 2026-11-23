using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace StpSDK.Tests;

/// <summary>
/// A stub that also offers the 5.12 connector capabilities (<see cref="IStpExtendedConnector"/>), and
/// records what the recognizer asked of them.
/// </summary>
internal class ExtendedStubConnector : IStpExtendedConnector
{
    public event StpMessageReceivedDelegate OnMessage;
    public event StpConnectionErrorDelegate OnConnectionError;

    public ILogger Logger => null;
    public string Name => "ext-stub";
    public string BaseName => "ext-stub";
    public bool Connected { get; set; } = true;

    public List<string> SentMessages { get; } = new();
    public string NextResponse { get; set; } = "\"ok\"";
    public Exception NextError { get; set; }

    public string LastUser { get; private set; } = "unset";
    public bool UsedBaseRegister { get; private set; }
    public int? LastTimeoutMs { get; private set; }
    public int? LastEngineTimeoutSeconds { get; private set; }
    public int? LastClientTimeoutMs { get; private set; }

    public Task<bool> ConnectAsync(string url, int secondsToRetry = 0, CancellationToken ct = default) => Task.FromResult(true);

    public Task<string> RegisterAsync(string serviceName, List<string> solvables, string machineId = null, string sessionId = null, CancellationToken ct = default)
    {
        UsedBaseRegister = true;
        return Task.FromResult("session");
    }

    public Task<string> RegisterAsync(string serviceName, List<string> solvables, string machineId, string sessionId, string user, CancellationToken ct = default)
    {
        LastUser = user;
        return Task.FromResult("session");
    }

    public void Disconnect() => Connected = false;

    public void Send(string jsonMessage) => SentMessages.Add(jsonMessage);

    public Task<string> SendRequestAsync(string jsonMessage, int cookie, int timeoutMs = 30000, CancellationToken ct = default)
    {
        LastTimeoutMs = timeoutMs;
        return Answer(jsonMessage);
    }

    public Task<string> SendRequestAsync(string jsonMessage, int cookie, int engineTimeoutSeconds, int clientTimeoutMs, CancellationToken ct = default)
    {
        LastEngineTimeoutSeconds = engineTimeoutSeconds;
        LastClientTimeoutMs = clientTimeoutMs;
        return Answer(jsonMessage);
    }

    private Task<string> Answer(string jsonMessage)
    {
        SentMessages.Add(jsonMessage);
        return NextError != null ? Task.FromException<string>(NextError) : Task.FromResult(NextResponse);
    }

    public void SimulateMessage(string json) => OnMessage?.Invoke(json);

    public void Dispose() { }
}

/// <summary>
/// STP-1798: the existing calls' params and results as the public contract has them
/// (sketch-thru-plan-api.json 0.9.11, sdk-js release/5.12 b7a490d). Method names, the exact param-name
/// set and the shapes are the contract's, so a rename on either side fails here.
/// </summary>
[TestFixture]
public class ContractParamsWireTests
{
    private ExtendedStubConnector _connector;
    private StpRecognizer _recognizer;

    [SetUp]
    public void Setup()
    {
        _connector = new ExtendedStubConnector();
        _recognizer = new StpRecognizer(_connector);
    }

    [TearDown]
    public void TearDown()
    {
        _recognizer.Dispose();
        _connector?.Dispose();
    }

    private JObject LastSent() => JObject.Parse(_connector.SentMessages[^1]);

    private static string[] ParamNames(JObject sent) =>
        ((JObject)sent["params"]).Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();

    private static string[] Sorted(params string[] names) => names.OrderBy(n => n, StringComparer.Ordinal).ToArray();

    #region 1. SetCurrentCoa

    [Test]
    public async Task SetCoa_SendsSetCurrentCoa_WithPoidOnly()
    {
        await _recognizer.SetCoaAsync("idCOA1");

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("SetCurrentCoa"));
        Assert.That(ParamNames(sent), Is.EqualTo(new[] { "poid" }));
        Assert.That(sent["params"]["poid"].Type, Is.EqualTo(JTokenType.String));
        Assert.That(sent["params"]["poid"].ToString(), Is.EqualTo("idCOA1"));
    }

    #endregion

    #region 2. nBestIndex and createIfNone

    [Test]
    public void ChooseAlternate_SendsPoidAndNBestIndex()
    {
        _recognizer.ChooseAlternate("idSYM1", 2);

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("ChooseAlternate"));
        Assert.That(ParamNames(sent), Is.EqualTo(Sorted("poid", "nBestIndex")));
        Assert.That(sent["params"]["nBestIndex"].Type, Is.EqualTo(JTokenType.Integer));
        Assert.That((int)sent["params"]["nBestIndex"], Is.EqualTo(2));
    }

    [Test]
    public void ConfirmTask_AlreadySendsTheContractNames()
    {
        _recognizer.ConfirmTask("idTASK1", 1, false);

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("ConfirmTask"));
        Assert.That(ParamNames(sent), Is.EqualTo(Sorted("poid", "nBestIndex", "isConfirmed")));
        Assert.That((int)sent["params"]["nBestIndex"], Is.EqualTo(1));
        Assert.That((bool)sent["params"]["isConfirmed"], Is.False);
    }

    [Test]
    public async Task SetRole_WithoutCreateIfNone_SendsRoleOnly()
    {
        await _recognizer.SetRoleAsync(StpRole.S3);

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("SetRole"));
        Assert.That(ParamNames(sent), Is.EqualTo(new[] { "role" }));
        Assert.That(sent["params"]["role"].ToString(), Is.EqualTo("S3"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SetRole_WithCreateIfNone_SendsItAsABoolean(bool createIfNone)
    {
        await _recognizer.SetRoleAsync(StpRole.FSO, createIfNone);

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("SetRole"));
        Assert.That(ParamNames(sent), Is.EqualTo(Sorted("role", "createIfNone")));
        Assert.That(sent["params"]["createIfNone"].Type, Is.EqualTo(JTokenType.Boolean));
        Assert.That((bool)sent["params"]["createIfNone"], Is.EqualTo(createIfNone));
    }

    #endregion

    #region 3. Register user

    [Test]
    public void RegisterMessage_WithUser_HasTheContractParamSet()
    {
        var msg = JObject.Parse(StpJsonRpcConnector.BuildRegisterMessage("App", new List<string> { "SymbolAdded" }, "M1", "S1", "Pat"));

        Assert.That(msg["method"]?.ToString(), Is.EqualTo("Register"));
        Assert.That(ParamNames(msg), Is.EqualTo(Sorted("serviceName", "language", "solvables", "machineId", "sessionId", "user")));
        Assert.That(msg["params"]["user"].Type, Is.EqualTo(JTokenType.String));
        Assert.That(msg["params"]["user"].ToString(), Is.EqualTo("Pat"));
    }

    [Test]
    public void RegisterMessage_WithoutUser_OmitsIt()
    {
        foreach (var user in new[] { null, "" })
        {
            var msg = JObject.Parse(StpJsonRpcConnector.BuildRegisterMessage("App", new List<string>(), "M1", "S1", user));

            Assert.That(ParamNames(msg), Is.EqualTo(Sorted("serviceName", "language", "solvables", "machineId", "sessionId")), user ?? "null");
        }
    }

    [Test]
    public async Task Recognizer_User_ReachesEveryRegistration()
    {
        _recognizer.User = "Pat";

        await _recognizer.RegisterAsync("App");
        Assert.That(_connector.LastUser, Is.EqualTo("Pat"));

        _recognizer.User = "Sam";
        await _recognizer.RefreshSubscriptionsAsync();
        Assert.That(_connector.LastUser, Is.EqualTo("Sam"));

        _recognizer.User = "Lee";
        await _recognizer.RegisterEventsAsync("App", new List<string> { "SymbolAdded" });
        Assert.That(_connector.LastUser, Is.EqualTo("Lee"));
        Assert.That(_connector.UsedBaseRegister, Is.False);
    }

    [Test]
    public async Task Recognizer_NoUser_UsesThePlainRegister()
    {
        await _recognizer.RegisterAsync("App");

        Assert.That(_connector.UsedBaseRegister, Is.True);
        Assert.That(_connector.LastUser, Is.EqualTo("unset"));
    }

    [Test]
    public void Recognizer_User_WithAPlainConnector_Throws()
    {
        using var plain = new MetadataStubConnector();
        using var recognizer = new StpRecognizer(plain) { User = "Pat" };

        Assert.ThrowsAsync<NotSupportedException>(async () => await recognizer.RegisterAsync("App"));
    }

    #endregion

    #region 4. PullC2SIMInitialization returns a string

    // A real-shaped C2SIM Initialization answer: multi-line, attributes, a timestamp with an offset.
    private const string InitXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
        "<MessageBody xmlns=\"http://www.sisostds.org/schemas/C2SIM/1.1\">\n" +
        "  <C2SIMInitializationBody>\n" +
        "    <InitializationDataFile><SystemName>STP</SystemName><StartTime>2026-10-19T01:00:00.123+02:00</StartTime></InitializationDataFile>\n" +
        "    <ObjectDefinitions><Entity><ActorEntity><CurrentState/><Name>1st Bn</Name><UUID>0f8fad5b-d9cb-469f-a165-70867728950e</UUID></ActorEntity></Entity></ObjectDefinitions>\n" +
        "  </C2SIMInitializationBody>\n" +
        "</MessageBody>";

    [Test]
    public async Task PullContent_ReturnsTheXmlVerbatim()
    {
        _connector.NextResponse = InitXml;

        string xml = await _recognizer.PullC2SIMInitializationContentAsync();

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("PullC2SIMInitialization"));
        Assert.That(ParamNames(sent), Is.Empty, "no options: the param is omitted");
        Assert.That(xml, Is.EqualTo(InitXml));
    }

    [Test]
    public async Task PullContent_EmptyAnswer_IsNull()
    {
        _connector.NextResponse = "";

        Assert.That(await _recognizer.PullC2SIMInitializationContentAsync(), Is.Null);
    }

    [Test]
    public async Task PullTuple_ContractStringAnswer_IsTheContent_NotAParseError()
    {
        _connector.NextResponse = InitXml;

#pragma warning disable CS0618 // exercises the obsolete member on purpose
        var (content, serverStatus) = await _recognizer.PullC2SIMInitializationAsync();
#pragma warning restore CS0618

        Assert.That(content, Is.EqualTo(InitXml));
        Assert.That(serverStatus, Is.Null);
    }

    [Test]
    public async Task PullTuple_BraceStartedNonJson_IsTheContent()
    {
        _connector.NextResponse = "{not json";

#pragma warning disable CS0618 // exercises the obsolete member on purpose
        var (content, serverStatus) = await _recognizer.PullC2SIMInitializationAsync();
#pragma warning restore CS0618

        Assert.That(content, Is.EqualTo("{not json"));
        Assert.That(serverStatus, Is.Null);
    }

    #endregion

    #region 5. Typed C2SIM options

    // The contract's key set for GetC2SIMContent / PullC2SIMInitialization / PushC2SIMContent options,
    // in its order and camelCase spelling.
    private static readonly string[] ContractC2SIMKeys =
    {
        "restUrl", "restPassword", "stompUrl", "serverProtocol", "systemName", "resetBeforeInitialize",
        "runAfterInitialize", "fullTO", "refuseUnconfirmedPlaceholders", "schemaVersion",
        "addOrphanTgToInitialization", "placeAllTgInInitialization", "includeMapGraphicId",
        "entityNameCharLimit", "includeDescriptionInName", "rulesOfEngagement", "updateUnitPositions",
        "exportFileDir", "validationSchema", "startDate", "phaseDuration", "fromSenderUUID",
        "toReceiverUUID", "friendFstName", "friendFstUUID", "hostileFstName", "hostileFstUUID",
        "neutralFstName", "neutralFstUUID", "unknownFstName", "unknownFstUUID",
    };

    private static readonly Dictionary<string, JTokenType> IntegerKeys = new()
    {
        ["entityNameCharLimit"] = JTokenType.Integer,
        ["phaseDuration"] = JTokenType.Integer,
    };

    private static readonly HashSet<string> BooleanKeys = new()
    {
        "resetBeforeInitialize", "runAfterInitialize", "fullTO", "refuseUnconfirmedPlaceholders",
        "addOrphanTgToInitialization", "placeAllTgInInitialization", "includeMapGraphicId",
        "includeDescriptionInName", "updateUnitPositions",
    };

    private static StpC2SIMOptions AllSet()
    {
        var o = new StpC2SIMOptions();
        foreach (var p in typeof(StpC2SIMOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
            object v = t == typeof(bool) ? true : t == typeof(int) ? 7 : (object)("v-" + p.Name);
            p.SetValue(o, v);
        }
        return o;
    }

    [Test]
    public void Options_KeySet_IsTheContracts()
    {
        Assert.That(AllSet().Keys, Is.EquivalentTo(ContractC2SIMKeys));
    }

    [Test]
    public async Task GetC2SIM_TypedOptions_SendCamelCaseKeysAndTypes()
    {
        await _recognizer.GetC2SIMContentAsync("plan", C2CIMDataType.Order, "friend", new List<string> { "idCOA1" }, AllSet(), CancellationToken.None);

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("GetC2SIMContent"));
        Assert.That(ParamNames(sent), Is.EqualTo(Sorted("name", "dataType", "affiliation", "coaPoids", "options")));
        var options = (JObject)sent["params"]["options"];
        Assert.That(options.Properties().Select(p => p.Name), Is.EquivalentTo(ContractC2SIMKeys));
        foreach (var p in options.Properties())
        {
            var expected = BooleanKeys.Contains(p.Name) ? JTokenType.Boolean
                : IntegerKeys.ContainsKey(p.Name) ? JTokenType.Integer
                : JTokenType.String;
            Assert.That(p.Value.Type, Is.EqualTo(expected), p.Name);
        }
    }

    [Test]
    public async Task GetC2SIM_RefuseUnconfirmedPlaceholders_AloneIsTheOnlyKey()
    {
        var options = new StpC2SIMOptions { RefuseUnconfirmedPlaceholders = true };

        await _recognizer.GetC2SIMContentAsync("plan", C2CIMDataType.Order, null, null, options, CancellationToken.None);

        var sent = LastSent();
        Assert.That(((JObject)sent["params"]["options"]).Properties().Select(p => p.Name), Is.EqualTo(new[] { "refuseUnconfirmedPlaceholders" }));
        Assert.That((bool)sent["params"]["options"]["refuseUnconfirmedPlaceholders"], Is.True);
    }

    [Test]
    public async Task PushC2SIM_TypedOptions_OmitUnsetKeys()
    {
        var options = new StpC2SIMOptions { RestUrl = "http://10.2.10.30:8080/C2SIMServer", ResetBeforeInitialize = false, EntityNameCharLimit = 40 };

        await _recognizer.PushC2SIMContentAsync("<x/>", C2CIMDataType.Initialization, options, CancellationToken.None);

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo("PushC2SIMContent"));
        Assert.That(ParamNames(sent), Is.EqualTo(Sorted("content", "dataType", "options")));
        var o = (JObject)sent["params"]["options"];
        Assert.That(o.Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "restUrl", "resetBeforeInitialize", "entityNameCharLimit" }));
        Assert.That(o["resetBeforeInitialize"].Type, Is.EqualTo(JTokenType.Boolean));
        Assert.That((bool)o["resetBeforeInitialize"], Is.False, "false is set, so it is sent");
        Assert.That(o["entityNameCharLimit"].Type, Is.EqualTo(JTokenType.Integer));
    }

    [Test]
    public async Task PullC2SIM_TypedOptions_AreSentUnderOptions()
    {
        _connector.NextResponse = InitXml;
        var options = new StpC2SIMOptions { RestUrl = "http://h:8080/C2SIMServer", RestPassword = "pw" };

        await _recognizer.PullC2SIMInitializationContentAsync(options);

        var sent = LastSent();
        Assert.That(ParamNames(sent), Is.EqualTo(new[] { "options" }));
        Assert.That(((JObject)sent["params"]["options"]).Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "restUrl", "restPassword" }));
    }

    public record C2SIMCase(string Method, Func<StpRecognizer, Dictionary<string, object>, Task> Call)
    {
        public override string ToString() => Method;
    }

    public static IEnumerable<C2SIMCase> C2SIMCases()
    {
        yield return new C2SIMCase("GetC2SIMContent", (r, o) => r.GetC2SIMContentAsync("plan", C2CIMDataType.Order, null, null, o, CancellationToken.None));
        yield return new C2SIMCase("PushC2SIMContent", (r, o) => r.PushC2SIMContentAsync("<x/>", C2CIMDataType.Order, o, CancellationToken.None));
#pragma warning disable CS0618 // exercises the obsolete member on purpose
        yield return new C2SIMCase("PullC2SIMInitialization", (r, o) => r.PullC2SIMInitializationAsync(o));
#pragma warning restore CS0618
        yield return new C2SIMCase("ConvertC2SIMContent", (r, o) => r.ConvertC2SIMContentAsync("<x/>", o));
    }

    [TestCaseSource(nameof(C2SIMCases))]
    public async Task TypedOptions_NamedKeys_GoCamelCaseOnEveryC2SIMCall(C2SIMCase c)
    {
        var options = new StpC2SIMOptions
        {
            RefuseUnconfirmedPlaceholders = true,
            RestUrl = "http://10.2.10.30:8080/C2SIMServer",
            ValidationSchema = "C2SIM_SMX_LOX_v1.0.2.xsd",
            IncludeDescriptionInName = false,
        };

        await c.Call(_recognizer, options);

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo(c.Method));
        var o = (JObject)sent["params"]["options"];
        Assert.That(o.Properties().Select(p => p.Name),
            Is.EquivalentTo(new[] { "refuseUnconfirmedPlaceholders", "restUrl", "validationSchema", "includeDescriptionInName" }));
        Assert.That(o["refuseUnconfirmedPlaceholders"].Type, Is.EqualTo(JTokenType.Boolean));
        Assert.That((bool)o["refuseUnconfirmedPlaceholders"], Is.True);
        Assert.That(o["restUrl"].ToString(), Is.EqualTo("http://10.2.10.30:8080/C2SIMServer"));
        Assert.That(o["validationSchema"].ToString(), Is.EqualTo("C2SIM_SMX_LOX_v1.0.2.xsd"));
        Assert.That(o["includeDescriptionInName"].Type, Is.EqualTo(JTokenType.Boolean));
        Assert.That((bool)o["includeDescriptionInName"], Is.False);
    }

    [TestCaseSource(nameof(C2SIMCases))]
    public async Task DictionaryOptions_StillWork_OnEveryC2SIMCall(C2SIMCase c)
    {
        await c.Call(_recognizer, new Dictionary<string, object> { ["restUrl"] = "http://h:1/C2SIMServer", ["fullTO"] = true });

        var o = (JObject)LastSent()["params"]["options"];
        Assert.That(o.Properties().Select(p => p.Name), Is.EquivalentTo(new[] { "restUrl", "fullTO" }));
        Assert.That((bool)o["fullTO"], Is.True);
    }

    [Test]
    public void Options_SetBackToNull_RemovesTheKey()
    {
        var o = new StpC2SIMOptions { FullTO = true, SystemName = "STP" };
        o.FullTO = null;
        o.SystemName = null;

        Assert.That(o, Is.Empty);
        Assert.That(o.FullTO, Is.Null);
        Assert.That(o.SystemName, Is.Null);
    }

    [Test]
    public void Options_ReadBack_WhatWasSet()
    {
        var o = new StpC2SIMOptions { FullTO = false, PhaseDuration = 30, StartDate = "2026-10-19T01:00:00Z" };

        Assert.That(o.FullTO, Is.False);
        Assert.That(o.PhaseDuration, Is.EqualTo(30));
        Assert.That(o.StartDate, Is.EqualTo("2026-10-19T01:00:00Z"));
    }

    #endregion

    #region 6. Roles

    [Test]
    public void Roles_MatchTheContractEnum()
    {
        Assert.That(StpRole.All, Is.EqualTo(new[] { "S2", "S3", "S4", "FSO", "ENGINEER" }));
        Assert.That(StpRole.ENG, Is.EqualTo(StpRole.ENGINEER), "the alias sends the value every engine knows");
    }

    [Test]
    public async Task SetRole_Engineer_SendsENGINEER()
    {
        await _recognizer.SetRoleAsync(StpRole.ENG);

        Assert.That(LastSent()["params"]["role"].ToString(), Is.EqualTo("ENGINEER"));
    }

    #endregion

    #region 7. Per-call load timeout

    private static ObjectSet TwoUnits() => new(new List<StpObject>
    {
        new() { Type = "unit", Poid = "idT1095X000000" },
        new() { Type = "unit", Poid = "idT1095X000001" },
    });

    public record LoadCase(string Method, Func<StpRecognizer, ObjectSet, TimeSpan, Task> Call)
    {
        public override string ToString() => Method;
    }

    public static IEnumerable<LoadCase> LoadCases()
    {
        yield return new LoadCase("LoadNewScenarioFromObjectSet", (r, os, t) => r.LoadNewScenarioAsync(os, t));
        yield return new LoadCase("ImportPlanDataFromObjectSet", (r, os, t) => r.ImportSTPDataAsync(os, t));
        yield return new LoadCase("ImportCoaFromObjectSet", (r, os, t) => r.ImportCoaAsync(os, t));
        yield return new LoadCase("ImportTaskOrgFromObjectSet", (r, os, t) => r.ImportTaskOrgAsync(os, t));
        yield return new LoadCase("SyncScenarioSessionFromObjectSet", (r, os, t) => r.SyncScenarioSessionAsync(os, t));
    }

    [TestCaseSource(nameof(LoadCases))]
    public async Task Load_SendsObjects_AndTheTimeoutInWholeSeconds_WaitingLongerForTheReport(LoadCase c)
    {
        await c.Call(_recognizer, TwoUnits(), TimeSpan.FromSeconds(1.2));

        var sent = LastSent();
        Assert.That(sent["method"]?.ToString(), Is.EqualTo(c.Method));
        Assert.That(ParamNames(sent), Is.EqualTo(new[] { "objects" }));
        Assert.That(sent["params"]["objects"].Type, Is.EqualTo(JTokenType.Array));
        Assert.That(sent["params"]["objects"].Count(), Is.EqualTo(2));
        Assert.That(_connector.LastEngineTimeoutSeconds, Is.EqualTo(2), "rounded up");
        Assert.That(_connector.LastClientTimeoutMs, Is.EqualTo(2000 + (int)StpRecognizer.LoadReportGrace.TotalMilliseconds));
    }

    // ImportPlanDataFromObjectSet's timeout edge case in the contract, verbatim.
    private const string TimeoutReport =
        "The object-set load was cancelled (the request timed out or was cancelled) at object 3 of 4. " +
        "Applied before it (2): unit 'idT1095X000000', unit 'idT1095X000001'. " +
        "In flight, sent with no reply yet - STP may still apply and announce it: unit 'idT1095X000002'. " +
        "Not applied (1): unit 'idT1095X000003'.";

    [TestCaseSource(nameof(LoadCases))]
    public void Load_TimedOut_ThrowsTheEngineReportVerbatim(LoadCase c)
    {
        _connector.NextError = new StpException(TimeoutReport, null);

        var ex = Assert.ThrowsAsync<StpObjectSetLoadException>(async () => await c.Call(_recognizer, TwoUnits(), TimeSpan.FromSeconds(1)));

        Assert.That(ex.Method, Is.EqualTo(c.Method));
        Assert.That(ex.Report, Is.EqualTo(TimeoutReport));
        Assert.That(ex.Message, Is.EqualTo(TimeoutReport));
        Assert.That(ex, Is.InstanceOf<StpException>(), "existing catch (StpException) handlers still catch it");
    }

    [Test]
    public async Task Load_ZeroTimeout_IsNoEngineTimeout()
    {
        await _recognizer.ImportCoaAsync(TwoUnits(), TimeSpan.Zero);

        Assert.That(_connector.LastEngineTimeoutSeconds, Is.EqualTo(0));
        Assert.That(_connector.LastClientTimeoutMs, Is.EqualTo(Timeout.Infinite));
    }

    [Test]
    public void Load_NegativeTimeout_IsRefusedBeforeSending()
    {
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _recognizer.ImportCoaAsync(TwoUnits(), TimeSpan.FromSeconds(-1)));
        Assert.That(_connector.SentMessages, Is.Empty);
    }

    [Test]
    public async Task Load_WithAPlainConnector_SendsTheTimeoutThroughTheBaseCall()
    {
        var plain = new MetadataStubConnector { NextResponse = "idCOA1" };
        using var recognizer = new StpRecognizer(plain);

        string poid = await recognizer.ImportCoaAsync(TwoUnits(), TimeSpan.FromSeconds(3));

        Assert.That(poid, Is.EqualTo("idCOA1"));
        Assert.That(JObject.Parse(plain.SentMessages[^1])["method"]?.ToString(), Is.EqualTo("ImportCoaFromObjectSet"));
    }

    [Test]
    public async Task Load_WithoutTimeout_KeepsTheDefault30s()
    {
        var plain = new TimeoutRecordingConnector();
        using var recognizer = new StpRecognizer(plain);

        await recognizer.ImportCoaAsync(TwoUnits());

        Assert.That(plain.LastTimeoutMs, Is.EqualTo(30000));
    }

    [Test]
    public void RequestEnvelope_CarriesTheEngineTimeoutInSeconds()
    {
        var msg = JObject.Parse(StpJsonRpcConnector.BuildRequestMessage("{\"method\":\"ImportCoaFromObjectSet\"}", 9, 12));

        Assert.That(msg["method"]?.ToString(), Is.EqualTo("Request"));
        Assert.That(ParamNames(msg), Is.EqualTo(Sorted("jsonRequest", "cookie", "timeout")));
        Assert.That(msg["params"]["timeout"].Type, Is.EqualTo(JTokenType.Integer));
        Assert.That((int)msg["params"]["timeout"], Is.EqualTo(12));
        Assert.That((int)msg["params"]["cookie"], Is.EqualTo(9));
    }

    // The engine cuts the load off at its timeout (1 s) and only then sends its report; here it lands
    // 300 ms later. Drives the real connector's wait and answer handling (no socket).
    private const int EngineTimeoutSeconds = 1;
    private const int ReportLagMs = 300;

    private static async Task<string> LoadThroughConnectorAsync(int clientTimeoutMs)
    {
        var connector = new StpJsonRpcConnector(null, "ws://127.0.0.1:1/");
        const int cookie = 41;
        string inner = "{\"method\":\"ImportPlanDataFromObjectSet\",\"params\":{\"objects\":[]},\"id\":41}";
        string envelope = StpJsonRpcConnector.BuildRequestMessage(inner, cookie, EngineTimeoutSeconds);

        return await connector.SendAndAwaitAsync(envelope, cookie, clientTimeoutMs, sent =>
        {
            int engineTimeout = (int)JObject.Parse(sent)["params"]["timeout"];
            _ = Task.Run(async () =>
            {
                await Task.Delay(engineTimeout * 1000 + ReportLagMs);
                var answer = new JObject
                {
                    ["method"] = "RequestResponse",
                    ["params"] = new JObject { ["cookie"] = cookie, ["success"] = false, ["result"] = TimeoutReport },
                };
                await connector.ProcessMessageAsync(answer.ToString());
            });
        }, CancellationToken.None);
    }

    [Test]
    public void TimedOutLoad_ClientWaitingLongerThanTheEngine_GetsTheReport()
    {
        int clientMs = EngineTimeoutSeconds * 1000 + (int)StpRecognizer.LoadReportGrace.TotalMilliseconds;

        var ex = Assert.ThrowsAsync<StpException>(async () => await LoadThroughConnectorAsync(clientMs));

        Assert.That(ex.Message, Is.EqualTo(TimeoutReport));
        Assert.That(ex.Message, Does.Contain("Applied before it (2)"));
        Assert.That(ex.Message, Does.Contain("In flight"));
        Assert.That(ex.Message, Does.Contain("Not applied (1)"));
    }

    [Test]
    public void TimedOutLoad_ClientWaitingOnlyAsLongAsTheEngine_LosesTheReport()
    {
        // The pre-STP-1798 behaviour (both sides 30 s, here 1 s): the client gives up first.
        Assert.ThrowsAsync<TimeoutException>(async () => await LoadThroughConnectorAsync(EngineTimeoutSeconds * 1000));
    }

    [Test]
    public void LoadReportGrace_CoversTheReportLag()
    {
        Assert.That(StpRecognizer.LoadReportGrace.TotalMilliseconds, Is.GreaterThan(ReportLagMs));
    }

    private class TimeoutRecordingConnector : MetadataStubConnectorBase
    {
        public int LastTimeoutMs { get; private set; }

        public override Task<string> SendRequestAsync(string jsonMessage, int cookie, int timeoutMs = 30000, CancellationToken ct = default)
        {
            LastTimeoutMs = timeoutMs;
            return Task.FromResult("idCOA1");
        }
    }

    #endregion

    #region 9. When* streams

    [Test]
    public void WhenTaskEventAdded_CarriesPoidEntryAndBatchId()
    {
        var seen = new List<TaskEventEventArgs>();
        using var sub = _recognizer.WhenTaskEventAdded.Subscribe(seen.Add);

        _connector.SimulateMessage(@"{ ""method"": ""TaskEventAdded"", ""params"": {
            ""poid"": ""idEV3"",
            ""entry"": { ""task"": ""idTASK1"", ""utc"": ""2026-10-19T01:00:00.123+02:00"", ""kind"": ""comment"", ""origin"": ""user"",
              ""author"": ""Pat"", ""role"": ""s3"", ""source"": ""WebSocketsBridge"", ""text"": ""hi"", ""detail"": ""{}"", ""edited"": null, ""deleted"": null },
            ""batchId"": ""batch-7"" } }");

        Assert.That(seen, Has.Count.EqualTo(1));
        Assert.That(seen[0].Poid, Is.EqualTo("idEV3"));
        Assert.That(seen[0].Entry.Utc, Is.EqualTo("2026-10-19T01:00:00.123+02:00"));
        Assert.That(seen[0].BatchId, Is.EqualTo("batch-7"));
    }

    [Test]
    public void WhenTaskEventModified_CarriesPoidEntryAndBatchId()
    {
        var seen = new List<TaskEventEventArgs>();
        using var sub = _recognizer.WhenTaskEventModified.Subscribe(seen.Add);

        _connector.SimulateMessage(@"{ ""method"": ""TaskEventModified"", ""params"": {
            ""poid"": ""idEV2"",
            ""entry"": { ""task"": ""idTASK1"", ""utc"": ""2026-10-19T01:00:00Z"", ""kind"": ""comment"", ""origin"": ""user"",
              ""author"": ""Pat"", ""role"": ""s3"", ""source"": ""WebSocketsBridge"", ""text"": null, ""detail"": null,
              ""edited"": null, ""deleted"": ""2026-10-19T01:10:00Z"" },
            ""batchId"": null } }");

        Assert.That(seen, Has.Count.EqualTo(1));
        Assert.That(seen[0].Poid, Is.EqualTo("idEV2"));
        Assert.That(seen[0].Entry.Deleted, Is.EqualTo("2026-10-19T01:10:00Z"));
        Assert.That(seen[0].BatchId, Is.Null);
    }

    [Test]
    public void WhenBatchStarted_CarriesEveryParam()
    {
        var seen = new List<BatchStartedEventArgs>();
        using var sub = _recognizer.WhenBatchStarted.Subscribe(seen.Add);

        _connector.SimulateMessage(@"{ ""method"": ""BatchStarted"", ""params"": {
            ""batchId"": ""batch-7"", ""kind"": ""load"", ""expected"": 42, ""isOwn"": true, ""origin"": ""live"" } }");

        Assert.That(seen, Has.Count.EqualTo(1));
        var b = seen[0];
        Assert.That((b.BatchId, b.Kind, b.Expected, b.IsOwn, b.Origin), Is.EqualTo(("batch-7", "load", 42, true, "live")));
    }

    [Test]
    public void WhenBatchEnded_CarriesEveryParam()
    {
        var seen = new List<BatchEndedEventArgs>();
        using var sub = _recognizer.WhenBatchEnded.Subscribe(seen.Add);

        _connector.SimulateMessage(@"{ ""method"": ""BatchEnded"", ""params"": {
            ""batchId"": ""batch-7"", ""kind"": ""import"", ""status"": ""abandoned"", ""informs"": 17, ""isOwn"": false, ""origin"": ""replay"" } }");

        Assert.That(seen, Has.Count.EqualTo(1));
        var b = seen[0];
        Assert.That((b.BatchId, b.Kind, b.Status, b.Informs, b.IsOwn, b.Origin), Is.EqualTo(("batch-7", "import", "abandoned", 17, false, "replay")));
    }

    [Test]
    public void WhenStreams_DetachOnDispose()
    {
        int count = 0;
        var sub = _recognizer.WhenBatchStarted.Subscribe(_ => count++);
        const string msg = @"{ ""method"": ""BatchStarted"", ""params"": { ""batchId"": ""b"", ""kind"": ""load"", ""expected"": 1, ""isOwn"": true, ""origin"": ""live"" } }";

        _connector.SimulateMessage(msg);
        sub.Dispose();
        _connector.SimulateMessage(msg);

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task WhenStreams_SubscribeTheirEvents()
    {
        // The solvables come from the attached handlers; StubConnector records them.
        var plain = new StubConnector();
        using var recognizer = new StpRecognizer(plain);
        using var a2 = recognizer.WhenTaskEventAdded.Subscribe(_ => { });
        using var m2 = recognizer.WhenTaskEventModified.Subscribe(_ => { });
        using var s2 = recognizer.WhenBatchStarted.Subscribe(_ => { });
        using var e2 = recognizer.WhenBatchEnded.Subscribe(_ => { });
        await recognizer.RegisterAsync("test");

        Assert.That(plain.LastSolvables, Is.SupersetOf(new[] { "TaskEventAdded", "TaskEventModified", "BatchStarted", "BatchEnded" }));
    }

    #endregion
}

/// <summary>A minimal overridable IStpConnector for tests that only care about one member.</summary>
internal abstract class MetadataStubConnectorBase : IStpConnector
{
    public event StpMessageReceivedDelegate OnMessage { add { } remove { } }
    public event StpConnectionErrorDelegate OnConnectionError { add { } remove { } }
    public ILogger Logger => null;
    public string Name => "base-stub";
    public string BaseName => "base-stub";
    public bool Connected => true;
    public Task<bool> ConnectAsync(string url, int secondsToRetry = 0, CancellationToken ct = default) => Task.FromResult(true);
    public Task<string> RegisterAsync(string serviceName, List<string> solvables, string machineId = null, string sessionId = null, CancellationToken ct = default) => Task.FromResult("session");
    public void Disconnect() { }
    public void Send(string jsonMessage) { }
    public abstract Task<string> SendRequestAsync(string jsonMessage, int cookie, int timeoutMs = 30000, CancellationToken ct = default);
    public void Dispose() { }
}

/// <summary>
/// Item 8 of STP-1798: a request result reaches the caller with its strings exactly as the engine sent
/// them. The connector hands results on as <c>Result.ToString()</c>; parsed with the default date handling,
/// an ISO timestamp became a DateTime and came back reformatted (offset dropped or shifted, fraction lost).
/// </summary>
[TestFixture]
public class RequestResultVerbatimTests
{
    private static string ResultFor(string resultJson)
    {
        // Never connects: the message pump is driven directly (as RefusalMessageTests does).
        var connector = new StpJsonRpcConnector(null, "ws://127.0.0.1:1/");
        var pending = typeof(StpJsonRpcConnector)
            .GetField("_pendingRequests", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(connector);
        var tcs = new TaskCompletionSource<string>();
        pending.GetType().GetMethod("TryAdd").Invoke(pending, new object[] { 1, tcs });

        typeof(StpJsonRpcConnector)
            .GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(connector, new object[] { "{\"method\":\"RequestResponse\",\"params\":{\"cookie\":1,\"success\":true,\"result\":" + resultJson + "}}" });

        Assert.That(tcs.Task.IsCompleted, Is.True);
        return tcs.Task.Result;
    }

    [TestCase("2026-10-19T01:00:00.123+02:00")]
    [TestCase("2026-10-19T01:00:00+05:30")]
    [TestCase("2026-10-19T01:00:00.1234567Z")]
    [TestCase("2026-10-19T01:00:00Z")]
    public void StringResult_IsVerbatim(string iso)
    {
        Assert.That(ResultFor("\"" + iso + "\""), Is.EqualTo(iso));
    }

    [Test]
    public async Task PullC2SIM_XmlAnswer_ThroughTheConnector_IsVerbatim()
    {
        const string xml = "<?xml version=\"1.0\"?>\n<MessageBody><StartTime>2026-10-19T01:00:00.123+02:00</StartTime></MessageBody>";
        string raw = ResultFor(new JValue(xml).ToString(Newtonsoft.Json.Formatting.None));
        var stub = new MetadataStubConnector { NextResponse = raw };
        using var recognizer = new StpRecognizer(stub);

        Assert.That(await recognizer.PullC2SIMInitializationContentAsync(), Is.EqualTo(xml));
#pragma warning disable CS0618 // exercises the obsolete member on purpose
        Assert.That((await recognizer.PullC2SIMInitializationAsync()).Content, Is.EqualTo(xml));
#pragma warning restore CS0618
    }

    [Test]
    public void ObjectResult_KeepsTimestampStringsVerbatim()
    {
        string result = ResultFor("{\"task\":\"idTASK1\",\"utc\":\"2026-10-19T01:00:00.123+02:00\"}");

        Assert.That(result, Does.Contain("\"2026-10-19T01:00:00.123+02:00\""));
    }

    [Test]
    public async Task TaskHistory_ThroughTheConnector_KeepsUtcVerbatim()
    {
        string raw = ResultFor(@"[{ ""poid"": ""idEV2"", ""entry"": { ""task"": ""idTASK1"", ""utc"": ""2026-10-19T01:00:00.5+02:00"",
            ""kind"": ""comment"", ""origin"": ""user"", ""author"": ""Pat"", ""role"": ""s3"", ""source"": ""WebSocketsBridge"",
            ""text"": ""t"", ""detail"": ""{}"", ""edited"": null, ""deleted"": null } }]");
        var stub = new MetadataStubConnector { NextResponse = raw };
        using var recognizer = new StpRecognizer(stub);

        var items = await recognizer.GetTaskHistoryAsync("idTASK1");

        Assert.That(items[0].Entry.Utc, Is.EqualTo("2026-10-19T01:00:00.5+02:00"));
    }
}
