using Microsoft.Extensions.Logging;
using NUnit.Framework;
using StpSDK;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StpSDK.Tests;

internal class StubConnector : IStpConnector
{
    public event StpMessageReceivedDelegate OnMessage;
    public event StpConnectionErrorDelegate OnConnectionError;

    public ILogger Logger => null;
    public string Name => "stub";
    public string BaseName => "stub";
    public bool Connected { get; private set; }

    public List<string> SentMessages { get; } = new();
    public string LastRegisteredService { get; private set; }
    public List<string> LastSolvables { get; private set; }

    public Task<bool> ConnectAsync(string url, int secondsToRetry = 0, CancellationToken ct = default)
    {
        Connected = true;
        return Task.FromResult(true);
    }

    public Task<string> RegisterAsync(string serviceName, List<string> solvables, string machineId = null, string sessionId = null, CancellationToken ct = default)
    {
        LastRegisteredService = serviceName;
        LastSolvables = solvables;
        return Task.FromResult("session-123");
    }

    public void Disconnect() => Connected = false;

    public void Send(string jsonMessage) => SentMessages.Add(jsonMessage);

    public Task<string> SendRequestAsync(string jsonMessage, int cookie, int timeoutMs = 30000, CancellationToken ct = default)
    {
        SentMessages.Add(jsonMessage);
        return Task.FromResult("\"ok\"");
    }

    public void SimulateMessage(string json) => OnMessage?.Invoke(json);

    public void Dispose() { }
}

[TestFixture]
public class StpRecognizerConnectionTests
{
    [Test]
    public async Task ConnectAndRegister_Succeeds()
    {
        var connector = new StubConnector();
        using var recognizer = new StpRecognizer(connector);
        recognizer.OnSymbolAdded += (p, s, u) => { };

        string session = await recognizer.ConnectAndRegisterAsync("TestAgent");
        Assert.That(session, Is.EqualTo("session-123"));
        Assert.That(recognizer.IsConnected, Is.True);
    }

    [Test]
    public void Disconnect_SetsNotConnected()
    {
        var connector = new StubConnector();
        using var recognizer = new StpRecognizer(connector);
        connector.ConnectAsync("ws://localhost:9599").Wait();
        Assert.That(recognizer.IsConnected, Is.True);

        recognizer.Disconnect();
        Assert.That(recognizer.IsConnected, Is.False);
    }

    [Test]
    public async Task Register_OnlyIncludesSubscribedEvents()
    {
        var connector = new StubConnector();
        using var recognizer = new StpRecognizer(connector);

        recognizer.OnSymbolAdded += (p, s, u) => { };
        recognizer.OnTaskAdded += (p, t, tp, u) => { };

        await recognizer.ConnectAndRegisterAsync("TestAgent");

        Assert.That(connector.LastSolvables, Does.Contain("SymbolAdded"));
        Assert.That(connector.LastSolvables, Does.Contain("TaskAdded"));
        Assert.That(connector.LastSolvables, Does.Not.Contain("CoaAdded"));
    }

    [Test]
    public void Constructor_NullConnector_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new StpRecognizer(null));
    }
}

[TestFixture]
public class StpRecognizerDispatchTests
{
    private StubConnector _connector;
    private StpRecognizer _recognizer;

    [SetUp]
    public void Setup()
    {
        _connector = new StubConnector();
        _recognizer = new StpRecognizer(_connector);
    }

    [TearDown]
    public void TearDown()
    {
        _recognizer.Dispose();
    }

    [Test]
    public void SymbolAdded_DispatchesCorrectly()
    {
        string receivedPoid = null;
        StpItem receivedSymbol = null;
        _recognizer.OnSymbolAdded += (poid, symbol, isUndo) =>
        {
            receivedPoid = poid;
            receivedSymbol = symbol;
        };

        string json = @"{
            ""method"": ""SymbolAdded"",
            ""params"": {
                ""alternates"": [
                    { ""fsTYPE"": ""unit"", ""poid"": ""sym-1"", ""sidc"": ""SFGPUCI----D---"", ""affiliation"": ""friend"" },
                    { ""fsTYPE"": ""unit"", ""poid"": ""sym-1"", ""sidc"": ""SFGPUCA----D---"", ""affiliation"": ""friend"" }
                ],
                ""isUndo"": false
            }
        }";

        _connector.SimulateMessage(json);

