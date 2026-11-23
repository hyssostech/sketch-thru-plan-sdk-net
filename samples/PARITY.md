# Sample parity: JS and .NET

Every method and event of the STP contract (`sketch-thru-plan-api.json`, contract 0.9.11), the JavaScript sample
that shows it, and the .NET sample that shows it. The capability names are the contract's, the same names the
JavaScript SDK's table ([samples/PARITY.md](https://github.com/hyssostech/sketch-thru-plan-sdk-js/blob/main/samples/PARITY.md)
in hyssostech/sketch-thru-plan-sdk-js) uses, so the two tables line up row by row.

* **JS sample** - the samples under `samples/` of the JavaScript SDK (`quickstart/js` and `quickstart/ts` are its
  quickstarts); "via" names the higher-level JS call that sends the method
* **.NET sample** - the samples in this folder, and `quickstart`; "via" names the higher-level .NET call the
  sample uses
* **.NET SDK** - the public `StpRecognizer` member that sends the method, or the event to attach. Events with a
  `batchId` also list their `Ext` twin. Where the .NET name differs from the contract's, see
  [contract-name-mapping](../docs/articles/contract-name-mapping.md)
* **none** - no sample of that SDK shows the capability yet

A .NET sample is listed when its code calls the member or attaches the event (designer files are not counted).
Where one .NET method has a text overload and an `ObjectSet` overload, the row names the overload.

## Methods

