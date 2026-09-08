using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StpSDK;

public enum CoaAffiliation { FRIENDLY, ENEMY }
public enum C2CIMDataType { All, Initialization, Order }

public partial class StpRecognizer : IDisposable
{
    private readonly IStpConnector _connector;
    private int _cookie;
    private const int DefaultTimeoutMs = 30000;

    // Captured from the most recent RegisterAsync call (including the one made by
    // ConnectAndRegisterAsync) so RefreshSubscriptionsAsync can re-register with the same
    // identity after a caller attaches a new handler post-connect.
    private string _registeredAgentName;
    private string _registeredMachineId;
    private string _registeredSession;

    public bool IsConnected => _connector?.Connected ?? false;
    public bool IsStandAloneEngine { get; private set; }

    public static string JMSSVGPath { get; set; }

    public StpRecognizer(IStpConnector stpConnector)
    {
        _connector = stpConnector ?? throw new ArgumentNullException(nameof(stpConnector));
        _connector.OnMessage += HandleMessage;
        _connector.OnConnectionError += (msg, disabled, ex) => OnConnectionError?.Invoke(msg, disabled, ex);
    }

    #region Connection

    public async Task<string> ConnectAndRegisterAsync(
        string agentName,
        string machineId = null,
        string session = null,
        bool exitAppIfNoConnection = true,
        int secondsToRetry = 0,
        string url = null,
        CancellationToken cancellationToken = default)
    {
        bool connected = await ConnectAsync(secondsToRetry, url, cancellationToken).ConfigureAwait(false);
        if (!connected)
        {
            if (exitAppIfNoConnection)
                throw new StpCommunicationException("Failed to connect to STP engine");
            return null;
        }
        return await RegisterAsync(agentName, machineId, session, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> ConnectAsync(int secondsToRetry = 0, string url = null, CancellationToken cancellationToken = default)
    {
        string connectUrl = url
            ?? (_connector as StpJsonRpcConnector)?.Url
            ?? "ws://localhost:9599";
        return _connector.ConnectAsync(connectUrl, secondsToRetry, cancellationToken);
    }

    public async Task<string> RegisterAsync(
        string appName,
        string machineId = null,
        string session = null,
        CancellationToken cancellationToken = default)
    {
        _registeredAgentName = appName;
        _registeredMachineId = machineId;
        _registeredSession = session;

        var solvables = BuildSolvables();
        return await _connector.RegisterAsync(appName, solvables, machineId, session, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Subscriptions are fixed at connect time: <see cref="RegisterAsync"/> (and therefore
    /// <see cref="ConnectAndRegisterAsync"/>) computes the solvables list once, from whichever
    /// On&lt;Event&gt; handlers are attached at that moment, and the engine will only ever route
    /// those events to this connection. If you attach a handler (for example
    /// <c>recognizer.OnSymbolAdded += handler;</c>) AFTER connecting, the engine keeps routing
    /// exactly the events it saw at register time - the new handler is silently never invoked,
    /// even though <see cref="IsConnected"/> still reports true. Call
    /// <see cref="RefreshSubscriptionsAsync"/> immediately after attaching any handler post-connect
    /// to rebuild the solvables list from the current handler set and re-register it with the engine.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the re-registration request.</param>
    /// <returns>The session identifier returned by the engine for the refreshed registration.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the connection is not open, or if this connection was registered through
    /// <see cref="RegisterEventsAsync"/>, which supplies an explicit event list that this method
    /// would discard. The two cases carry different messages.
    /// </exception>
    public async Task<string> RefreshSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException(
                "RefreshSubscriptionsAsync requires an open connection: call ConnectAndRegisterAsync " +
                "(or ConnectAsync followed by RegisterAsync) first.");
        if (_registeredAgentName == null)
            throw new InvalidOperationException(
                "RefreshSubscriptionsAsync cannot refresh this registration. It rebuilds the subscription list " +
                "from the attached On<Event> handlers, so it applies only to a registration made through " +
                "RegisterAsync or ConnectAndRegisterAsync. This connection was registered through " +
                "RegisterEventsAsync, which takes an explicit event list, and refreshing would silently " +
                "discard it. Call RegisterEventsAsync again with the events you want instead.");

        var solvables = BuildSolvables();
        return await _connector.RegisterAsync(_registeredAgentName, solvables, _registeredMachineId, _registeredSession, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> RegisterEventsAsync(
        string appName,
        List<string> events,
        string machineId = null,
        string session = null,
        CancellationToken cancellationToken = default)
    {
        return await _connector.RegisterAsync(appName, events, machineId, session, cancellationToken).ConfigureAwait(false);
    }

    public void Disconnect()
    {
        _connector.Disconnect();
    }

    public void Stop()
    {
        Disconnect();
    }

    private List<string> BuildSolvables()
    {
        var solvables = new List<string>();

        if (OnSymbolAdded != null) solvables.Add("SymbolAdded");
        if (OnSymbolModified != null) solvables.Add("SymbolModified");
        if (OnSymbolDeleted != null) solvables.Add("SymbolDeleted");
        if (OnSymbolReport != null) solvables.Add("SymbolReport");
        if (OnSymbolEdited != null || OnSymbolEditedExt != null) solvables.Add("SymbolEdited");
        if (OnTaskAdded != null) solvables.Add("TaskAdded");
        if (OnTaskModified != null) solvables.Add("TaskModified");
        if (OnTaskDeleted != null) solvables.Add("TaskDeleted");
        if (OnTaskOrgAdded != null) solvables.Add("TaskOrgAdded");
        if (OnTaskOrgModified != null) solvables.Add("TaskOrgModified");
        if (OnTaskOrgDeleted != null) solvables.Add("TaskOrgDeleted");
        if (OnTaskOrgUnitAdded != null) solvables.Add("TaskOrgUnitAdded");
        if (OnTaskOrgUnitModified != null) solvables.Add("TaskOrgUnitModified");
        if (OnTaskOrgUnitDeleted != null) solvables.Add("TaskOrgUnitDeleted");
        if (OnTaskOrgRelationshipAdded != null) solvables.Add("TaskOrgRelationshipAdded");
        if (OnTaskOrgRelationshipModified != null) solvables.Add("TaskOrgRelationshipModified");
        if (OnTaskOrgRelationshipDeleted != null) solvables.Add("TaskOrgRelationshipDeleted");
        if (OnTaskOrgSwitched != null) solvables.Add("TaskOrgSwitched");
        if (OnCoaAdded != null) solvables.Add("CoaAdded");
        if (OnCoaModified != null) solvables.Add("CoaModified");
        if (OnCoaDeleted != null) solvables.Add("CoaDeleted");
        if (OnCoaSwitched != null) solvables.Add("CoaSwitched");
        if (OnRoleSwitched != null) solvables.Add("RoleSwitched");
        if (OnAutoTaskingSwitched != null) solvables.Add("AutoTaskingSwitched");
        if (OnStpMessage != null) solvables.Add("StpMessage");
        if (OnCommand != null || OnCommandExt != null) solvables.Add("Command");
        if (OnMapOperation != null || OnMapOperationExt != null) solvables.Add("MapOperation");
        if (OnSpeechRecognized != null) solvables.Add("SpeechRecognized");
        if (OnSpeechParsed != null) solvables.Add("SpeechParsed");
        if (OnListeningStateChanged != null) solvables.Add("AudioCapture");
        if (OnListen != null) solvables.Add("Listen");
        if (OnSketchRecognized != null) solvables.Add("SketchRecognized");
        if (OnSketchIntegrated != null) solvables.Add("SketchIntegrated");
        if (OnSketchDiscarded != null) solvables.Add("SketchDiscarded");
        if (OnSpeechIntegrated != null) solvables.Add("SpeechIntegrated");
        if (OnSpeechDiscarded != null) solvables.Add("SpeechDiscarded");
        if (OnPenDown != null) solvables.Add("PenDown");
        if (OnPenUp != null) solvables.Add("PenUp");
        if (OnInkProcessed != null) solvables.Add("InkProcessed");
        if (OnNewScenario != null) solvables.Add("NewScenario");
        if (OnShutdown != null) solvables.Add("Shutdown");

        return solvables;
    }

    #endregion

    #region Message Dispatch

    private void HandleMessage(string json)
    {
        try
        {
            var msg = JObject.Parse(json);

            if (msg["result"] != null || msg["error"] != null)
                return;

            string method = msg["method"]?.ToString();
            if (string.IsNullOrEmpty(method))
                return;

            var p = msg["params"];
            if (p == null)
                return;

            switch (method)
            {
                case "SymbolAdded": HandleSymbolAdded(p); break;
                case "SymbolModified": HandleSymbolModified(p); break;
                case "SymbolDeleted": HandleSymbolDeleted(p); break;
                case "SymbolReport": HandleSymbolReport(p); break;
                case "SymbolEdited": HandleSymbolEdited(p); break;
                case "TaskAdded": HandleTaskAdded(p); break;
                case "TaskModified": HandleTaskModified(p); break;
                case "TaskDeleted": HandleTaskDeleted(p); break;
                case "TaskOrgAdded": HandleTaskOrgAdded(p); break;
                case "TaskOrgModified": HandleTaskOrgModified(p); break;
                case "TaskOrgDeleted": HandleTaskOrgDeleted(p); break;
                case "TaskOrgUnitAdded": HandleTaskOrgUnitAdded(p); break;
                case "TaskOrgUnitModified": HandleTaskOrgUnitModified(p); break;
                case "TaskOrgUnitDeleted": HandleTaskOrgUnitDeleted(p); break;
                case "TaskOrgRelationshipAdded": HandleTaskOrgRelationshipAdded(p); break;
                case "TaskOrgRelationshipModified": HandleTaskOrgRelationshipModified(p); break;
                case "TaskOrgRelationshipDeleted": HandleTaskOrgRelationshipDeleted(p); break;
                case "TaskOrgSwitched": HandleTaskOrgSwitched(p); break;
                case "CoaAdded": HandleCoaAdded(p); break;
                case "CoaModified": HandleCoaModified(p); break;
                case "CoaDeleted": HandleCoaDeleted(p); break;
                case "CoaSwitched": HandleCoaSwitched(p); break;
                case "RoleSwitched": HandleRoleSwitched(p); break;
                case "AutoTaskingSwitched": HandleAutoTaskingSwitched(p); break;
                case "StpMessage": HandleStpMessage(p); break;
                case "Command": HandleCommand(p); break;
                case "MapOperation": HandleMapOperation(p); break;
                case "SpeechRecognized": HandleSpeechRecognized(p); break;
                case "SpeechParsed": HandleSpeechParsed(p); break;
                case "AudioCapture": HandleAudioCapture(p); break;
                case "Listen": HandleListen(p); break;
                case "SketchRecognized": HandleSketchRecognized(p); break;
                case "SketchIntegrated": OnSketchIntegrated?.Invoke(); break;
                case "SketchDiscarded": OnSketchDiscarded?.Invoke(); break;
                case "SpeechIntegrated": OnSpeechIntegrated?.Invoke(); break;
                case "SpeechDiscarded": OnSpeechDiscarded?.Invoke(); break;
                case "PenDown": HandlePenDown(p); break;
                case "PenUp": HandlePenUp(p); break;
                case "InkProcessed": OnInkProcessed?.Invoke(); break;
                case "NewScenario": OnNewScenario?.Invoke(); break;
                case "Shutdown": OnShutdown?.Invoke(); break;
                default:
                    Log(StpMessageLevel.Debug, $"Unhandled method: {method}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log(StpMessageLevel.Error, $"Error handling message: {ex.Message}");
        }
    }

    #endregion

    #region Event Handlers

    private void HandleSymbolAdded(JToken p)
    {
        var alternates = p["alternates"]?.ToObject<List<StpSymbol>>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (alternates == null || alternates.Count == 0) return;

        var primary = alternates[0];
        if (alternates.Count > 1)
            primary.Alternates = alternates.Skip(1).Cast<StpItem>().ToList();

        OnSymbolAdded?.Invoke(primary.Poid, primary, isUndo);
    }

    private void HandleSymbolModified(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var symbol = p["symbol"]?.ToObject<StpSymbol>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (poid == null || symbol == null) return;

        OnSymbolModified?.Invoke(poid, symbol, isUndo);
    }

    private void HandleSymbolDeleted(JToken p)
    {
        string poid = p["poid"]?.ToString();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (poid == null) return;

        OnSymbolDeleted?.Invoke(poid, isUndo);
    }

    private void HandleSymbolReport(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var symbol = p["symbol"]?.ToObject<StpSymbol>();
        if (poid == null || symbol == null) return;

        OnSymbolReport?.Invoke(poid, symbol);
    }

    private void HandleSymbolEdited(JToken p)
    {
        string operation = p["operation"]?.ToString();
        var location = p["location"]?.ToObject<Location>();

        OnSymbolEdited?.Invoke(operation, location);

        var properties = p["properties"]?.ToObject<Dictionary<string, string>>();
        OnSymbolEditedExt?.Invoke(operation, location, properties);
    }

    private void HandleTaskAdded(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var alternates = p["alternates"]?.ToObject<List<StpTask>>();
        var taskPoids = p["taskPoids"]?.ToObject<List<string>>() ?? new List<string>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (alternates == null || alternates.Count == 0) return;

        var primary = alternates[0];
        if (alternates.Count > 1)
            primary.Alternates = alternates.Skip(1).Cast<StpItem>().ToList();

        OnTaskAdded?.Invoke(poid ?? primary.Poid, primary, taskPoids, isUndo);
    }

    private void HandleTaskModified(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var alternates = p["alternates"]?.ToObject<List<StpTask>>();
        var taskPoids = p["taskPoids"]?.ToObject<List<string>>() ?? new List<string>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (alternates == null || alternates.Count == 0) return;

        var primary = alternates[0];
        if (alternates.Count > 1)
            primary.Alternates = alternates.Skip(1).Cast<StpItem>().ToList();

        OnTaskModified?.Invoke(poid ?? primary.Poid, primary, taskPoids, isUndo);
    }

    private void HandleTaskDeleted(JToken p)
    {
        string poid = p["poid"]?.ToString();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (poid == null) return;

        OnTaskDeleted?.Invoke(poid, isUndo);
    }

    private void HandleTaskOrgAdded(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var taskOrg = p["taskOrg"]?.ToObject<StpTaskOrg>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (taskOrg == null) return;

        OnTaskOrgAdded?.Invoke(poid ?? taskOrg.Poid, taskOrg, isUndo);
    }

    private void HandleTaskOrgModified(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var taskOrg = p["taskOrg"]?.ToObject<StpTaskOrg>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (poid == null || taskOrg == null) return;

        OnTaskOrgModified?.Invoke(poid, taskOrg, isUndo);
    }

    private void HandleTaskOrgDeleted(JToken p)
    {
        string poid = p["poid"]?.ToString();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (poid == null) return;

        OnTaskOrgDeleted?.Invoke(poid, isUndo);
    }

    private void HandleTaskOrgUnitAdded(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var unit = p["toUnit"]?.ToObject<StpTaskOrgUnit>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (unit == null) return;

        OnTaskOrgUnitAdded?.Invoke(poid ?? unit.Poid, unit, isUndo);
    }

    private void HandleTaskOrgUnitModified(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var unit = p["toUnit"]?.ToObject<StpTaskOrgUnit>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (poid == null || unit == null) return;

        OnTaskOrgUnitModified?.Invoke(poid, unit, isUndo);
    }

    private void HandleTaskOrgUnitDeleted(JToken p)
    {
        string poid = p["poid"]?.ToString();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (poid == null) return;

        OnTaskOrgUnitDeleted?.Invoke(poid, isUndo);
    }

    private void HandleTaskOrgRelationshipAdded(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var rel = p["toRelationship"]?.ToObject<StpTaskOrgRelationship>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (rel == null) return;

        OnTaskOrgRelationshipAdded?.Invoke(poid ?? rel.Poid, rel, isUndo);
    }

    private void HandleTaskOrgRelationshipModified(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var rel = p["toRelationship"]?.ToObject<StpTaskOrgRelationship>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (poid == null || rel == null) return;

        OnTaskOrgRelationshipModified?.Invoke(poid, rel, isUndo);
    }

    private void HandleTaskOrgRelationshipDeleted(JToken p)
    {
        string poid = p["poid"]?.ToString();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (poid == null) return;

        OnTaskOrgRelationshipDeleted?.Invoke(poid, isUndo);
    }

    private void HandleTaskOrgSwitched(JToken p)
    {
        var taskOrg = p["taskOrg"]?.ToObject<StpTaskOrg>();
        OnTaskOrgSwitched?.Invoke(taskOrg);
    }

    private void HandleCoaAdded(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var coa = p["coa"]?.ToObject<StpCoa>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (coa == null) return;

        OnCoaAdded?.Invoke(poid ?? coa.Poid, coa, isUndo);
    }

    private void HandleCoaModified(JToken p)
    {
        string poid = p["poid"]?.ToString();
        var coa = p["coa"]?.ToObject<StpCoa>();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (poid == null || coa == null) return;

        OnCoaModified?.Invoke(poid, coa, isUndo);
    }

    private void HandleCoaDeleted(JToken p)
    {
        string poid = p["poid"]?.ToString();
        bool isUndo = p["isUndo"]?.Value<bool>() ?? false;
        if (poid == null) return;

        OnCoaDeleted?.Invoke(poid, isUndo);
    }

    private void HandleCoaSwitched(JToken p)
    {
        var coa = p["coa"]?.ToObject<StpCoa>();
        OnCoaSwitched?.Invoke(coa);
    }

    private void HandleRoleSwitched(JToken p)
    {
        string role = p["role"]?.ToString();
        OnRoleSwitched?.Invoke(role);
    }

    private void HandleAutoTaskingSwitched(JToken p)
    {
        bool isEnabled = p["isEnabled"]?.Value<bool>() ?? false;
        OnAutoTaskingSwitched?.Invoke(isEnabled);
    }

    private void HandleStpMessage(JToken p)
    {
        string message = p["message"]?.ToString();
        var level = p["level"]?.ToObject<StpMessageLevel>() ?? StpMessageLevel.Info;
        OnStpMessage?.Invoke(level, message);
    }

    private void HandleCommand(JToken p)
    {
        string operation = p["operation"]?.ToString();
        var location = p["location"]?.ToObject<Location>();

        OnCommand?.Invoke(operation, location);

        var properties = p["properties"]?.ToObject<Dictionary<string, string>>();
        OnCommandExt?.Invoke(operation, location, properties);
    }

    private void HandleMapOperation(JToken p)
    {
        string operation = p["operation"]?.ToString();
        var location = p["location"]?.ToObject<Location>();

        OnMapOperation?.Invoke(operation, location);

        var properties = p["properties"]?.ToObject<Dictionary<string, string>>();
        OnMapOperationExt?.Invoke(operation, location, properties);
    }

    private void HandleSpeechRecognized(JToken p)
    {
        var phrases = p["phrases"]?.ToObject<List<string>>();
        if (phrases == null) return;

        OnSpeechRecognized?.Invoke(phrases);
    }

    private void HandleSpeechParsed(JToken p)
    {
        var parsedAlternates = p["alternates"]?.ToObject<List<SpeechRecoItem>>();
        if (parsedAlternates == null) return;

        OnSpeechParsed?.Invoke(parsedAlternates);
    }

    private void HandleAudioCapture(JToken p)
    {
        bool isListening = p["isListening"]?.Value<bool>() ?? false;
        OnListeningStateChanged?.Invoke(isListening);
    }

    private void HandleListen(JToken p)
    {
        var auth = p["auth"]?.ToObject<Auth>();
        var mode = p["mode"]?.ToObject<ListenMode>() ?? ListenMode.off;
        var time = p["time"]?.ToObject<DateTime>() ?? DateTime.UtcNow;
        OnListen?.Invoke(auth, mode, time);
    }

    private void HandleSketchRecognized(JToken p)
    {
        var sketchList = p["sketchList"]?.ToObject<List<SketchRecoResult>>();
        if (sketchList == null) return;

        OnSketchRecognized?.Invoke(sketchList);
    }

    private void HandlePenDown(JToken p)
    {
        var time = p["time"]?.ToObject<DateTime>() ?? DateTime.UtcNow;
        var coord = p["coord"]?.ToObject<LatLon>();
        OnPenDown?.Invoke(time, coord);
    }

    private void HandlePenUp(JToken p)
    {
        var time = p["time"]?.ToObject<DateTime>() ?? DateTime.UtcNow;
        var coord = p["coord"]?.ToObject<LatLon>();
        OnPenUp?.Invoke(time, coord);
    }

    #endregion

    #region Utility

    private int NextCookie() => Interlocked.Increment(ref _cookie);

    private void Log(StpMessageLevel level, string message)
    {
        OnStpMessage?.Invoke(level, message);
    }

    /// <summary>
    /// Wraps text as a single recognition item. Despite the name it performs NO transcription:
    /// numbers and letters are passed through verbatim. The phonetic/number conversion described in
    /// the engine documentation ("A 3 1" -> "alpha three one") happens SERVER-side, so use
    /// <see cref="SendSimulatedSpeechRecognition(string, DateTime?)"/> to get it. No path in this
    /// SDK calls this method any more; it is retained for source compatibility.
    /// </summary>
    public List<SpeechRecoItem> ConvertToTranscription(string typedInput)
    {
        if (string.IsNullOrWhiteSpace(typedInput))
            return new List<SpeechRecoItem>();

        return new List<SpeechRecoItem>
        {
            new SpeechRecoItem(typedInput, 1.0)
        };
    }

    public void Dispose()
    {
        _connector.OnMessage -= HandleMessage;
        _connector?.Dispose();
    }

    #endregion
}
