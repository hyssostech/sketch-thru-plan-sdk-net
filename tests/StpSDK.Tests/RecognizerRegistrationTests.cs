using Microsoft.Extensions.Logging;
using NUnit.Framework;
using StpSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StpSDK.Tests;

internal class RegistrationCapturingConnector : IStpConnector
{
    public event StpMessageReceivedDelegate OnMessage;
    public event StpConnectionErrorDelegate OnConnectionError;

    public ILogger Logger => null;
    public string Name => "reg-stub";
    public string BaseName => "reg-stub";
    public bool Connected { get; set; } = true;

    public List<string> LastSolvables { get; private set; }
    public string LastConnectUrl { get; private set; }
    public bool ConnectResult { get; set; } = true;

    public Task<bool> ConnectAsync(string url, int secondsToRetry = 0, CancellationToken ct = default)
    {
        LastConnectUrl = url;
        Connected = ConnectResult;
        return Task.FromResult(ConnectResult);
    }

    public Task<string> RegisterAsync(string serviceName, List<string> solvables, string machineId = null, string sessionId = null, CancellationToken ct = default)
    {
        LastSolvables = solvables;
        return Task.FromResult("session-reg");
    }

    public void Disconnect() => Connected = false;

    public void Send(string jsonMessage) { }

    public Task<string> SendRequestAsync(string jsonMessage, int cookie, int timeoutMs = 30000, CancellationToken ct = default)
        => Task.FromResult("\"ok\"");

    public void Dispose() { }
}

[TestFixture]
public class RecognizerRegistrationTests
{
    private RegistrationCapturingConnector _connector;
    private StpRecognizer _recognizer;

    [SetUp]
    public void Setup()
    {
        _connector = new RegistrationCapturingConnector();
        _recognizer = new StpRecognizer(_connector);
    }

    [TearDown]
    public void TearDown()
    {
        _recognizer.Dispose();
    }

    [Test]
    public async Task NoSubscribers_RegistersEmptySolvables()
    {
        // No event subscriptions
        await _recognizer.ConnectAndRegisterAsync("TestAgent");

        Assert.That(_connector.LastSolvables, Is.Not.Null);
        Assert.That(_connector.LastSolvables, Is.Empty);
    }

    [Test]
    public async Task AllCoreEvents_RegistersCorrectSolvables()
    {
        _recognizer.OnSymbolAdded += (p, s, u) => { };
        _recognizer.OnSymbolModified += (p, s, u) => { };
        _recognizer.OnSymbolDeleted += (p, u) => { };
        _recognizer.OnTaskAdded += (p, t, tp, u) => { };
        _recognizer.OnStpMessage += (l, m) => { };

        await _recognizer.ConnectAndRegisterAsync("TestAgent");

        var expected = new List<string>
        {
            "SymbolAdded",
            "SymbolModified",
            "SymbolDeleted",
            "TaskAdded",
            "StpMessage"
        };

        Assert.That(_connector.LastSolvables, Is.EquivalentTo(expected));
    }

    [Test]
    public async Task SymbolEditedExt_AlsoRegistersSymbolEdited()
    {
        _recognizer.OnSymbolEditedExt += (op, loc, props) => { };

        await _recognizer.ConnectAndRegisterAsync("TestAgent");

        Assert.That(_connector.LastSolvables, Does.Contain("SymbolEdited"));
    }

    [Test]
    public async Task CommandExt_AlsoRegistersCommand()
    {
        _recognizer.OnCommandExt += (op, loc, props) => { };

        await _recognizer.ConnectAndRegisterAsync("TestAgent");

        Assert.That(_connector.LastSolvables, Does.Contain("Command"));
    }

    [Test]
    public async Task MapOperationExt_AlsoRegistersMapOperation()
    {
        _recognizer.OnMapOperationExt += (op, loc, props) => { };

        await _recognizer.ConnectAndRegisterAsync("TestAgent");

        Assert.That(_connector.LastSolvables, Does.Contain("MapOperation"));
    }

