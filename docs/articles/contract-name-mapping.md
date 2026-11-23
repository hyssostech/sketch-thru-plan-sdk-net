# Contract method names and their .NET names

The public contract (`sketch-thru-plan-api.json`, the OpenRPC file published with the JavaScript
SDK) names every call by its wire method. Most .NET methods are that name plus `Async`
(`GetTaskHistory` is `GetTaskHistoryAsync`), and most events are `On` plus the name
(`TaskEventAdded` is `OnTaskEventAdded`). The calls below are the exceptions: the .NET name is
older than the contract and is kept for source compatibility. Each sends exactly the wire method on
the left.

| Contract method | .NET SDK |
|---|---|
| `GetPoidObject` | `StpRecognizer.RequestPoidObjectAsync` |
| `GetScenarioObjectSet` | `StpRecognizer.GetScenarioObjectSetContentAsync` |
| `GetScenarioTaskOrgList` | `StpRecognizer.GetScenarioTaskOrgsAsync` |
| `SetCurrentCoa` | `StpRecognizer.SetCoaAsync` |
| `GetDeletedObjects` | `StpRecognizer.RequestDeletedStpObjectsAsync` |
| `GetAllObjects` | `StpRecognizer.RequestAllStpObjectsAsync` |
| `ImportPlanData` | `StpRecognizer.ImportSTPDataAsync(string content)` |
| `SearchSymbol` | `SymbolSearch.Search` - answered in the client from a `SymbolIndex`; the engine is never asked |

## The `*FromObjectSet` methods

Each `*FromObjectSet` method is an overload of the text-content call, taking an `ObjectSet` instead
of a string. The overload sends the `*FromObjectSet` wire method with the objects as an array under
`objects`.

| Contract method | .NET SDK |
|---|---|
| `LoadNewScenarioFromObjectSet` | `StpRecognizer.LoadNewScenarioAsync(ObjectSet)` |
| `SyncScenarioSessionFromObjectSet` | `StpRecognizer.SyncScenarioSessionAsync(ObjectSet)` |
| `ImportPlanDataFromObjectSet` | `StpRecognizer.ImportSTPDataAsync(ObjectSet)` |
| `ImportTaskOrgFromObjectSet` | `StpRecognizer.ImportTaskOrgAsync(ObjectSet)` |
| `ImportCoaFromObjectSet` | `StpRecognizer.ImportCoaAsync(ObjectSet)` |

## The trailing `batchId` on object events (contract 0.9.11)

From contract 0.9.11, the add/modify/delete events for symbols, tasks, task orgs, task org units,
task org relationships and COAs end with a nullable `batchId`. It names the change set
(`BatchStarted` ... `BatchEnded`) that the object arrived in. It is null outside a change set, and
from an engine that predates change sets. The existing delegates are unchanged. Each of these events
also has an `Ext` twin with `batchId` as one more trailing argument, the pattern `OnSymbolEditedExt`
and `OnCommandExt` already use:

```csharp
stp.OnSymbolAddedExt += (poid, symbol, isUndo, batchId) => { /* batchId may be null */ };
```

| Contract event | Existing event (unchanged) | With `batchId` |
|---|---|---|
| `SymbolAdded` / `SymbolModified` / `SymbolDeleted` | `OnSymbolAdded` / `OnSymbolModified` / `OnSymbolDeleted` | `OnSymbolAddedExt` / `OnSymbolModifiedExt` / `OnSymbolDeletedExt` |
| `TaskAdded` / `TaskModified` / `TaskDeleted` | `OnTaskAdded` / ... | `OnTaskAddedExt` / ... |
| `TaskOrgAdded` / `TaskOrgModified` / `TaskOrgDeleted` | `OnTaskOrgAdded` / ... | `OnTaskOrgAddedExt` / ... |
| `TaskOrgUnitAdded` / `TaskOrgUnitModified` / `TaskOrgUnitDeleted` | `OnTaskOrgUnitAdded` / ... | `OnTaskOrgUnitAddedExt` / ... |
| `TaskOrgRelationshipAdded` / `...Modified` / `...Deleted` | `OnTaskOrgRelationshipAdded` / ... | `OnTaskOrgRelationshipAddedExt` / ... |
| `CoaAdded` / `CoaModified` / `CoaDeleted` | `OnCoaAdded` / ... | `OnCoaAddedExt` / ... |

If you attach the plain event, its Ext twin, or both, the SDK subscribes to the engine event once.
When both are attached, the plain event fires first. `OnTaskEventAdded` and `OnTaskEventModified`
are new in 0.9.11, so they carry `batchId` directly.