| Capability (contract method) | JS sample | .NET sample | .NET SDK |
|---|---|---|---|
| SendPenDown | arcgis/react, arcgis/ts, basic, batches, c2sim, commands, objectsets, quickstart/js, quickstart/ts, roles, scenario, session, taskhistory, tasks, to | C2SIMSample, EditingSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample, quickstart | `SendPenDown` |
| SendInk | arcgis/react, arcgis/ts, basic, batches, c2sim, commands, objectsets, quickstart/js, quickstart/ts, roles, scenario, session, taskhistory, tasks, to | C2SIMSample, EditingSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample, quickstart | `SendInk` |
| SendSpeechRecognition | arcgis/react, arcgis/ts, basic, batches, c2sim, commands, objectsets, quickstart/js, quickstart/ts, roles, scenario, session, taskhistory, tasks, to | SpeechSample | `SendSpeechRecognition`, `SendSpeechRecognitionExt` |
| SendSimulatedSpeechRecognition | **none** | **none** | `SendSimulatedSpeechRecognition` |
| CreateNewScenario | arcgis/react, arcgis/ts, batches, c2sim, commands, objectsets, roles, scenario, session, taskhistory, tasks, to | C2SIMSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample | `CreateNewScenarioAsync` |
| LoadNewScenario | arcgis/react, arcgis/ts, batches, c2sim, commands, scenario, session | C2SIMSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample | `LoadNewScenarioAsync(string)` |
| ImportPlanData | batches | C2SIMSample | `ImportSTPDataAsync(string)` |
| GetScenarioContent | arcgis/react, arcgis/ts, batches, c2sim, commands, scenario, session | C2SIMSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample | `GetScenarioContentAsync` |
| JoinScenarioSession | arcgis/react, arcgis/ts, batches, c2sim, commands, objectsets, scenario, session, taskhistory | C2SIMSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample | `JoinScenarioSessionAsync` |
| SyncScenarioSession | arcgis/react, arcgis/ts, batches, c2sim, commands, scenario, session | **none** | `SyncScenarioSessionAsync(string)` |
| HasActiveScenario | arcgis/react, arcgis/ts, batches, c2sim, commands, objectsets, scenario, session, taskhistory | C2SIMSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample | `HasActiveScenarioAsync` |
| AddSymbol | **none** | **none** | `AddSymbol` |
| UpdateSymbol | **none** | C2SIMSample, EditingSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample | `UpdateSymbol` |
| DeleteSymbol | arcgis/react, arcgis/ts, basic, c2sim, commands, quickstart/js, quickstart/ts, roles, scenario, session, tasks, to | C2SIMSample, EditingSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample | `DeleteSymbol` |
| ChooseAlternate | **none** | C2SIMSample, EditingSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample | `ChooseAlternate` |
| ImportTaskOrgContent | arcgis/react, arcgis/ts, c2sim, commands, roles, scenario, session, to | C2SIMSample, RolesSample, SessionSample, TaskOrgSample | `ImportTaskOrgContentAsync` |
| GetTaskOrgContent | arcgis/react, arcgis/ts, c2sim, commands, roles, scenario, session, to | C2SIMSample, RolesSample, SessionSample, TaskOrgSample | `GetTaskOrgContentAsync` |
| SetDefaultTaskOrg | arcgis/react, arcgis/ts, c2sim, commands, roles, scenario, session, to | C2SIMSample, RolesSample, SessionSample, TaskOrgSample | `SetDefaultTaskOrgAsync` |
| ResetDefaultTaskOrg | **none** | TaskOrgSample | `ResetDefaultTaskOrgAsync` |
| AddTaskOrg | **none** | **none** | `AddTaskOrg` |
| UpdateTaskOrg | **none** | **none** | `UpdateTaskOrg` |
| DeleteTaskOrg | **none** | **none** | `DeleteTaskOrg` |
| AddTaskOrgUnit | **none** | **none** | `AddTaskOrgUnit` |
| UpdateTaskOrgUnit | **none** | **none** | `UpdateTaskOrgUnit` |
| DeleteTaskOrgUnit | **none** | **none** | `DeleteTaskOrgUnit` |
| AddTaskOrgRelationship | **none** | **none** | `AddTaskOrgRelationship` |
| UpdateTaskOrgRelationship | **none** | **none** | `UpdateTaskOrgRelationship` |
| DeleteTaskOrgRelationship | **none** | **none** | `DeleteTaskOrgRelationship` |
| AddTask | **none** | **none** | `AddTask` |
| UpdateTask | **none** | C2SIMSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample | `UpdateTask` |
| DeleteTask | **none** | C2SIMSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample | `DeleteTask` |
| ConfirmTask | **none** | C2SIMSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample | `ConfirmTask` |
| GetTaskHistory | taskhistory | TaskingSample | `GetTaskHistoryAsync` |
| AddTaskComment | taskhistory | TaskingSample | `AddTaskCommentAsync` |
| EditTaskComment | taskhistory | TaskingSample | `EditTaskCommentAsync` |
| DeleteTaskComment | taskhistory | TaskingSample | `DeleteTaskCommentAsync` |
| SetCoaTaskOrg (limited in STP 5.12) | **none** | **none** | `SetCoaTaskOrgAsync` |
| ResetCoaTaskOrg (limited in STP 5.12) | **none** | **none** | `ResetCoaTaskOrgAsync` |
| ImportCoaContent (limited in STP 5.12) | **none** | **none** | `ImportCoaContentAsync` |
| GetCoaContent (limited in STP 5.12) | **none** | **none** | `GetCoaContentAsync` |
| SetCurrentCoa (limited in STP 5.12) | **none** | **none** | `SetCoaAsync` |
| AddCoa (limited in STP 5.12) | **none** | **none** | `AddCoa` |
| UpdateCoa (limited in STP 5.12) | **none** | **none** | `UpdateCoa` |
| DeleteCoa (limited in STP 5.12) | **none** | **none** | `DeleteCoa`, `DeleteCoaAsync` |
| SetRole | arcgis/react, arcgis/ts, c2sim, commands, roles, scenario, session | C2SIMSample, RolesSample, SessionSample | `SetRoleAsync` |
| ConvertC2SIMContent | arcgis/react (via importInitializationFromC2SIMServer), arcgis/ts (via importInitializationFromC2SIMServer), c2sim (via importInitializationFromC2SIMServer) | C2SIMSample | `ConvertC2SIMContentAsync` |
| GetC2SIMContent | arcgis/react (via exportPlanDataToC2SIMServer), arcgis/ts (via exportPlanDataToC2SIMServer), c2sim (via exportPlanDataToC2SIMServer) | C2SIMSample | `GetC2SIMContentAsync` |
| GetCoaObjectSet (limited in STP 5.12) | objectsets | **none** | `GetCoaObjectSetAsync` |
| GetScenarioObjectSet | objectsets | C2SIMSample, RolesSample, ScenarioSample, SessionSample, TaskOrgSample | `GetScenarioObjectSetContentAsync` |
| GetTaskOrgObjectSet | objectsets | TaskOrgSample | `GetTaskOrgObjectSetAsync` |
| ImportCoaFromObjectSet (limited in STP 5.12) | objectsets | **none** | `ImportCoaAsync(ObjectSet)`, `ImportCoaAsync(ObjectSet, TimeSpan)` |
| ImportPlanDataFromObjectSet | objectsets | ScenarioSample | `ImportSTPDataAsync(ObjectSet)`, `ImportSTPDataAsync(ObjectSet, TimeSpan)` |
| ImportTaskOrgFromObjectSet | objectsets | TaskOrgSample | `ImportTaskOrgAsync(ObjectSet)`, `ImportTaskOrgAsync(ObjectSet, TimeSpan)` |
| LoadNewScenarioFromObjectSet | objectsets | ScenarioSample | `LoadNewScenarioAsync(ObjectSet)`, `LoadNewScenarioAsync(ObjectSet, TimeSpan)` |
| PullC2SIMInitialization | arcgis/react (via importInitializationFromC2SIMServer), arcgis/ts (via importInitializationFromC2SIMServer), c2sim (via importInitializationFromC2SIMServer) | C2SIMSample | `PullC2SIMInitializationAsync`, `PullC2SIMInitializationContentAsync` |
| PushC2SIMContent | arcgis/react (via exportPlanDataToC2SIMServer), arcgis/ts (via exportPlanDataToC2SIMServer), c2sim (via exportPlanDataToC2SIMServer) | C2SIMSample | `PushC2SIMContentAsync` |
| Register (transport) | arcgis/react, arcgis/ts, basic, batches, c2sim, commands, objectsets, quickstart/js, quickstart/ts, roles, scenario, session, symbolsearch, taskhistory, tasks, to | C2SIMSample (via ConnectAndRegisterAsync), DotNetFrameworkSample (via ConnectAndRegisterAsync), EditingSample (via ConnectAndRegisterAsync), quickstart (via ConnectAndRegisterAsync), ReactiveSample (via ConnectAndRegisterAsync), RolesSample (via ConnectAndRegisterAsync), ScenarioSample (via ConnectAndRegisterAsync), SessionSample (via ConnectAndRegisterAsync), SpeechSample (via ConnectAndRegisterAsync), TaskingSample (via ConnectAndRegisterAsync), TaskOrgSample (via ConnectAndRegisterAsync) | `RegisterAsync` |
| Request (transport) | every sample (transport) | every sample (transport) | - |
| SyncScenarioSessionFromObjectSet | objectsets | ScenarioSample | `SyncScenarioSessionAsync(ObjectSet)`, `SyncScenarioSessionAsync(ObjectSet, TimeSpan)` |
| ChangeTimeOut | **none** | C2SIMSample, EditingSample, ReactiveSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample, quickstart | `ChangeTimeOut` |
| ResetSegmentationTimeout | **none** | **none** | `ResetSegmentationTimeout` |
| SetSpeechListening | **none** | **none** | `SetSpeechListening` |
| RecognizeNow | **none** | **none** | `RecognizeNow` |
| SendListen | **none** | **none** | `SendListen` |
| SendAudioCaptureState | **none** | **none** | `SendAudioCaptureState` |
| AdvertiseViewport | **none** | **none** | `AdvertiseViewport` |
| SetAutoTasking | **none** | TaskingSample | `SetAutoTasking` |
| ResetRole | **none** | **none** | `ResetRole` |
| UndoLastOp | **none** | **none** | `UndoLastOp` |
| ResetStpScenario | **none** | TaskingSample | `ResetStpScenarioAsync` |
| GetActiveScenarioDescription | **none** | **none** | `GetActiveScenarioDescriptionAsync` |
| GetAllObjects | **none** | **none** | `RequestAllStpObjectsAsync` |
| GetDeletedObjects | **none** | **none** | `RequestDeletedStpObjectsAsync` |
| GetPoidObject | **none** | **none** | `RequestPoidObjectAsync` |
| GetScenarioTaskOrgList | **none** | TaskOrgSample | `GetScenarioTaskOrgsAsync` |
| GetTaskOrgObjects | **none** | **none** | `GetTaskOrgObjectsAsync` |
| SearchSymbol (client) | symbolsearch | EditingSample | `SymbolSearch.Search` |

