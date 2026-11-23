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

// Contract 0.9.11 (STP-1265): every object event carries a trailing nullable batchId, the change set
// it arrived in (null outside one, or from an engine that predates change sets). The delegates above
// are unchanged so existing handlers keep compiling; the *Ext delegates below carry batchId as one
// more trailing argument, the pattern SymbolEditExtDelegate / CommandExtDelegate already use.
// Attach either, or both: the engine event is subscribed when any one is attached.
public delegate void SymbolAddedExtDelegate(string poid, StpItem stpSymbol, bool isUndo, string batchId);
public delegate void SymbolModifiedExtDelegate(string poid, StpItem stpSymbol, bool isUndo, string batchId);
public delegate void SymbolDeletedExtDelegate(string poid, bool isUndo, string batchId);

public delegate void TaskAddedExtDelegate(string poid, StpTask stpTask, List<string> taskPoids, bool isUndo, string batchId);
public delegate void TaskModifiedExtDelegate(string poid, StpTask stpTask, List<string> taskPoids, bool isUndo, string batchId);
public delegate void TaskDeletedExtDelegate(string poid, bool isUndo, string batchId);

public delegate void TaskOrgAddedExtDelegate(string poid, StpTaskOrg stpTaskOrg, bool isUndo, string batchId);
public delegate void TaskOrgModifiedExtDelegate(string poid, StpTaskOrg stpTaskOrg, bool isUndo, string batchId);
public delegate void TaskOrgDeletedExtDelegate(string poid, bool isUndo, string batchId);
public delegate void TaskOrgUnitAddedExtDelegate(string poid, StpTaskOrgUnit stpTaskOrgUnit, bool isUndo, string batchId);
public delegate void TaskOrgUnitModifiedExtDelegate(string poid, StpTaskOrgUnit stpTaskOrgUnit, bool isUndo, string batchId);
public delegate void TaskOrgUnitDeletedExtDelegate(string poid, bool isUndo, string batchId);
public delegate void TaskOrgRelationshipAddedExtDelegate(string poid, StpTaskOrgRelationship stpTaskOrgRelationship, bool isUndo, string batchId);
public delegate void TaskOrgRelationshipModifiedExtDelegate(string poid, StpTaskOrgRelationship stpTaskOrgRelationship, bool isUndo, string batchId);
public delegate void TaskOrgRelationshipDeletedExtDelegate(string poid, bool isUndo, string batchId);

public delegate void CoaAddedExtDelegate(string poid, StpCoa stpCoa, bool isUndo, string batchId);
public delegate void CoaModifiedExtDelegate(string poid, StpCoa stpCoa, bool isUndo, string batchId);
public delegate void CoaDeletedExtDelegate(string poid, bool isUndo, string batchId);

// Task history (STP-1250) and change sets (STP-1265), contract 0.9.11.
public delegate void TaskEventAddedDelegate(string poid, TaskHistoryEntry entry, string batchId);
public delegate void TaskEventModifiedDelegate(string poid, TaskHistoryEntry entry, string batchId);
public delegate void BatchStartedDelegate(string batchId, string kind, int expected, bool isOwn, string origin);
public delegate void BatchEndedDelegate(string batchId, string kind, string status, int informs, bool isOwn, string origin);

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

/// <summary><see cref="StpRecognizer.WhenTaskEventAdded"/> / <see cref="StpRecognizer.WhenTaskEventModified"/>: one task history entry.</summary>
public class TaskEventEventArgs
{
    /// <summary>The entry's poid.</summary>
    public string Poid { get; }
    public TaskHistoryEntry Entry { get; }
    /// <summary>The change set it arrived in, or null.</summary>
    public string BatchId { get; }
    public TaskEventEventArgs(string poid, TaskHistoryEntry entry, string batchId) { Poid = poid; Entry = entry; BatchId = batchId; }
}

