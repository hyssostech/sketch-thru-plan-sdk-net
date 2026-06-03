using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;

namespace StpSDK;

#region Delegates

public delegate void SymbolAddedDelegate(string poid, StpItem stpSymbol, bool isUndo);
public delegate void SymbolModifiedDelegate(string poid, StpItem stpSymbol, bool isUndo);
public delegate void SymbolDeletedDelegate(string poid, bool isUndo);
public delegate void SymboReportDelegate(string poid, StpItem stpSymbol);

public delegate void SymbolEditDelegate(string operation, Location location);
public delegate void SymbolEditExtDelegate(string operation, Location location, Dictionary<string, string> properties);

public delegate void TaskAddeddDelegate(string poid, StpTask stpTask, List<string> taskPoids, bool isUndo);
public delegate void TaskModifiedDelegate(string poid, StpTask stpTask, List<string> taskPoids, bool isUndo);
public delegate void TaskDeletedDelegate(string poid, bool isUndo);

public delegate void TaskOrgAddeddDelegate(string poid, StpTaskOrg stpTaskOrg, bool isUndo);
public delegate void TaskOrgModifiedDelegate(string poid, StpTaskOrg stpTaskOrg, bool isUndo);
public delegate void TaskOrgDeletedDelegate(string poid, bool isUndo);
public delegate void TaskOrgUnitAddeddDelegate(string poid, StpTaskOrgUnit stpTaskOrgUnit, bool isUndo);
public delegate void TaskOrgUnitModifiedDelegate(string poid, StpTaskOrgUnit stpTaskOrgUnit, bool isUndo);
public delegate void TaskOrgUnitDeletedDelegate(string poid, bool isUndo);
public delegate void TaskOrgRelationshipAddeddDelegate(string poid, StpTaskOrgRelationship stpTaskOrgRelationship, bool isUndo);
public delegate void TaskOrgRelationshipModifiedDelegate(string poid, StpTaskOrgRelationship stpTaskOrgRelationship, bool isUndo);
public delegate void TaskOrgRelationshipDeletedDelegate(string poid, bool isUndo);

public delegate void StpMessageDelegate(StpMessageLevel level, string msg);
public delegate void CommandDelegate(string operation, Location location);
public delegate void CommandExtDelegate(string operation, Location location, Dictionary<string, string> properties);
public delegate void MapOperationDelegate(string operation, Location location);
public delegate void MapOperationExtDelegate(string operation, Location location, Dictionary<string, string> properties);

public delegate void CoaAddedDelegate(string poid, StpCoa stpCoa, bool isUndo);
public delegate void CoaModifiedDelegate(string poid, StpCoa stpCoa, bool isUndo);
public delegate void CoaDeletedDelegate(string poid, bool isUndo);
public delegate void CoaSwitchDelegate(StpCoa coa);

public delegate void RoleSwitchDelegate(string role);
public delegate void TaskOrgSwitchDelegate(StpTaskOrg taskOrg);
public delegate void AutoTaskingSwitchedDelegate(bool isEnabled);

public delegate void SpeechRecognitionDelegate(List<string> speechList);
public delegate void SpeechParsedDelegate(List<SpeechRecoItem> parsedAlternates);
public delegate void ListeningStateChangedDelegate(bool isListening);
public delegate void ListenDelegate(Auth auth, ListenMode mode, DateTime time);

public delegate void SketchRecognizedDelegate(List<SketchRecoResult> sketchList);
public delegate void SketchIntegratedDelegate();
public delegate void SketchDiscardedDelegate();
public delegate void SpeechIntegratedDelegate();
public delegate void SpeechDiscardedDelegate();

public delegate void PenDownUpDelegate(DateTime time, LatLon coord);
public delegate void NewScenarioDelegate();

public delegate void InkProcessedDelegate();
public delegate void ShutdownDelegate();

#endregion

#region EventArgs

public class StpMessageEventArgs
{
    public StpMessageLevel Level { get; }
    public string Message { get; }
    public StpMessageEventArgs(StpMessageLevel level, string message) { Level = level; Message = message; }
}

