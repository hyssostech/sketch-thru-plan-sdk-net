using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StpSDK;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StpSDK.Tests;

internal class MetadataStubConnector : IStpConnector
{
    public event StpMessageReceivedDelegate OnMessage;
    public event StpConnectionErrorDelegate OnConnectionError;

    public ILogger Logger => null;
    public string Name => "meta-stub";
    public string BaseName => "meta-stub";
    public bool Connected { get; set; } = true;

    public List<string> SentMessages { get; } = new();
    public string NextResponse { get; set; } = "\"ok\"";

    public Task<bool> ConnectAsync(string url, int secondsToRetry = 0, CancellationToken ct = default)
    {
        Connected = true;
        return Task.FromResult(true);
    }

    public Task<string> RegisterAsync(string serviceName, List<string> solvables, string machineId = null, string sessionId = null, CancellationToken ct = default)
        => Task.FromResult("session");

    public void Disconnect() => Connected = false;

    public void Send(string jsonMessage) => SentMessages.Add(jsonMessage);

    public Task<string> SendRequestAsync(string jsonMessage, int cookie, int timeoutMs = 30000, CancellationToken ct = default)
    {
        SentMessages.Add(jsonMessage);
        return Task.FromResult(NextResponse);
    }

    public void Dispose() { }
}

[TestFixture]
public class RecognizerMetadataTests
{
    private MetadataStubConnector _connector;
    private StpRecognizer _recognizer;

    [SetUp]
    public void Setup()
    {
        _connector = new MetadataStubConnector();
        _recognizer = new StpRecognizer(_connector);
    }

    [TearDown]
    public void TearDown()
    {
        _recognizer.Dispose();
    }

    private JObject LastSentJson()
    {
        Assert.That(_connector.SentMessages, Has.Count.GreaterThan(0), "No messages were sent");
        return JObject.Parse(_connector.SentMessages[^1]);
    }

    #region Scenario Management