## Events

| Capability (contract event) | JS sample | .NET sample | .NET SDK |
|---|---|---|---|
| InkProcessed | arcgis/react, arcgis/ts, basic, batches, c2sim, commands, objectsets, quickstart/js, quickstart/ts, roles, scenario, session, taskhistory, tasks, to | **none** | `OnInkProcessed` |
| SpeechRecognized | arcgis/react, arcgis/ts, basic, batches, c2sim, commands, objectsets, quickstart/js, quickstart/ts, roles, scenario, session, taskhistory, tasks, to | C2SIMSample, DotNetFrameworkSample, EditingSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample, quickstart | `OnSpeechRecognized` |
| SpeechDiscarded | **none** | **none** | `OnSpeechDiscarded` |
| SpeechParsed | **none** | **none** | `OnSpeechParsed` |
| NewScenario | **none** | **none** | `OnNewScenario` |
| SymbolAdded | arcgis/react, arcgis/ts, basic, batches, c2sim, commands, objectsets, quickstart/js, quickstart/ts, roles, scenario, session, taskhistory, tasks, to | C2SIMSample, DotNetFrameworkSample, EditingSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample, quickstart | `OnSymbolAdded`, `OnSymbolAddedExt` |
| SymbolModified | arcgis/react, arcgis/ts, basic, batches, c2sim, commands, objectsets, quickstart/js, quickstart/ts, roles, scenario, session, taskhistory, tasks, to | C2SIMSample, DotNetFrameworkSample, EditingSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample, quickstart | `OnSymbolModified`, `OnSymbolModifiedExt` |
| SymbolDeleted | arcgis/react, arcgis/ts, basic, batches, c2sim, commands, objectsets, quickstart/js, quickstart/ts, roles, scenario, session, taskhistory, tasks, to | C2SIMSample, DotNetFrameworkSample, EditingSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample, quickstart | `OnSymbolDeleted`, `OnSymbolDeletedExt` |
| SymbolReport | arcgis/react, arcgis/ts, c2sim | C2SIMSample | `OnSymbolReport` |
| TaskOrgAdded | arcgis/react, arcgis/ts, batches, c2sim, commands, objectsets, roles, scenario, session, to | C2SIMSample, RolesSample, ScenarioSample, SessionSample, TaskOrgSample | `OnTaskOrgAdded`, `OnTaskOrgAddedExt` |
| TaskOrgModified | arcgis/react, arcgis/ts, batches, c2sim, commands, roles, scenario, session, to | C2SIMSample, RolesSample, SessionSample, TaskOrgSample | `OnTaskOrgModified` |
| TaskOrgDeleted | arcgis/react, arcgis/ts, batches, c2sim, commands, roles, scenario, session, to | C2SIMSample, RolesSample, SessionSample, TaskOrgSample | `OnTaskOrgDeleted` |
| TaskOrgUnitAdded | arcgis/react, arcgis/ts, batches, c2sim, commands, objectsets, roles, scenario, session, to | C2SIMSample, RolesSample, ScenarioSample, SessionSample, TaskOrgSample | `OnTaskOrgUnitAdded`, `OnTaskOrgUnitAddedExt` |
| TaskOrgUnitModified | arcgis/react, arcgis/ts, batches, c2sim, commands, roles, scenario, session, to | C2SIMSample, RolesSample, SessionSample, TaskOrgSample | `OnTaskOrgUnitModified` |
| TaskOrgUnitDeleted | arcgis/react, arcgis/ts, batches, c2sim, commands, roles, scenario, session, to | C2SIMSample, RolesSample, SessionSample, TaskOrgSample | `OnTaskOrgUnitDeleted` |
| TaskOrgRelationshipAdded | arcgis/react, arcgis/ts, batches, c2sim, commands, objectsets, roles, scenario, session, to | C2SIMSample, RolesSample, ScenarioSample, SessionSample, TaskOrgSample | `OnTaskOrgRelationshipAdded`, `OnTaskOrgRelationshipAddedExt` |
| TaskOrgRelationshipModified | arcgis/react, arcgis/ts, batches, c2sim, commands, roles, scenario, session, to | C2SIMSample, RolesSample, SessionSample, TaskOrgSample | `OnTaskOrgRelationshipModified` |
| TaskOrgRelationshipDeleted | arcgis/react, arcgis/ts, batches, c2sim, commands, roles, scenario, session, to | C2SIMSample, RolesSample, SessionSample, TaskOrgSample | `OnTaskOrgRelationshipDeleted` |
| TaskOrgSwitched | arcgis/react, arcgis/ts, c2sim, commands, roles, scenario, session, to | C2SIMSample, RolesSample, SessionSample, TaskOrgSample | `OnTaskOrgSwitched` |
| TaskAdded | arcgis/react, arcgis/ts, batches, c2sim, commands, objectsets, roles, scenario, session, taskhistory, tasks, to | C2SIMSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample | `OnTaskAdded`, `OnTaskAddedExt` |
| TaskModified | arcgis/react, arcgis/ts, batches, c2sim, commands, roles, scenario, session, taskhistory, tasks, to | C2SIMSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample | `OnTaskModified`, `OnTaskModifiedExt` |
| TaskDeleted | arcgis/react, arcgis/ts, batches, c2sim, commands, objectsets, roles, scenario, session, taskhistory, tasks, to | C2SIMSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample | `OnTaskDeleted`, `OnTaskDeletedExt` |
| TaskEventAdded | batches, taskhistory | TaskingSample | `OnTaskEventAdded` |
| TaskEventModified | batches, taskhistory | TaskingSample | `OnTaskEventModified` |
| SymbolEdited | arcgis/react, arcgis/ts, c2sim, commands | C2SIMSample, DotNetFrameworkSample, EditingSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample | `OnSymbolEdited` |
| MapOperation | arcgis/react, arcgis/ts, c2sim, commands | C2SIMSample, DotNetFrameworkSample, EditingSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample | `OnMapOperation` |
| Command | arcgis/react, arcgis/ts, c2sim, commands | C2SIMSample, EditingSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample | `OnCommand` |
| CoaAdded (limited in STP 5.12) | batches, objectsets | ScenarioSample | `OnCoaAdded`, `OnCoaAddedExt` |
| CoaModified (limited in STP 5.12) | batches | **none** | `OnCoaModified`, `OnCoaModifiedExt` |
| CoaDeleted (limited in STP 5.12) | batches | ScenarioSample | `OnCoaDeleted`, `OnCoaDeletedExt` |
| CoaSwitched | **none** | **none** | `OnCoaSwitched` |
| RoleSwitched | arcgis/react, arcgis/ts, c2sim, commands, roles, scenario, session | C2SIMSample, RolesSample, SessionSample | `OnRoleSwitched` |
| BatchStarted | batches | ScenarioSample | `OnBatchStarted` |
| BatchEnded | batches | ScenarioSample | `OnBatchEnded` |
| StpMessage | arcgis/react, arcgis/ts, basic, batches, c2sim, commands, objectsets, quickstart/js, quickstart/ts, roles, scenario, session, taskhistory, tasks, to | C2SIMSample, DotNetFrameworkSample, EditingSample, RolesSample, ScenarioSample, SessionSample, SpeechSample, TaskOrgSample, TaskingSample, quickstart | `OnStpMessage` |
| RequestResponse | every sample (transport) | every sample (transport) | - |