public class SymboReportEventArgs
{
    public string Poid { get; }
    public StpItem Symbol { get; }
    public SymboReportEventArgs(string poid, StpItem symbol) { Poid = poid; Symbol = symbol; }
}

public class SymbolEditEventArgs
{
    public string Operation { get; }
    public Location Location { get; }
    public SymbolEditEventArgs(string operation, Location location) { Operation = operation; Location = location; }
}

public class SymbolEditExtEventArgs
{
    public string Operation { get; }
    public Location Location { get; }
    public Dictionary<string, string> Properties { get; }
    public SymbolEditExtEventArgs(string operation, Location location, Dictionary<string, string> properties) { Operation = operation; Location = location; Properties = properties; }
}

public class CommandEventArgs
{
    public string Operation { get; }
    public Location Location { get; }
    public CommandEventArgs(string operation, Location location) { Operation = operation; Location = location; }
}

public class CommandExtEventArgs
{
    public string Operation { get; }
    public Location Location { get; }
    public Dictionary<string, string> Properties { get; }
    public CommandExtEventArgs(string operation, Location location, Dictionary<string, string> properties) { Operation = operation; Location = location; Properties = properties; }
}

public class MapOperationEventArgs
{
    public string Operation { get; }
    public Location Location { get; }
    public MapOperationEventArgs(string operation, Location location) { Operation = operation; Location = location; }
}

public class MapOperationExtEventArgs
{
    public string Operation { get; }
    public Location Location { get; }
    public Dictionary<string, string> Properties { get; }
    public MapOperationExtEventArgs(string operation, Location location, Dictionary<string, string> properties) { Operation = operation; Location = location; Properties = properties; }
}

public class NewScenarioEventArgs
{
    public NewScenarioEventArgs() { }
}

public class CoaSwitchedEventArgs
{
    public StpCoa Coa { get; }
    public CoaSwitchedEventArgs(StpCoa coa) { Coa = coa; }
}

public class RoleSwitchEventArgs
{
    public string Role { get; }
    public RoleSwitchEventArgs(string role) { Role = role; }
}

public class TaskOrgSwitchEventArgs
{
    public StpTaskOrg TaskOrg { get; }
    public TaskOrgSwitchEventArgs(StpTaskOrg taskOrg) { TaskOrg = taskOrg; }
}

public class AutoTaskingSwitchEventArgs
{
    public bool IsEnabled { get; }
    public AutoTaskingSwitchEventArgs(bool isEnabled) { IsEnabled = isEnabled; }
}

public class SpeechRecognitionEventArgs
{
    public List<string> SpeechList { get; }
    public SpeechRecognitionEventArgs(List<string> speechList) { SpeechList = speechList; }
}

public class SpeechParsedEventArgs
{
    public List<SpeechRecoItem> ParsedAlternates { get; }
    public SpeechParsedEventArgs(List<SpeechRecoItem> parsedAlternates) { ParsedAlternates = parsedAlternates; }
}

public class ListeningStateChangedEventArgs
{
    public bool IsListening { get; }
    public ListeningStateChangedEventArgs(bool isListening) { IsListening = isListening; }
}

public class ListenEventArgs
{
    public Auth Auth { get; }
    public ListenMode Mode { get; }
    public DateTime Time { get; }
    public ListenEventArgs(Auth auth, ListenMode mode, DateTime time) { Auth = auth; Mode = mode; Time = time; }
}

public class SketchRecognitionEventArgs
{
    public List<SketchRecoResult> SketchList { get; }
    public SketchRecognitionEventArgs(List<SketchRecoResult> sketchList) { SketchList = sketchList; }
}

public class PenDownUpEventArgs
{
    public DateTime Time { get; }
    public LatLon Coord { get; }
    public PenDownUpEventArgs(DateTime time, LatLon coord) { Time = time; Coord = coord; }
}

public class StpConnectionErrorEventArgs
{
    public string Message { get; }
    public bool StpDisabled { get; }
    public Exception Exception { get; }
    public StpConnectionErrorEventArgs(string message, bool stpDisabled, Exception exception) { Message = message; StpDisabled = stpDisabled; Exception = exception; }
}

