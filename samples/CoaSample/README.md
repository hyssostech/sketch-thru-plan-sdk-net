# COA Sample Overview

This sample extends the [Roles sample](../RolesSample), adding COA (Course of Action) handling:

* Creating, renaming and deleting COAs
* Making a COA the current one, and following COA switches
* Following the COA events, with the change set each event arrived in
* Tying a task org to a COA (a per-COA task org) and clearing it
* Exporting a COA and importing it back, as text and as an object set (with a load timeout and the engine's
  report when the load does not complete)

Only the aspects particular to this sample are described here.
Details shared by all samples are described in the [main samples page](../README.md).
The COA code is in [Form1.Coa.cs](./Form1.Coa.cs).

## STP COAs

A COA groups the symbols and tasks of one course of action. A scenario can hold several COAs per affiliation,
for example the friendly COAs being weighed for a mission and the enemy COAs they are weighed against. Each
role works on its own *current* COA: new symbols and tasks go into it. A COA can also have its own task org,
which comes into force while the COA is current; without one, the COA uses the default task org of its
affiliation (see the [Task Org sample](../TaskOrgSample)).

COAs are `StpCoa` objects: `Name`, `Affiliation` (`friend` or `hostile`), `CreatorRole` and the `Poid` STP
assigns.

## COA events

The COA events are subscribed before connecting, like every other event:

```csharp
_stpRecognizer.OnCoaAddedExt += StpRecognizer_OnCoaAdded;
_stpRecognizer.OnCoaModifiedExt += StpRecognizer_OnCoaModified;
_stpRecognizer.OnCoaDeletedExt += StpRecognizer_OnCoaDeleted;
_stpRecognizer.OnCoaSwitched += StpRecognizer_OnCoaSwitched;
```

The sample uses the `Ext` variants of the add/modify/delete events, which carry one more argument than
`OnCoaAdded` and friends: the `batchId` of the change set the COA arrived in (for example an import), or null
outside a change set. Change sets are described in the [Scenario sample](../ScenarioSample).

```csharp
private void StpRecognizer_OnCoaAdded(string poid, StpCoa coa, bool isUndo, string batchId)
```

`OnCoaSwitched` reports the COA that became current - after `SetCoaAsync`, or after a role switch. The COA list
marks it with `*`.

## COA panel

The `COAs` panel opens next to the main window once connected; the `COAs` toolbar button shows it again if it
is closed. The list shows the scenario's COAs; most buttons act on the selected one.

| Button | SDK call | Notes |
|---|---|---|
| `Add` | `AddCoa(new StpCoa { Name, Affiliation, CreatorRole })` | Fire-and-forget: STP assigns the poid and `OnCoaAdded` confirms it |
| `Rename` | `UpdateCoa(poid, coa)` | Sends the whole definition, with the new name; `OnCoaModified` confirms it |
| `Delete` | `DeleteCoaAsync(poid)` | Deletes the COA with its symbols and tasks; `OnCoaDeleted` follows |
| `Make current` | `SetCoaAsync(poid)` | `OnCoaSwitched` follows, and `OnTaskOrgSwitched` when the COA has its own task org |
| `Refresh` | `GetScenarioCoasAsync()`, `GetCurrentCoaPoidAsync(role)` | Re-reads the list, for example after joining a session that already had COAs |
| `Tie loaded TO` | `SetCoaTaskOrgAsync(toPoid, coaPoid)` | Ties the task org loaded with the Task Org box's `Friend` / `Hostile` button (the one matching the COA's affiliation) to the COA |
| `Clear COA TO` | `ResetCoaTaskOrgAsync(null, coaPoid)` | Clears the COA's task org for every affiliation; the task org itself is kept |
| `Export` / `Import` | `GetCoaContentAsync(poid)` / `ImportCoaContentAsync(content)` | STP text content. Poids are kept, so Import brings back the very COA exported, not a copy. Task org units and relationships are not part of a COA |
| `Export object set` / `Import object set` | `GetCoaObjectSetAsync(poid)` / `ImportCoaAsync(objectSet, timeout)` | The same content as typed objects, imported with the panel's load timeout |

The role used for `Add` and `Refresh` is the one chosen in the Role box (S2, S3, S4, FSO or ENGINEER, from
`StpRole.All`).

Per-COA task orgs need a recent engine: engines before STP-1077 refuse `SetCoaTaskOrgAsync` and
`ResetCoaTaskOrgAsync` with a COA poid, and the sample shows the refusal.

### Loads that do not complete

`ImportCoaAsync(objectSet, timeout)` sends the timeout to the engine. A load the engine stops, or cuts off at
the timeout, throws `StpObjectSetLoadException`; its `Report` is the engine's account of which objects were
applied, which one was in flight and which were not sent:

```csharp
try
{
    string poid = await _stpRecognizer.ImportCoaAsync(_lastCoaObjectSet, timeout);
}
catch (StpObjectSetLoadException ex)
{
    StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, ex.Report);
}
```

## Running the sample

Build and run it as described in the [main samples page](../README.md#running-the-samples). Then:

1. Choose `S3` in the Role box
2. In the `COAs` panel, type a name, choose `friend` and select `Add`: the COA appears in the list
3. Select it and `Make current`; sketch and speak a few symbols - they go into this COA
4. Add a second COA, make it current and place different symbols; switch back and forth
5. Load the friendly task org with `Friend`, select a COA and `Tie loaded TO`, then make that COA current:
   the log shows `TO SWITCHED`
6. `Export` a COA, `Delete` it, and `Import` to bring it back