    [Test]
    public async Task CreateNewScenarioAsync_SendsCorrectMethodAndParams()
    {
        var result = await _recognizer.CreateNewScenarioAsync("TestScenario");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("CreateNewScenario"));
        Assert.That(json["params"]?["name"]?.ToString(), Is.EqualTo("TestScenario"));
        Assert.That(json["id"]?.Type, Is.EqualTo(JTokenType.Integer));
        Assert.That(result, Is.EqualTo("\"ok\""));
    }

    [Test]
    public async Task CreateCoaAsync_SendsCorrectMethodAndParams()
    {
        var result = await _recognizer.CreateCoaAsync("COA1", "FRIENDLY", "Commander");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("CreateCoa"));
        Assert.That(json["params"]?["name"]?.ToString(), Is.EqualTo("COA1"));
        Assert.That(json["params"]?["affiliation"]?.ToString(), Is.EqualTo("FRIENDLY"));
        Assert.That(json["params"]?["role"]?.ToString(), Is.EqualTo("Commander"));
        Assert.That(result, Is.EqualTo("\"ok\""));
    }

    [Test]
    public async Task LoadNewScenarioAsync_String_SendsCorrectMethodAndParams()
    {
        await _recognizer.LoadNewScenarioAsync("<scenario>data</scenario>");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("LoadNewScenario"));
        Assert.That(json["params"]?["content"]?.ToString(), Is.EqualTo("<scenario>data</scenario>"));
    }

    [Test]
    public async Task LoadNewScenarioAsync_ObjectSet_SendsCorrectMethodAndSerializedContent()
    {
        var os = new ObjectSet(new List<StpObject>
        {
            new StpObject { Type = "unit", Poid = "p1" }
        });

        await _recognizer.LoadNewScenarioAsync(os);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("LoadNewScenarioFromObjectSet"));
        var contentStr = json["params"]?["content"]?.ToString();
        Assert.That(contentStr, Is.Not.Null.And.Not.Empty);
        var parsed = JsonConvert.DeserializeObject<ObjectSet>(contentStr);
        Assert.That(parsed.Objects, Has.Count.EqualTo(1));
        Assert.That(parsed.Objects[0].Poid, Is.EqualTo("p1"));
    }

    [Test]
    public async Task ResetStpScenarioAsync_SendsCorrectMethod()
    {
        await _recognizer.ResetStpScenarioAsync();

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("ResetStpScenario"));
    }

    [Test]
    public async Task GetScenarioContentAsync_ReturnsStringResult()
    {
        _connector.NextResponse = "<scenario>xml content</scenario>";

        var result = await _recognizer.GetScenarioContentAsync();

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetScenarioContent"));
        Assert.That(result, Is.EqualTo("<scenario>xml content</scenario>"));
    }

    [Test]
    public async Task GetScenarioObjectSetContentAsync_DeserializesObjectSet()
    {
        _connector.NextResponse = "{\"objects\":[{\"fsTYPE\":\"unit\",\"poid\":\"p1\"}]}";

        var result = await _recognizer.GetScenarioObjectSetContentAsync();

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetScenarioObjectSet"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Objects, Has.Count.EqualTo(1));
        Assert.That(result.Objects[0].Poid, Is.EqualTo("p1"));
        Assert.That(result.Objects[0].Type, Is.EqualTo("unit"));
    }

    [Test]
    public async Task GetScenarioObjectSetContentAsync_EmptyResult_ReturnsEmptyObjectSet()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.GetScenarioObjectSetContentAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Objects, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task JoinScenarioSessionAsync_SendsCorrectMethod()
    {
        await _recognizer.JoinScenarioSessionAsync();

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("JoinScenarioSession"));
    }

    [Test]
    public async Task SyncScenarioSessionAsync_String_SendsCorrectMethodAndParams()
    {
        await _recognizer.SyncScenarioSessionAsync("<sync>content</sync>");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("SyncScenarioSession"));
        Assert.That(json["params"]?["content"]?.ToString(), Is.EqualTo("<sync>content</sync>"));
    }

    [Test]
    public async Task SyncScenarioSessionAsync_ObjectSet_SendsCorrectMethodAndSerializedContent()
    {
        var os = new ObjectSet(new List<StpObject>
        {
            new StpObject { Type = "tg", Poid = "tg-1" }
        });

        await _recognizer.SyncScenarioSessionAsync(os);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("SyncScenarioSessionFromObjectSet"));
        var contentStr = json["params"]?["content"]?.ToString();
        Assert.That(contentStr, Is.Not.Null.And.Not.Empty);
        var parsed = JsonConvert.DeserializeObject<ObjectSet>(contentStr);
        Assert.That(parsed.Objects[0].Poid, Is.EqualTo("tg-1"));
    }

    [Test]
    public async Task ImportSTPDataAsync_String_ReturnsTrue_WhenResultIsNonEmpty()
    {
        _connector.NextResponse = "imported";

        var result = await _recognizer.ImportSTPDataAsync("<data>plan</data>");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("ImportPlanData"));
        Assert.That(json["params"]?["content"]?.ToString(), Is.EqualTo("<data>plan</data>"));
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ImportSTPDataAsync_String_ReturnsFalse_WhenResultIsFalse()
    {
        _connector.NextResponse = "false";

        var result = await _recognizer.ImportSTPDataAsync("content");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ImportSTPDataAsync_String_ReturnsFalse_WhenResultIsEmpty()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.ImportSTPDataAsync("content");

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ImportSTPDataAsync_ObjectSet_SendsCorrectMethodAndReturnsTrue()
    {
        _connector.NextResponse = "imported";
        var os = new ObjectSet(new List<StpObject>
        {
            new StpObject { Type = "unit", Poid = "u1" }
        });

        var result = await _recognizer.ImportSTPDataAsync(os);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("ImportPlanDataFromObjectSet"));
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ImportSTPDataAsync_ObjectSet_ReturnsFalse_WhenResultIsFalse()
    {
        _connector.NextResponse = "false";
        var os = new ObjectSet();

        var result = await _recognizer.ImportSTPDataAsync(os);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task HasActiveScenarioAsync_ReturnsTrue_WhenResultIsTrue()
    {
        _connector.NextResponse = "true";

        var result = await _recognizer.HasActiveScenarioAsync();

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("HasActiveScenario"));
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HasActiveScenarioAsync_ReturnsFalse_WhenResultIsFalse()
    {
        _connector.NextResponse = "false";

        var result = await _recognizer.HasActiveScenarioAsync();

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task HasActiveScenarioAsync_ReturnsFalse_WhenResultIsOther()
    {
        _connector.NextResponse = "something";

        var result = await _recognizer.HasActiveScenarioAsync();

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetActiveScenarioDescriptionAsync_DeserializesPlanningScenario()
    {
        _connector.NextResponse = "{\"fsTYPE\":\"planning_scenario\",\"name\":\"Scenario1\",\"isValid\":true,\"isLoaded\":true,\"creatorRole\":\"CO\"}";

        var result = await _recognizer.GetActiveScenarioDescriptionAsync();

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetActiveScenarioDescription"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Scenario1"));
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.IsLoaded, Is.True);
        Assert.That(result.CreatorRole, Is.EqualTo("CO"));
    }

    [Test]
    public async Task GetActiveScenarioDescriptionAsync_ReturnsNull_WhenResultIsEmpty()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.GetActiveScenarioDescriptionAsync();

        Assert.That(result, Is.Null);
    }

    #endregion

    #region Task Org Import/Export

    [Test]
    public async Task ImportTaskOrgContentAsync_SendsCorrectMethodAndParams()
    {
        var result = await _recognizer.ImportTaskOrgContentAsync("<taskorg>data</taskorg>");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("ImportTaskOrgContent"));
        Assert.That(json["params"]?["content"]?.ToString(), Is.EqualTo("<taskorg>data</taskorg>"));
        Assert.That(result, Is.EqualTo("\"ok\""));
    }

    [Test]
    public async Task ImportTaskOrgAsync_ObjectSet_SendsCorrectMethodAndSerializedContent()
    {
        var os = new ObjectSet(new List<StpObject>
        {
            new StpObject { Type = "task_org", Poid = "to-1" }
        });

        var result = await _recognizer.ImportTaskOrgAsync(os);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("ImportTaskOrgFromObjectSet"));
        var contentStr = json["params"]?["content"]?.ToString();
        Assert.That(contentStr, Is.Not.Null.And.Not.Empty);
        Assert.That(result, Is.EqualTo("\"ok\""));
    }

    [Test]
    public async Task GetTaskOrgContentAsync_SendsCorrectMethodAndParams()
    {
        _connector.NextResponse = "{\"name\":\"TO1\"}";

        var result = await _recognizer.GetTaskOrgContentAsync("to-poid-1", CancellationToken.None);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetTaskOrgContent"));
        Assert.That(json["params"]?["poid"]?.ToString(), Is.EqualTo("to-poid-1"));
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetTaskOrgContentAsync_ReturnsNull_WhenResultIsEmpty()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.GetTaskOrgContentAsync("to-poid-1", CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetTaskOrgObjectSetAsync_DeserializesObjectSet()
    {
        _connector.NextResponse = "{\"objects\":[{\"fsTYPE\":\"task_org_unit\",\"poid\":\"tou-1\"}]}";

        var result = await _recognizer.GetTaskOrgObjectSetAsync("to-poid-1");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetTaskOrgObjectSet"));
        Assert.That(json["params"]?["poid"]?.ToString(), Is.EqualTo("to-poid-1"));
        Assert.That(result.Objects, Has.Count.EqualTo(1));
        Assert.That(result.Objects[0].Poid, Is.EqualTo("tou-1"));
    }

    [Test]
    public async Task GetTaskOrgObjectSetAsync_EmptyResult_ReturnsEmptyObjectSet()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.GetTaskOrgObjectSetAsync("to-poid-1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Objects, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task SetDefaultTaskOrgAsync_SendsCorrectMethodAndParams()
    {
        await _recognizer.SetDefaultTaskOrgAsync("to-poid-1");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("SetDefaultTaskOrg"));
        Assert.That(json["params"]?["poid"]?.ToString(), Is.EqualTo("to-poid-1"));
    }

    [Test]
    public async Task ResetDefaultTaskOrgAsync_SendsCorrectMethodAndParams()
    {
        await _recognizer.ResetDefaultTaskOrgAsync("FRIENDLY", CancellationToken.None);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("ResetDefaultTaskOrg"));
        Assert.That(json["params"]?["affiliation"]?.ToString(), Is.EqualTo("FRIENDLY"));
    }

    #endregion

    #region COA Import/Export

    [Test]
    public async Task ImportCoaContentAsync_SendsCorrectMethodAndParams()
    {
        var result = await _recognizer.ImportCoaContentAsync("<coa>data</coa>");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("ImportCoaContent"));
        Assert.That(json["params"]?["content"]?.ToString(), Is.EqualTo("<coa>data</coa>"));
        Assert.That(result, Is.EqualTo("\"ok\""));
    }

    [Test]
    public async Task ImportCoaAsync_ObjectSet_SendsCorrectMethodAndSerializedContent()
    {
        var os = new ObjectSet(new List<StpObject>
        {
            new StpObject { Type = "coa", Poid = "coa-1" }
        });

        var result = await _recognizer.ImportCoaAsync(os);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("ImportCoaFromObjectSet"));
        var contentStr = json["params"]?["content"]?.ToString();
        Assert.That(contentStr, Is.Not.Null.And.Not.Empty);
        Assert.That(result, Is.EqualTo("\"ok\""));
    }

    [Test]
    public async Task GetCoaContentAsync_SendsCorrectMethodAndParams()
    {
        _connector.NextResponse = "{\"name\":\"COA1\"}";

        var result = await _recognizer.GetCoaContentAsync("coa-poid-1");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetCoaContent"));
        Assert.That(json["params"]?["poid"]?.ToString(), Is.EqualTo("coa-poid-1"));
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetCoaContentAsync_ReturnsNull_WhenResultIsEmpty()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.GetCoaContentAsync("coa-poid-1");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCoaObjectSetAsync_DeserializesObjectSet()
    {
        _connector.NextResponse = "{\"objects\":[{\"fsTYPE\":\"unit\",\"poid\":\"u-1\"},{\"fsTYPE\":\"tg\",\"poid\":\"tg-1\"}]}";

        var result = await _recognizer.GetCoaObjectSetAsync("coa-poid-1");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetCoaObjectSet"));
        Assert.That(json["params"]?["poid"]?.ToString(), Is.EqualTo("coa-poid-1"));
        Assert.That(result.Objects, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetCoaObjectSetAsync_EmptyResult_ReturnsEmptyObjectSet()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.GetCoaObjectSetAsync("coa-poid-1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Objects, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task DeleteCoaAsync_SendsCorrectMethodAndParams()
    {
        await _recognizer.DeleteCoaAsync("coa-poid-1");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("DeleteCoa"));
        Assert.That(json["params"]?["poid"]?.ToString(), Is.EqualTo("coa-poid-1"));
    }

    #endregion

    #region Role and COA

    [Test]
    public async Task SetRoleAsync_SendsCorrectMethodAndParams()
    {
        await _recognizer.SetRoleAsync("FRIENDLY_CO");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("SetRole"));
        Assert.That(json["params"]?["role"]?.ToString(), Is.EqualTo("FRIENDLY_CO"));
    }

    [Test]
    public async Task SetCoaAsync_SendsCorrectMethodAndParams()
    {
        await _recognizer.SetCoaAsync("coa-poid-99");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("SetCurrentCoa"));
        Assert.That(json["params"]?["coaPoid"]?.ToString(), Is.EqualTo("coa-poid-99"));
    }

    [Test]
    public async Task GetCurrentCoaPoidAsync_SendsCorrectMethodAndReturnsResult()
    {
        _connector.NextResponse = "coa-active-1";

        var result = await _recognizer.GetCurrentCoaPoidAsync("FRIENDLY_CO");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetCurrentCoaPoid"));
        Assert.That(json["params"]?["role"]?.ToString(), Is.EqualTo("FRIENDLY_CO"));
        Assert.That(result, Is.EqualTo("coa-active-1"));
    }

    [Test]
    public async Task CreateDefaultCoaAsync_SendsCorrectMethodAndParams()
    {
        _connector.NextResponse = "new-coa-poid";

        var result = await _recognizer.CreateDefaultCoaAsync("ENEMY_CO");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("CreateDefaultCoa"));
        Assert.That(json["params"]?["role"]?.ToString(), Is.EqualTo("ENEMY_CO"));
        Assert.That(result, Is.EqualTo("new-coa-poid"));
    }

    [Test]
    public async Task SwitchRoleAndCoaAsync_SendsCorrectMethodAndParams()
    {
        await _recognizer.SwitchRoleAndCoaAsync("FRIENDLY_XO", "coa-poid-2");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("SwitchRoleAndCoa"));
        Assert.That(json["params"]?["role"]?.ToString(), Is.EqualTo("FRIENDLY_XO"));
        Assert.That(json["params"]?["coaPoid"]?.ToString(), Is.EqualTo("coa-poid-2"));
    }

    [Test]
    public async Task SwitchTaskConfirmationAsync_SendsCorrectMethodAndParams()
    {
        await _recognizer.SwitchTaskConfirmationAsync("task-poid-1", 3, true);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("SwitchTaskConfirmation"));
        Assert.That(json["params"]?["poid"]?.ToString(), Is.EqualTo("task-poid-1"));
        Assert.That(json["params"]?["index"]?.Value<int>(), Is.EqualTo(3));
        Assert.That(json["params"]?["isConfirmed"]?.Value<bool>(), Is.True);
    }

    [Test]
    public async Task SwitchTaskConfirmationAsync_FalseConfirmation_SendsCorrectParams()
    {
        await _recognizer.SwitchTaskConfirmationAsync("task-poid-2", 0, false);

        var json = LastSentJson();
        Assert.That(json["params"]?["isConfirmed"]?.Value<bool>(), Is.False);
        Assert.That(json["params"]?["index"]?.Value<int>(), Is.EqualTo(0));
    }

    #endregion

    #region Queries

    [Test]
    public async Task RequestActiveCoasAsync_DeserializesCoaList()
    {
        _connector.NextResponse = "[{\"fsTYPE\":\"coa\",\"poid\":\"coa-1\",\"name\":\"COA Alpha\"},{\"fsTYPE\":\"coa\",\"poid\":\"coa-2\",\"name\":\"COA Bravo\"}]";

        var result = await _recognizer.RequestActiveCoasAsync(CancellationToken.None);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetActiveCoaList"));
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Poid, Is.EqualTo("coa-1"));
        Assert.That(result[0].Name, Is.EqualTo("COA Alpha"));
        Assert.That(result[1].Poid, Is.EqualTo("coa-2"));
        Assert.That(result[1].Name, Is.EqualTo("COA Bravo"));
    }

    [Test]
    public async Task RequestActiveCoasAsync_EmptyResult_ReturnsEmptyList()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.RequestActiveCoasAsync(CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task RequestAllStpObjectsAsync_DeserializesAndCallsAsTypedObject()
    {
        _connector.NextResponse = "[{\"fsTYPE\":\"unit\",\"poid\":\"u-1\"},{\"fsTYPE\":\"task\",\"poid\":\"t-1\"}]";

        var result = await _recognizer.RequestAllStpObjectsAsync(CancellationToken.None);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetAllObjects"));
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Poid, Is.EqualTo("u-1"));
        Assert.That(result[0].Type, Is.EqualTo("unit"));
        Assert.That(result[1].Poid, Is.EqualTo("t-1"));
        Assert.That(result[1].Type, Is.EqualTo("task"));
    }

    [Test]
    public async Task RequestAllStpObjectsAsync_EmptyResult_ReturnsEmptyList()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.RequestAllStpObjectsAsync(CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task GetScenarioCoasAsync_DeserializesCoaList()
    {
        _connector.NextResponse = "[{\"fsTYPE\":\"coa\",\"poid\":\"sc-coa-1\",\"name\":\"Scenario COA\"}]";

        var result = await _recognizer.GetScenarioCoasAsync();

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetScenarioCoaList"));
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Poid, Is.EqualTo("sc-coa-1"));
        Assert.That(result[0].Name, Is.EqualTo("Scenario COA"));
    }

    [Test]
    public async Task GetScenarioCoasAsync_EmptyResult_ReturnsEmptyList()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.GetScenarioCoasAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task GetScenarioTaskOrgsAsync_DeserializesTaskOrgList()
    {
        _connector.NextResponse = "[{\"fsTYPE\":\"task_org\",\"poid\":\"to-1\",\"name\":\"Main TO\"}]";

        var result = await _recognizer.GetScenarioTaskOrgsAsync();

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetScenarioTaskOrgList"));
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Poid, Is.EqualTo("to-1"));
        Assert.That(result[0].Name, Is.EqualTo("Main TO"));
    }

    [Test]
    public async Task GetScenarioTaskOrgsAsync_EmptyResult_ReturnsEmptyList()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.GetScenarioTaskOrgsAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task GetTaskOrgObjectsAsync_DeserializesAndTypesObjects()
    {
        _connector.NextResponse = "[{\"fsTYPE\":\"task_org_unit\",\"poid\":\"tou-1\"},{\"fsTYPE\":\"task_org_relationship\",\"poid\":\"tor-1\"}]";

        var result = await _recognizer.GetTaskOrgObjectsAsync("to-poid-1");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetTaskOrgObjects"));
        Assert.That(json["params"]?["poid"]?.ToString(), Is.EqualTo("to-poid-1"));
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Poid, Is.EqualTo("tou-1"));
        Assert.That(result[1].Poid, Is.EqualTo("tor-1"));
    }

    [Test]
    public async Task GetTaskOrgObjectsAsync_EmptyResult_ReturnsEmptyList()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.GetTaskOrgObjectsAsync("to-poid-1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task GetCoaObjectsAsync_DeserializesAndTypesObjects()
    {
        _connector.NextResponse = "[{\"fsTYPE\":\"unit\",\"poid\":\"u-1\"},{\"fsTYPE\":\"tg\",\"poid\":\"tg-1\"}]";

        var result = await _recognizer.GetCoaObjectsAsync("coa-poid-1");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetCoaObjects"));
        Assert.That(json["params"]?["poid"]?.ToString(), Is.EqualTo("coa-poid-1"));
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Poid, Is.EqualTo("u-1"));
        Assert.That(result[1].Poid, Is.EqualTo("tg-1"));
    }

    [Test]
    public async Task GetCoaObjectsAsync_EmptyResult_ReturnsEmptyList()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.GetCoaObjectsAsync("coa-poid-1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task RequestPoidObjectAsync_DeserializesAndTypesObject()
    {
        _connector.NextResponse = "{\"fsTYPE\":\"unit\",\"poid\":\"u-42\"}";

        var result = await _recognizer.RequestPoidObjectAsync("u-42");

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetPoidObject"));
        Assert.That(json["params"]?["poid"]?.ToString(), Is.EqualTo("u-42"));
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Poid, Is.EqualTo("u-42"));
        Assert.That(result.Type, Is.EqualTo("unit"));
    }

    [Test]
    public async Task RequestPoidObjectAsync_EmptyResult_ReturnsNull()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.RequestPoidObjectAsync("u-42");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task RequestDeletedStpObjectsAsync_DeserializesAndTypesObjects()
    {
        _connector.NextResponse = "[{\"fsTYPE\":\"unit\",\"poid\":\"del-1\"},{\"fsTYPE\":\"tg\",\"poid\":\"del-2\"}]";

        var result = await _recognizer.RequestDeletedStpObjectsAsync();

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetDeletedObjects"));
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Poid, Is.EqualTo("del-1"));
        Assert.That(result[1].Poid, Is.EqualTo("del-2"));
    }

    [Test]
    public async Task RequestDeletedStpObjectsAsync_EmptyResult_ReturnsEmptyList()
    {
        _connector.NextResponse = "";

        var result = await _recognizer.RequestDeletedStpObjectsAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(0));
    }

    #endregion

    #region C2SIM

    [Test]
    public async Task GetC2SIMContentAsync_SendsCorrectMethodAndAllParams()
    {
        _connector.NextResponse = "<c2sim>content</c2sim>";

        var options = new Dictionary<string, object> { { "key1", "val1" } };
        var coaPoids = new List<string> { "coa-1", "coa-2" };

        var result = await _recognizer.GetC2SIMContentAsync(
            "TestName",
            C2CIMDataType.Order,
            "FRIENDLY",
            coaPoids,
            options,
            CancellationToken.None);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("GetC2SIMContent"));
        Assert.That(json["params"]?["name"]?.ToString(), Is.EqualTo("TestName"));
        Assert.That(json["params"]?["dataType"]?.ToString(), Is.EqualTo("Order"));
        Assert.That(json["params"]?["affiliation"]?.ToString(), Is.EqualTo("FRIENDLY"));
        Assert.That(json["params"]?["coaPoids"]?.ToObject<List<string>>(), Has.Count.EqualTo(2));
        Assert.That(json["params"]?["options"]?["key1"]?.ToString(), Is.EqualTo("val1"));
        Assert.That(result, Is.EqualTo("<c2sim>content</c2sim>"));
    }

    [Test]
    public async Task GetC2SIMContentAsync_AllDataType_SendsCorrectEnum()
    {
        _connector.NextResponse = "<c2sim/>";

        await _recognizer.GetC2SIMContentAsync(
            "Test", C2CIMDataType.All, "ENEMY", new List<string>(), null, CancellationToken.None);

        var json = LastSentJson();
        Assert.That(json["params"]?["dataType"]?.ToString(), Is.EqualTo("All"));
    }

    [Test]
    public async Task GetC2SIMContentAsync_InitializationDataType_SendsCorrectEnum()
    {
        _connector.NextResponse = "<c2sim/>";

        await _recognizer.GetC2SIMContentAsync(
            "Test", C2CIMDataType.Initialization, "FRIENDLY", new List<string>(), null, CancellationToken.None);

        var json = LastSentJson();
        Assert.That(json["params"]?["dataType"]?.ToString(), Is.EqualTo("Initialization"));
    }

    [Test]
    public async Task PushC2SIMContentAsync_SendsCorrectMethodAndParams()
    {
        var options = new Dictionary<string, object> { { "server", "localhost" } };

        await _recognizer.PushC2SIMContentAsync(
            "<c2sim>push data</c2sim>",
            C2CIMDataType.Order,
            options,
            CancellationToken.None);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("PushC2SIMContent"));
        Assert.That(json["params"]?["content"]?.ToString(), Is.EqualTo("<c2sim>push data</c2sim>"));
        Assert.That(json["params"]?["dataType"]?.ToString(), Is.EqualTo("Order"));
        Assert.That(json["params"]?["options"]?["server"]?.ToString(), Is.EqualTo("localhost"));
    }

    [Test]
    public async Task PushC2SIMContentAsync_NullOptions_OmitsOptionsFromJson()
    {
        await _recognizer.PushC2SIMContentAsync(
            "<c2sim/>",
            C2CIMDataType.Initialization,
            null,
            CancellationToken.None);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("PushC2SIMContent"));
        Assert.That(json["params"]?["content"]?.ToString(), Is.EqualTo("<c2sim/>"));
        Assert.That(json["params"]?["dataType"]?.ToString(), Is.EqualTo("Initialization"));
        // NullValueHandling.Ignore means null options should not appear
        Assert.That(json["params"]?["options"], Is.Null);
    }

    [Test]
    public async Task PullC2SIMInitializationAsync_DeserializesContentAndServerStatus()
    {
        _connector.NextResponse = "{\"content\":\"<xml/>\",\"serverStatus\":\"OK\"}";

        var (content, serverStatus) = await _recognizer.PullC2SIMInitializationAsync();

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("PullC2SIMInitialization"));
        Assert.That(content, Is.EqualTo("<xml/>"));
        Assert.That(serverStatus, Is.EqualTo("OK"));
    }

    [Test]
    public async Task PullC2SIMInitializationAsync_WithOptions_SendsOptions()
    {
        _connector.NextResponse = "{\"content\":\"<data/>\",\"serverStatus\":\"Connected\"}";
        var options = new Dictionary<string, object> { { "endpoint", "https://server" } };

        var (content, serverStatus) = await _recognizer.PullC2SIMInitializationAsync(options);

        var json = LastSentJson();
        Assert.That(json["params"]?["options"]?["endpoint"]?.ToString(), Is.EqualTo("https://server"));
        Assert.That(content, Is.EqualTo("<data/>"));
        Assert.That(serverStatus, Is.EqualTo("Connected"));
    }

    [Test]
    public async Task PullC2SIMInitializationAsync_EmptyResult_ReturnsTupleWithNulls()
    {
        _connector.NextResponse = "";

        var (content, serverStatus) = await _recognizer.PullC2SIMInitializationAsync();

        Assert.That(content, Is.Null);
        Assert.That(serverStatus, Is.Null);
    }

    [Test]
    public async Task PullC2SIMInitializationAsync_NullOptions_OmitsFromJson()
    {
        _connector.NextResponse = "{\"content\":\"x\",\"serverStatus\":\"y\"}";

        await _recognizer.PullC2SIMInitializationAsync(null);

        var json = LastSentJson();
        Assert.That(json["method"]?.ToString(), Is.EqualTo("PullC2SIMInitialization"));
        // With NullValueHandling.Ignore, null options should not appear
        Assert.That(json["params"]?["options"], Is.Null);
    }

    #endregion

    #region Common JSON Structure

    [Test]
    public async Task AllRequests_ContainMethodParamsAndId()
    {
        await _recognizer.CreateNewScenarioAsync("Test");

        var json = LastSentJson();
        Assert.That(json.ContainsKey("method"), Is.True);
        Assert.That(json.ContainsKey("params"), Is.True);
        Assert.That(json.ContainsKey("id"), Is.True);
    }

    [Test]
    public async Task CookieIds_IncrementAcrossRequests()
    {
        await _recognizer.ResetStpScenarioAsync();
        var json1 = LastSentJson();
        int id1 = json1["id"]!.Value<int>();

        await _recognizer.JoinScenarioSessionAsync();
        var json2 = LastSentJson();
        int id2 = json2["id"]!.Value<int>();

        Assert.That(id2, Is.GreaterThan(id1));
    }

    [Test]
    public async Task Requests_UseCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        // Just verify methods accept and pass through cancellation tokens without error
        await _recognizer.CreateNewScenarioAsync("Test", cts.Token);
        await _recognizer.ResetStpScenarioAsync(cts.Token);
        await _recognizer.HasActiveScenarioAsync(cts.Token);

        Assert.That(_connector.SentMessages, Has.Count.EqualTo(3));
    }

    #endregion
}