#endregion

public partial class StpRecognizer
{
    /// <summary>
    /// Severity level of a message issued by STP (see <see cref="OnStpMessage"/>).
    /// Nested in <see cref="StpRecognizer"/> for source compatibility with prior SDK versions.
    /// </summary>
    public enum StpMessageLevel { Error, Warning, Info, Verbose, Debug }

    #region Events

    public event SymbolAddedDelegate OnSymbolAdded;
    public event SymbolModifiedDelegate OnSymbolModified;
    public event SymbolDeletedDelegate OnSymbolDeleted;
    public event SymboReportDelegate OnSymbolReport;

    public event SymbolEditDelegate OnSymbolEdited;
    public event SymbolEditExtDelegate OnSymbolEditedExt;

    public event TaskAddeddDelegate OnTaskAdded;
    public event TaskModifiedDelegate OnTaskModified;
    public event TaskDeletedDelegate OnTaskDeleted;

    public event TaskOrgAddeddDelegate OnTaskOrgAdded;
    public event TaskOrgModifiedDelegate OnTaskOrgModified;
    public event TaskOrgDeletedDelegate OnTaskOrgDeleted;
    public event TaskOrgUnitAddeddDelegate OnTaskOrgUnitAdded;
    public event TaskOrgUnitModifiedDelegate OnTaskOrgUnitModified;
    public event TaskOrgUnitDeletedDelegate OnTaskOrgUnitDeleted;
    public event TaskOrgRelationshipAddeddDelegate OnTaskOrgRelationshipAdded;
    public event TaskOrgRelationshipModifiedDelegate OnTaskOrgRelationshipModified;
    public event TaskOrgRelationshipDeletedDelegate OnTaskOrgRelationshipDeleted;

    public event StpMessageDelegate OnStpMessage;
    public event CommandDelegate OnCommand;
    public event CommandExtDelegate OnCommandExt;
    public event MapOperationDelegate OnMapOperation;
    public event MapOperationExtDelegate OnMapOperationExt;

    public event CoaAddedDelegate OnCoaAdded;
    public event CoaModifiedDelegate OnCoaModified;
    public event CoaDeletedDelegate OnCoaDeleted;
    public event CoaSwitchDelegate OnCoaSwitched;

    public event RoleSwitchDelegate OnRoleSwitched;
    public event TaskOrgSwitchDelegate OnTaskOrgSwitched;
    public event AutoTaskingSwitchedDelegate OnAutoTaskingSwitched;

    public event SpeechRecognitionDelegate OnSpeechRecognized;
    public event SpeechParsedDelegate OnSpeechParsed;
    public event ListeningStateChangedDelegate OnListeningStateChanged;
    public event ListenDelegate OnListen;

    public event SketchRecognizedDelegate OnSketchRecognized;
    public event SketchIntegratedDelegate OnSketchIntegrated;
    public event SketchDiscardedDelegate OnSketchDiscarded;
    public event SpeechIntegratedDelegate OnSpeechIntegrated;
    public event SpeechDiscardedDelegate OnSpeechDiscarded;

    public event PenDownUpDelegate OnPenDown;
    public event PenDownUpDelegate OnPenUp;
    public event NewScenarioDelegate OnNewScenario;

    public event InkProcessedDelegate OnInkProcessed;
    public event ShutdownDelegate OnShutdown;
    public event StpConnectionErrorDelegate OnConnectionError;

    #endregion

    #region IObservable streams

    public IObservable<StpMessageEventArgs> WhenStpMessage =>
        Observable.FromEvent<StpMessageDelegate, StpMessageEventArgs>(
            handler => (level, msg) => handler(new StpMessageEventArgs(level, msg)),
            h => OnStpMessage += h,
            h => OnStpMessage -= h);

    public IObservable<SymboReportEventArgs> WhenSymbolReport =>
        Observable.FromEvent<SymboReportDelegate, SymboReportEventArgs>(
            handler => (poid, symbol) => handler(new SymboReportEventArgs(poid, symbol)),
            h => OnSymbolReport += h,
            h => OnSymbolReport -= h);