/// <summary><see cref="StpRecognizer.WhenBatchStarted"/>: a change set opened.</summary>
public class BatchStartedEventArgs
{
    public string BatchId { get; }
    public string Kind { get; }
    /// <summary>-1 when the requester did not say.</summary>
    public int Expected { get; }
    public bool IsOwn { get; }
    public string Origin { get; }
    public BatchStartedEventArgs(string batchId, string kind, int expected, bool isOwn, string origin)
    { BatchId = batchId; Kind = kind; Expected = expected; IsOwn = isOwn; Origin = origin; }
}

/// <summary><see cref="StpRecognizer.WhenBatchEnded"/>: a change set closed.</summary>
public class BatchEndedEventArgs
{
    public string BatchId { get; }
    public string Kind { get; }
    public string Status { get; }
    public int Informs { get; }
    public bool IsOwn { get; }
    public string Origin { get; }
    public BatchEndedEventArgs(string batchId, string kind, string status, int informs, bool isOwn, string origin)
    { BatchId = batchId; Kind = kind; Status = status; Informs = informs; IsOwn = isOwn; Origin = origin; }
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

    /// <summary>SymbolAdded with its trailing batchId (contract 0.9.11). Fires after <see cref="OnSymbolAdded"/>.</summary>
    public event SymbolAddedExtDelegate OnSymbolAddedExt;
    /// <summary>SymbolModified with its trailing batchId (contract 0.9.11). Fires after <see cref="OnSymbolModified"/>.</summary>
    public event SymbolModifiedExtDelegate OnSymbolModifiedExt;
    /// <summary>SymbolDeleted with its trailing batchId (contract 0.9.11). Fires after <see cref="OnSymbolDeleted"/>.</summary>
    public event SymbolDeletedExtDelegate OnSymbolDeletedExt;
    /// <summary>TaskAdded with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskAdded"/>.</summary>
    public event TaskAddedExtDelegate OnTaskAddedExt;
    /// <summary>TaskModified with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskModified"/>.</summary>
    public event TaskModifiedExtDelegate OnTaskModifiedExt;
    /// <summary>TaskDeleted with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskDeleted"/>.</summary>
    public event TaskDeletedExtDelegate OnTaskDeletedExt;
    /// <summary>TaskOrgAdded with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskOrgAdded"/>.</summary>
    public event TaskOrgAddedExtDelegate OnTaskOrgAddedExt;
    /// <summary>TaskOrgModified with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskOrgModified"/>.</summary>
    public event TaskOrgModifiedExtDelegate OnTaskOrgModifiedExt;
    /// <summary>TaskOrgDeleted with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskOrgDeleted"/>.</summary>
    public event TaskOrgDeletedExtDelegate OnTaskOrgDeletedExt;
    /// <summary>TaskOrgUnitAdded with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskOrgUnitAdded"/>.</summary>
    public event TaskOrgUnitAddedExtDelegate OnTaskOrgUnitAddedExt;
    /// <summary>TaskOrgUnitModified with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskOrgUnitModified"/>.</summary>
    public event TaskOrgUnitModifiedExtDelegate OnTaskOrgUnitModifiedExt;
    /// <summary>TaskOrgUnitDeleted with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskOrgUnitDeleted"/>.</summary>
    public event TaskOrgUnitDeletedExtDelegate OnTaskOrgUnitDeletedExt;
    /// <summary>TaskOrgRelationshipAdded with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskOrgRelationshipAdded"/>.</summary>
    public event TaskOrgRelationshipAddedExtDelegate OnTaskOrgRelationshipAddedExt;
    /// <summary>TaskOrgRelationshipModified with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskOrgRelationshipModified"/>.</summary>
    public event TaskOrgRelationshipModifiedExtDelegate OnTaskOrgRelationshipModifiedExt;
    /// <summary>TaskOrgRelationshipDeleted with its trailing batchId (contract 0.9.11). Fires after <see cref="OnTaskOrgRelationshipDeleted"/>.</summary>
    public event TaskOrgRelationshipDeletedExtDelegate OnTaskOrgRelationshipDeletedExt;
    /// <summary>CoaAdded with its trailing batchId (contract 0.9.11). Fires after <see cref="OnCoaAdded"/>.</summary>
    public event CoaAddedExtDelegate OnCoaAddedExt;
    /// <summary>CoaModified with its trailing batchId (contract 0.9.11). Fires after <see cref="OnCoaModified"/>.</summary>
    public event CoaModifiedExtDelegate OnCoaModifiedExt;
    /// <summary>CoaDeleted with its trailing batchId (contract 0.9.11). Fires after <see cref="OnCoaDeleted"/>.</summary>
    public event CoaDeletedExtDelegate OnCoaDeletedExt;