        Assert.That(receivedPoid, Is.EqualTo("sym-1"));
        Assert.That(receivedSymbol, Is.Not.Null);
        Assert.That(receivedSymbol.Alternates, Has.Count.EqualTo(1));
    }

    [Test]
    public void SymbolDeleted_DispatchesCorrectly()
    {
        string deletedPoid = null;
        _recognizer.OnSymbolDeleted += (poid, isUndo) => deletedPoid = poid;

        _connector.SimulateMessage(@"{
            ""method"": ""SymbolDeleted"",
            ""params"": { ""poid"": ""sym-99"", ""isUndo"": false }
        }");

        Assert.That(deletedPoid, Is.EqualTo("sym-99"));
    }

    [Test]
    public void TaskAdded_DispatchesWithTaskPoids()
    {
        List<string> receivedTaskPoids = null;
        _recognizer.OnTaskAdded += (poid, task, taskPoids, isUndo) =>
        {
            receivedTaskPoids = taskPoids;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""TaskAdded"",
            ""params"": {
                ""poid"": ""task-1"",
                ""alternates"": [{ ""fsTYPE"": ""task"", ""poid"": ""task-1"", ""name"": ""Attack"" }],
                ""taskPoids"": [""tg-1"", ""tg-2""],
                ""isUndo"": false
            }
        }");

        Assert.That(receivedTaskPoids, Is.Not.Null);
        Assert.That(receivedTaskPoids, Has.Count.EqualTo(2));
    }

    [Test]
    public void StpMessage_DispatchesLevelAndMessage()
    {
        StpRecognizer.StpMessageLevel? receivedLevel = null;
        string receivedMsg = null;
        _recognizer.OnStpMessage += (level, msg) =>
        {
            receivedLevel = level;
            receivedMsg = msg;
        };

        _connector.SimulateMessage(@"{
            ""method"": ""StpMessage"",
            ""params"": { ""message"": ""test info"", ""level"": ""Info"" }
        }");

        Assert.That(receivedLevel, Is.EqualTo(StpRecognizer.StpMessageLevel.Info));
        Assert.That(receivedMsg, Is.EqualTo("test info"));
    }

    [Test]
    public void CoaSwitched_DispatchesCoa()
    {
        StpCoa receivedCoa = null;
        _recognizer.OnCoaSwitched += (coa) => receivedCoa = coa;

        _connector.SimulateMessage(@"{
            ""method"": ""CoaSwitched"",
            ""params"": { ""coa"": { ""fsTYPE"": ""coa"", ""poid"": ""coa-1"", ""name"": ""COA1"" } }
        }");

        Assert.That(receivedCoa, Is.Not.Null);
        Assert.That(receivedCoa.Name, Is.EqualTo("COA1"));
    }

    [Test]
    public void RoleSwitched_DispatchesRole()
    {
        string receivedRole = null;
        _recognizer.OnRoleSwitched += (role) => receivedRole = role;

        _connector.SimulateMessage(@"{
            ""method"": ""RoleSwitched"",
            ""params"": { ""role"": ""FRIENDLY_CO"" }
        }");

        Assert.That(receivedRole, Is.EqualTo("FRIENDLY_CO"));
    }

    [Test]
    public void SpeechRecognized_DispatchesPhrases()
    {
        List<string> receivedPhrases = null;
        _recognizer.OnSpeechRecognized += (phrases) => receivedPhrases = phrases;

        _connector.SimulateMessage(@"{
            ""method"": ""SpeechRecognized"",
            ""params"": { ""phrases"": [""attack"", ""defend""] }
        }");

        Assert.That(receivedPhrases, Has.Count.EqualTo(2));
        Assert.That(receivedPhrases[0], Is.EqualTo("attack"));
    }

    [Test]
    public void InkProcessed_Dispatches()
    {
        bool fired = false;
        _recognizer.OnInkProcessed += () => fired = true;

        _connector.SimulateMessage(@"{
            ""method"": ""InkProcessed"",
            ""params"": {}
        }");

        Assert.That(fired, Is.True);
    }

    [Test]
    public void UnknownMethod_DoesNotThrow()
    {
        _recognizer.OnStpMessage += (l, m) => { };

        Assert.DoesNotThrow(() =>
        {
            _connector.SimulateMessage(@"{
                ""method"": ""UnknownFutureEvent"",
                ""params"": { ""foo"": ""bar"" }
            }");
        });
    }

    [Test]
    public void MalformedJson_DoesNotThrow()
    {
        _recognizer.OnStpMessage += (l, m) => { };

        Assert.DoesNotThrow(() =>
        {
            _connector.SimulateMessage("this is not json {{{");
        });
    }

    [Test]
    public void ResponseMessage_IsIgnored()
    {
        bool anyEventFired = false;
        _recognizer.OnSymbolAdded += (p, s, u) => anyEventFired = true;
        _recognizer.OnStpMessage += (l, m) => anyEventFired = true;

        _connector.SimulateMessage(@"{ ""result"": ""ok"", ""id"": 1 }");

        Assert.That(anyEventFired, Is.False);
    }
}

[TestFixture]
public class StpRecognizerCommandTests
{
    private StubConnector _connector;
    private StpRecognizer _recognizer;

    [SetUp]
    public void Setup()
    {
        _connector = new StubConnector();
        _recognizer = new StpRecognizer(_connector);
    }

    [TearDown]
    public void TearDown()
    {
        _recognizer.Dispose();
    }

    [Test]
    public void SendPenDown_SendsCorrectMethod()
    {
        _recognizer.SendPenDown(new LatLon(59.14, 10.07), DateTime.UtcNow);

        Assert.That(_connector.SentMessages, Has.Count.EqualTo(1));
        Assert.That(_connector.SentMessages[0], Does.Contain("SendPenDown"));
    }