    public IObservable<SymbolEditEventArgs> WhenSymbolEdit =>
        Observable.FromEvent<SymbolEditDelegate, SymbolEditEventArgs>(
            handler => (operation, location) => handler(new SymbolEditEventArgs(operation, location)),
            h => OnSymbolEdited += h,
            h => OnSymbolEdited -= h);

    public IObservable<SymbolEditExtEventArgs> WhenSymbolEditExt =>
        Observable.FromEvent<SymbolEditExtDelegate, SymbolEditExtEventArgs>(
            handler => (operation, location, props) => handler(new SymbolEditExtEventArgs(operation, location, props)),
            h => OnSymbolEditedExt += h,
            h => OnSymbolEditedExt -= h);

    public IObservable<CommandEventArgs> WhenCommand =>
        Observable.FromEvent<CommandDelegate, CommandEventArgs>(
            handler => (operation, location) => handler(new CommandEventArgs(operation, location)),
            h => OnCommand += h,
            h => OnCommand -= h);

    public IObservable<CommandExtEventArgs> WhenCommandExt =>
        Observable.FromEvent<CommandExtDelegate, CommandExtEventArgs>(
            handler => (operation, location, props) => handler(new CommandExtEventArgs(operation, location, props)),
            h => OnCommandExt += h,
            h => OnCommandExt -= h);

    public IObservable<MapOperationEventArgs> WhenMapOperation =>
        Observable.FromEvent<MapOperationDelegate, MapOperationEventArgs>(
            handler => (operation, location) => handler(new MapOperationEventArgs(operation, location)),
            h => OnMapOperation += h,
            h => OnMapOperation -= h);

    public IObservable<MapOperationExtEventArgs> WhenMapOperationExt =>
        Observable.FromEvent<MapOperationExtDelegate, MapOperationExtEventArgs>(
            handler => (operation, location, props) => handler(new MapOperationExtEventArgs(operation, location, props)),
            h => OnMapOperationExt += h,
            h => OnMapOperationExt -= h);

    public IObservable<NewScenarioEventArgs> WhenNewScenario =>
        Observable.FromEvent<NewScenarioDelegate, NewScenarioEventArgs>(
            handler => () => handler(new NewScenarioEventArgs()),
            h => OnNewScenario += h,
            h => OnNewScenario -= h);

    public IObservable<CoaSwitchedEventArgs> WhenCoaSwitched =>
        Observable.FromEvent<CoaSwitchDelegate, CoaSwitchedEventArgs>(
            handler => (coa) => handler(new CoaSwitchedEventArgs(coa)),
            h => OnCoaSwitched += h,
            h => OnCoaSwitched -= h);

    public IObservable<RoleSwitchEventArgs> WhenRoleSwitched =>
        Observable.FromEvent<RoleSwitchDelegate, RoleSwitchEventArgs>(
            handler => (role) => handler(new RoleSwitchEventArgs(role)),
            h => OnRoleSwitched += h,
            h => OnRoleSwitched -= h);

    public IObservable<TaskOrgSwitchEventArgs> WhenTaskOrgSwitched =>
        Observable.FromEvent<TaskOrgSwitchDelegate, TaskOrgSwitchEventArgs>(
            handler => (taskOrg) => handler(new TaskOrgSwitchEventArgs(taskOrg)),
            h => OnTaskOrgSwitched += h,
            h => OnTaskOrgSwitched -= h);

    public IObservable<AutoTaskingSwitchEventArgs> WhenAutoTaskingSwitched =>
        Observable.FromEvent<AutoTaskingSwitchedDelegate, AutoTaskingSwitchEventArgs>(
            handler => (isEnabled) => handler(new AutoTaskingSwitchEventArgs(isEnabled)),
            h => OnAutoTaskingSwitched += h,
            h => OnAutoTaskingSwitched -= h);