    [Test]
    public async Task AllEvents_ProducesCompleteSolvablesList()
    {
        // Subscribe to all events
        _recognizer.OnSymbolAdded += (p, s, u) => { };
        _recognizer.OnSymbolModified += (p, s, u) => { };
        _recognizer.OnSymbolDeleted += (p, u) => { };
        _recognizer.OnSymbolReport += (p, s) => { };
        _recognizer.OnSymbolEdited += (op, loc) => { };
        _recognizer.OnTaskAdded += (p, t, tp, u) => { };
        _recognizer.OnTaskModified += (p, t, tp, u) => { };
        _recognizer.OnTaskDeleted += (p, u) => { };
        _recognizer.OnTaskOrgAdded += (p, to, u) => { };
        _recognizer.OnTaskOrgModified += (p, to, u) => { };
        _recognizer.OnTaskOrgDeleted += (p, u) => { };
        _recognizer.OnTaskOrgUnitAdded += (p, tou, u) => { };
        _recognizer.OnTaskOrgUnitModified += (p, tou, u) => { };
        _recognizer.OnTaskOrgUnitDeleted += (p, u) => { };
        _recognizer.OnTaskOrgRelationshipAdded += (p, tor, u) => { };
        _recognizer.OnTaskOrgRelationshipModified += (p, tor, u) => { };
        _recognizer.OnTaskOrgRelationshipDeleted += (p, u) => { };
        _recognizer.OnTaskOrgSwitched += (to) => { };
        _recognizer.OnCoaAdded += (p, c, u) => { };
        _recognizer.OnCoaModified += (p, c, u) => { };
        _recognizer.OnCoaDeleted += (p, u) => { };
        _recognizer.OnCoaSwitched += (c) => { };
        _recognizer.OnRoleSwitched += (r) => { };
        _recognizer.OnAutoTaskingSwitched += (e) => { };
        _recognizer.OnStpMessage += (l, m) => { };
        _recognizer.OnCommand += (op, loc) => { };
        _recognizer.OnMapOperation += (op, loc) => { };
        _recognizer.OnSpeechRecognized += (phrases) => { };
        _recognizer.OnSpeechParsed += (alts) => { };
        _recognizer.OnListeningStateChanged += (listening) => { };
        _recognizer.OnListen += (auth, mode, time) => { };
        _recognizer.OnSketchRecognized += (list) => { };
        _recognizer.OnSketchIntegrated += () => { };
        _recognizer.OnSketchDiscarded += () => { };
        _recognizer.OnSpeechIntegrated += () => { };
        _recognizer.OnSpeechDiscarded += () => { };
        _recognizer.OnPenDown += (time, coord) => { };
        _recognizer.OnPenUp += (time, coord) => { };
        _recognizer.OnInkProcessed += () => { };
        _recognizer.OnNewScenario += () => { };
        _recognizer.OnShutdown += () => { };

        await _recognizer.ConnectAndRegisterAsync("TestAgent");

        var expectedSolvables = new List<string>
        {
            "SymbolAdded",
            "SymbolModified",
            "SymbolDeleted",
            "SymbolReport",
            "SymbolEdited",
            "TaskAdded",
            "TaskModified",
            "TaskDeleted",
            "TaskOrgAdded",
            "TaskOrgModified",
            "TaskOrgDeleted",
            "TaskOrgUnitAdded",
            "TaskOrgUnitModified",
            "TaskOrgUnitDeleted",
            "TaskOrgRelationshipAdded",
            "TaskOrgRelationshipModified",
            "TaskOrgRelationshipDeleted",
            "TaskOrgSwitched",
            "CoaAdded",
            "CoaModified",
            "CoaDeleted",
            "CoaSwitched",
            "RoleSwitched",
            "AutoTaskingSwitched",
            "StpMessage",
            "Command",
            "MapOperation",
            "SpeechRecognized",
            "SpeechParsed",
            "AudioCapture",
            "Listen",
            "SketchRecognized",
            "SketchIntegrated",
            "SketchDiscarded",
            "SpeechIntegrated",
            "SpeechDiscarded",
            "PenDown",
            "PenUp",
            "InkProcessed",
            "NewScenario",
            "Shutdown"
        };

        Assert.That(_connector.LastSolvables, Has.Count.EqualTo(expectedSolvables.Count),
            $"Expected {expectedSolvables.Count} solvables but got {_connector.LastSolvables?.Count}");
        Assert.That(_connector.LastSolvables, Is.EquivalentTo(expectedSolvables));
    }

    [Test]
    public async Task RegisterEventsAsync_UsesExplicitList()
    {
        var customEvents = new List<string> { "SymbolAdded", "CustomEvent", "AnotherEvent" };

        await _recognizer.RegisterEventsAsync("TestAgent", customEvents);

        Assert.That(_connector.LastSolvables, Is.EqualTo(customEvents));
    }

    [Test]
    public void ConnectAndRegister_FailedConnection_ThrowsStpCommunicationException()
    {
        _connector.ConnectResult = false;

        Assert.ThrowsAsync<StpCommunicationException>(async () =>
            await _recognizer.ConnectAndRegisterAsync("TestAgent", exitAppIfNoConnection: true));
    }

    [Test]
    public async Task ConnectAndRegister_FailedConnection_ReturnsNull_WhenNotExiting()
    {
        _connector.ConnectResult = false;

        string result = await _recognizer.ConnectAndRegisterAsync("TestAgent", exitAppIfNoConnection: false);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ConnectAndRegister_PassesUrl()
    {
        const string expectedUrl = "ws://myhost:1234";

        _recognizer.OnSymbolAdded += (p, s, u) => { };

        await _recognizer.ConnectAndRegisterAsync("TestAgent", url: expectedUrl);

        Assert.That(_connector.LastConnectUrl, Is.EqualTo(expectedUrl));
    }
}