    /// <summary>
    /// A task's history gained an entry (STP-1250): an edit, retask, confirm, delete, undo - by any
    /// client - or a comment. Wire event <c>TaskEventAdded</c>.
    /// </summary>
    public event TaskEventAddedDelegate OnTaskEventAdded;
    /// <summary>
    /// A comment in a task's history was edited or deleted by its author (STP-1250 S3); the entry is
    /// sent whole, a deleted comment as its tombstone. Wire event <c>TaskEventModified</c>.
    /// </summary>
    public event TaskEventModifiedDelegate OnTaskEventModified;
    /// <summary>
    /// A change set began (STP-1265). Every object event it produces carries its batchId. Do not wait
    /// for it before handling objects: an engine that predates change sets sends none.
    /// Wire event <c>BatchStarted</c>; <c>expected</c> is -1 when the requester did not say.
    /// </summary>
    public event BatchStartedDelegate OnBatchStarted;
    /// <summary>
    /// A change set ended (STP-1265), after its last object event. <c>status</c> is completed,
    /// cancelled, failed, abandoned or expired; <c>informs</c> is 0 when unknown. Wire event <c>BatchEnded</c>.
    /// </summary>
    public event BatchEndedDelegate OnBatchEnded;

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

    /// <summary><see cref="OnTaskEventAdded"/> as a stream (wire event <c>TaskEventAdded</c>).</summary>
    public IObservable<TaskEventEventArgs> WhenTaskEventAdded =>
        Observable.FromEvent<TaskEventAddedDelegate, TaskEventEventArgs>(
            handler => (poid, entry, batchId) => handler(new TaskEventEventArgs(poid, entry, batchId)),
            h => OnTaskEventAdded += h,
            h => OnTaskEventAdded -= h);

    /// <summary><see cref="OnTaskEventModified"/> as a stream (wire event <c>TaskEventModified</c>).</summary>
    public IObservable<TaskEventEventArgs> WhenTaskEventModified =>
        Observable.FromEvent<TaskEventModifiedDelegate, TaskEventEventArgs>(
            handler => (poid, entry, batchId) => handler(new TaskEventEventArgs(poid, entry, batchId)),
            h => OnTaskEventModified += h,
            h => OnTaskEventModified -= h);

    /// <summary><see cref="OnBatchStarted"/> as a stream (wire event <c>BatchStarted</c>).</summary>
    public IObservable<BatchStartedEventArgs> WhenBatchStarted =>
        Observable.FromEvent<BatchStartedDelegate, BatchStartedEventArgs>(
            handler => (batchId, kind, expected, isOwn, origin) => handler(new BatchStartedEventArgs(batchId, kind, expected, isOwn, origin)),
            h => OnBatchStarted += h,
            h => OnBatchStarted -= h);

    /// <summary><see cref="OnBatchEnded"/> as a stream (wire event <c>BatchEnded</c>).</summary>
    public IObservable<BatchEndedEventArgs> WhenBatchEnded =>
        Observable.FromEvent<BatchEndedDelegate, BatchEndedEventArgs>(
            handler => (batchId, kind, status, informs, isOwn, origin) => handler(new BatchEndedEventArgs(batchId, kind, status, informs, isOwn, origin)),
            h => OnBatchEnded += h,
            h => OnBatchEnded -= h);

    #endregion
}