    public IObservable<SpeechRecognitionEventArgs> WhenSpeechRecognized =>
        Observable.FromEvent<SpeechRecognitionDelegate, SpeechRecognitionEventArgs>(
            handler => (speechList) => handler(new SpeechRecognitionEventArgs(speechList)),
            h => OnSpeechRecognized += h,
            h => OnSpeechRecognized -= h);

    public IObservable<SpeechParsedEventArgs> WhenSpeechParsed =>
        Observable.FromEvent<SpeechParsedDelegate, SpeechParsedEventArgs>(
            handler => (parsedAlternates) => handler(new SpeechParsedEventArgs(parsedAlternates)),
            h => OnSpeechParsed += h,
            h => OnSpeechParsed -= h);

    public IObservable<ListeningStateChangedEventArgs> WhenListeningStateChanged =>
        Observable.FromEvent<ListeningStateChangedDelegate, ListeningStateChangedEventArgs>(
            handler => (isListening) => handler(new ListeningStateChangedEventArgs(isListening)),
            h => OnListeningStateChanged += h,
            h => OnListeningStateChanged -= h);

    public IObservable<ListenEventArgs> WhenListenRaised =>
        Observable.FromEvent<ListenDelegate, ListenEventArgs>(
            handler => (auth, mode, time) => handler(new ListenEventArgs(auth, mode, time)),
            h => OnListen += h,
            h => OnListen -= h);

    public IObservable<SketchRecognitionEventArgs> WhenSketchRecognized =>
        Observable.FromEvent<SketchRecognizedDelegate, SketchRecognitionEventArgs>(
            handler => (sketchList) => handler(new SketchRecognitionEventArgs(sketchList)),
            h => OnSketchRecognized += h,
            h => OnSketchRecognized -= h);

    public IObservable<Unit> WhenSketchIntegrated =>
        Observable.FromEvent<SketchIntegratedDelegate, Unit>(
            handler => () => handler(Unit.Default),
            h => OnSketchIntegrated += h,
            h => OnSketchIntegrated -= h);

    public IObservable<Unit> WhenSketchDiscarded =>
        Observable.FromEvent<SketchDiscardedDelegate, Unit>(
            handler => () => handler(Unit.Default),
            h => OnSketchDiscarded += h,
            h => OnSketchDiscarded -= h);

    public IObservable<Unit> WhenSpeechIntegrated =>
        Observable.FromEvent<SpeechIntegratedDelegate, Unit>(
            handler => () => handler(Unit.Default),
            h => OnSpeechIntegrated += h,
            h => OnSpeechIntegrated -= h);

    public IObservable<Unit> WhenSpeechDiscarded =>
        Observable.FromEvent<SpeechDiscardedDelegate, Unit>(
            handler => () => handler(Unit.Default),
            h => OnSpeechDiscarded += h,
            h => OnSpeechDiscarded -= h);

    public IObservable<PenDownUpEventArgs> WhenPenDown =>
        Observable.FromEvent<PenDownUpDelegate, PenDownUpEventArgs>(
            handler => (time, coord) => handler(new PenDownUpEventArgs(time, coord)),
            h => OnPenDown += h,
            h => OnPenDown -= h);

    public IObservable<PenDownUpEventArgs> WhenPenUp =>
        Observable.FromEvent<PenDownUpDelegate, PenDownUpEventArgs>(
            handler => (time, coord) => handler(new PenDownUpEventArgs(time, coord)),
            h => OnPenUp += h,
            h => OnPenUp -= h);

    public IObservable<StpConnectionErrorEventArgs> WhenConnectionError =>
        Observable.FromEvent<StpConnectionErrorDelegate, StpConnectionErrorEventArgs>(
            handler => (msg, disabled, ex) => handler(new StpConnectionErrorEventArgs(msg, disabled, ex)),
            h => _connector.OnConnectionError += h,
            h => _connector.OnConnectionError -= h);

    public IObservable<Unit> WhenShutingdown =>
        Observable.FromEvent<ShutdownDelegate, Unit>(
            handler => () => handler(Unit.Default),
            h => OnShutdown += h,
            h => OnShutdown -= h);

    #endregion
}
