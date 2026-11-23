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
| `SetCurrentCoa` | `StpRecognizer.SetCoaAsync` (sends `poid`) |
| `PullC2SIMInitialization` | `StpRecognizer.PullC2SIMInitializationContentAsync` - returns the XML string. The older `PullC2SIMInitializationAsync` returns it as `Content` with a null `ServerStatus` |
| `Register` | `StpRecognizer.RegisterAsync` / `ConnectAndRegisterAsync`; `user` is sent from `StpRecognizer.User` |
| `GetDeletedObjects` | `StpRecognizer.RequestDeletedStpObjectsAsync` |
| `GetAllObjects` | `StpRecognizer.RequestAllStpObjectsAsync` |
| `ImportPlanData` | `StpRecognizer.ImportSTPDataAsync(string content)` |
| `SearchSymbol` | `SymbolSearch.Search` - answered in the client from a `SymbolIndex`; the engine is never asked |
| `Request` | Transport: `IStpConnector.SendRequestAsync` wraps every `*Async` call in it; not called directly |

| Contract event | .NET SDK |
|---|---|
| `RequestResponse` | Transport: the answer to `Request`. The connector completes the pending `IStpConnector.SendRequestAsync` with it (a refusal throws `StpException`); no event is raised |

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

Each also has an overload taking a `TimeSpan timeout` after the `ObjectSet`. It sends that timeout as
the request's `timeout` (whole seconds, rounded up) and waits a few seconds longer for the answer.
A load that the engine stops, or cuts off at the timeout, throws `StpObjectSetLoadException`
(an `StpException`). Its `Report` holds the engine's report verbatim: which objects were applied,
which was in flight, and which were not sent.

## C2SIM options and roles

`StpC2SIMOptions` is the options dictionary that `GetC2SIMContentAsync`, `PushC2SIMContentAsync` and
`PullC2SIMInitialization*Async` take. Its typed properties are the contract's keys, in camelCase,
including `refuseUnconfirmedPlaceholders`. An unset property is omitted from the wire.
`StpRole` holds the contract's `StpRole` values: `S2`, `S3`, `S4`, `FSO` and `ENGINEER`.
`StpRole.ENG` is an alias that has the value `ENGINEER`.

## The trailing `batchId` on object events (contract 0.9.11)

From contract 0.9.11, the add/modify/delete events for symbols, tasks, task orgs, task org units,
task org relationships and COAs end with a nullable `batchId`. It names the batch
(`BatchStarted` ... `BatchEnded`) that the object arrived in. It is null outside a batch, and
from an engine that predates batches. The existing delegates are unchanged. Each of these events
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

## Calls the contract does not have

The pinned 0.9.11 contract does not list these. The SDK keeps them for source compatibility; an engine
that does not dispatch one refuses it (`StpException`) or, for `SwitchTaskConfirmation`, does nothing.

| Wire method sent | .NET SDK | Note |
|---|---|---|
| `GetActiveCoaList` | `StpRecognizer.RequestActiveCoasAsync` | Not in contract 0.9.11 |
| `GetScenarioCoaList` | `StpRecognizer.GetScenarioCoasAsync` | Not in contract 0.9.11 |
| `GetCoaObjects` | `StpRecognizer.GetCoaObjectsAsync` | Not in contract 0.9.11; `GetCoaObjectSet` is the contract call |
| `GetCurrentCoaPoid` | `StpRecognizer.GetCurrentCoaPoidAsync` | Not in contract 0.9.11 |
| `CreateCoa` | `StpRecognizer.CreateCoaAsync` | Not in contract 0.9.11; `AddCoa` is the contract call |
| `CreateDefaultCoa` | `StpRecognizer.CreateDefaultCoaAsync` | Not in contract 0.9.11 |
| `SwitchTaskConfirmation` | `StpRecognizer.SwitchTaskConfirmationAsync` | Obsolete; use `ConfirmTask` |

`SwitchRoleAndCoa` is not a contract method either. `StpRecognizer.SwitchRoleAndCoaAsync` is an SDK
convenience that sends the two contract calls in order: `SetRole` with `role`, then (unless the COA poid
is null or blank) `SetCurrentCoa` with `poid`. A refused `SetRole` throws and `SetCurrentCoa` is not
sent. The fire-and-forget `SwitchRoleAndCoa` sends the same two calls without waiting for either answer.
Earlier versions sent a single `SwitchRoleAndCoa` with `role` and `coaPoid`, which no engine dispatches.

## `ImportPlanData` results

The contract answers `ImportPlanData` and `ImportPlanDataFromObjectSet` with null on success.
`ImportSTPDataAsync` (all three overloads) returns true for that answer and throws `StpException` on a
refusal (`StpObjectSetLoadException` for the timeout overload). Before 5.12 it returned false on success.

## Coverage test

`tests/StpSDK.Tests/TestData/Contract/sketch-thru-plan-api.json` is a pinned copy of the contract (its
README names the source commit and version). `ContractCoverageTests` checks that every contract method
and event maps to an SDK member, by the default rule or by the tables above, and fails naming each one
that does not. It also checks that every name in these tables is in this page.