    [Test]
    public void AddSymbol_SendsCorrectMethod()
    {
        var sym = new StpSymbol { Type = "unit", Poid = "p1" };
        _recognizer.AddSymbol(sym);

        Assert.That(_connector.SentMessages[0], Does.Contain("AddSymbol"));
    }

    [Test]
    public void DeleteSymbol_SendsCorrectPoid()
    {
        _recognizer.DeleteSymbol("poid-to-delete");

        Assert.That(_connector.SentMessages[0], Does.Contain("DeleteSymbol"));
        Assert.That(_connector.SentMessages[0], Does.Contain("poid-to-delete"));
    }

    [Test]
    public void SendInk_SendsAllParameters()
    {
        _recognizer.SendInk(
            new System.Drawing.Size(800, 600),
            new LatLon(59.27, 9.70),
            new LatLon(59.01, 10.44),
            new List<LatLon> { new LatLon(59.14, 10.07), new LatLon(59.15, 10.08) },
            DateTime.UtcNow.AddSeconds(-1),
            DateTime.UtcNow,
            new List<string> { "poid-1" });

        Assert.That(_connector.SentMessages[0], Does.Contain("SendInk"));
        Assert.That(_connector.SentMessages[0], Does.Contain("strokePoints"));
    }

    [Test]
    public void SendSimulatedSpeechRecognition_SendsCorrectMethod()
    {
        _recognizer.SendSimulatedSpeechRecognition("attack position");

        Assert.That(_connector.SentMessages[0], Does.Contain("SendSpeechRecognition"));
    }

    [Test]
    public void AdvertiseViewport_SendsCorrectMethod()
    {
        _recognizer.AdvertiseViewport(new LatLon(59.27, 9.70), new LatLon(59.01, 10.44));

        Assert.That(_connector.SentMessages[0], Does.Contain("AdvertiseViewport"));
    }

    [Test]
    public void ConvertToTranscription_EmptyInput_ReturnsEmpty()
    {
        var result = _recognizer.ConvertToTranscription("");
        Assert.That(result, Has.Count.EqualTo(0));
    }

    [Test]
    public void ConvertToTranscription_NonEmptyInput_ReturnsSingleItem()
    {
        var result = _recognizer.ConvertToTranscription("attack");
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Text, Is.EqualTo("attack"));
        Assert.That(result[0].Confidence, Is.EqualTo(1.0));
    }
}

[TestFixture]
[Category("SmokeTest")]
public class JsonRpcIntegrationSmokeTests
{
    private const int ConnectTimeoutSec = 15;
    private const int EventWaitMs = 30_000;

    [Test]
    public async Task Agent_ConnectAndRegister_Succeeds()
    {
        using var connector = new StpJsonRpcConnector(url: LiveParitySmokeTests.TestStpUrl);
        using var recognizer = new StpRecognizer(connector);

        recognizer.OnSymbolAdded += (poid, symbol, isUndo) => { };
        recognizer.OnSymbolModified += (poid, symbol, isUndo) => { };
        recognizer.OnSymbolDeleted += (poid, isUndo) => { };
        recognizer.OnStpMessage += (level, msg) => { };

        string sessionId = await recognizer.ConnectAndRegisterAsync(
            "JsonRpcSmokeTestAgent",
            secondsToRetry: ConnectTimeoutSec);

        Assert.That(sessionId, Is.Not.Null.And.Not.Empty);
        Assert.That(recognizer.IsConnected, Is.True);

        recognizer.Stop();
    }

    [Test]
    public async Task Agent_AddSymbol_TriggersSymbolAddedEvent()
    {
        using var connector = new StpJsonRpcConnector(url: LiveParitySmokeTests.TestStpUrl);
        using var recognizer = new StpRecognizer(connector);

        StpItem receivedSymbol = null;
        var symbolAddedEvent = new ManualResetEventSlim(false);

        recognizer.OnSymbolAdded += (poid, symbol, isUndo) =>
        {
            receivedSymbol = symbol;
            symbolAddedEvent.Set();
        };
        recognizer.OnStpMessage += (level, msg) => { };

        string sessionId = await recognizer.ConnectAndRegisterAsync(
            "JsonRpcSymbolTestAgent",
            secondsToRetry: ConnectTimeoutSec);
        Assert.That(sessionId, Is.Not.Null.And.Not.Empty);

        recognizer.AdvertiseViewport(
            new LatLon(59.27, 9.70),
            new LatLon(59.01, 10.44));

        var symbol = new StpSymbol
        {
            Type = "unit",
            SymbolId = "SFGPUCI----E---",
            Affiliation = Affiliation.friend,
            Location = new Location
            {
                Type = "point",
                Shape = "point",
                Coords = new List<LatLon> { new LatLon(59.14, 10.07) },
                Centroid = new LatLon(59.14, 10.07)
            },
            Geometry = "point"
        };

        recognizer.AddSymbol(symbol);

        bool eventFired = symbolAddedEvent.Wait(EventWaitMs);
        Assert.That(eventFired, Is.True);
        Assert.That(receivedSymbol, Is.Not.Null);

        recognizer.Stop();
    }
}
