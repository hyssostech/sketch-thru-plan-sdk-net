# Task Org Sample Overview

This sample extends the [Scenario sample](../ScenarioSample), adding Task Organization (Task Org / ORBAT)
handling:

* Importing a friendly and a hostile task org from STP text content and making each the default for its affiliation
* Retrieving a task org's content
* Listing the scenario's task orgs, choosing the default task org and clearing it
* Exporting a task org as an object set and importing it back, with a load timeout and the engine's report when the load does not complete
* Following the task org events: task orgs, units and relationships added, modified and deleted, and the switch of the task org in force

Only the aspects particular to this sample are described here.
Details shared by all samples are described in the [main samples page](../README.md).
Common rendering and other map operations are described in the
[Mapping class documentation](../../plugins/Mapping/SimpleMapPlugin/README.md).

## STP task orgs

A task org describes the units available to a plan and how they are organized: the task org itself
(`StpTaskOrg`), its units (`StpTaskOrgUnit`) and the relationships between them (`StpTaskOrgRelationship`,
for example `organic`). A scenario can hold several task orgs per affiliation; one of them is the
*default* for its affiliation, and is the one STP uses when it recognizes units by speech ("one first
infantry") and when it builds tasks.

## Task org events

The task org events are subscribed before connecting, like every other event
([Form1.cs](./Form1.cs), `Connect()`):

```csharp
_stpRecognizer.OnTaskOrgAdded += StpRecognizer_OnTaskOrgAdded;
_stpRecognizer.OnTaskOrgUnitAdded += StpRecognizer_OnTaskOrgUnitAdded;
_stpRecognizer.OnTaskOrgRelationshipAdded += StpRecognizer_OnTaskOrgRelationshipAdded;
// ... and the Modified / Deleted events of each
_stpRecognizer.OnTaskOrgSwitched += StpRecognizer_OnTaskOrgSwitched;
```

`OnTaskOrgSwitched` reports the task org now in force, after a new default is set or when a COA with its own
task org becomes current (see the [COA sample](../CoaSample)). The sample shows its name in the Task Org box,
in blue for friendly and red for hostile.

## Task Org box: importing and selecting a default

The `Friend` and `Hostile` buttons import a small task org from STP text content, the first time they are
pressed, and make it the default for its affiliation. `Get` retrieves the content of the loaded task org.

```csharp
_toFriendPoid = await _stpRecognizer.ImportTaskOrgContentAsync(content);
await _stpRecognizer.SetDefaultTaskOrgAsync(_toFriendPoid);
...
string content = await _stpRecognizer.GetTaskOrgContentAsync(poid, CancellationToken.None);
```

## Task orgs panel

The `Task orgs` panel ([Form1.TaskOrgs.cs](./Form1.TaskOrgs.cs)) opens next to the main window once connected;
the `Task orgs` toolbar button shows it again if it is closed.

* `Refresh` lists the scenario's task orgs - `GetScenarioTaskOrgsAsync()`
* `Make default` makes the selected task org the default for its affiliation - `SetDefaultTaskOrgAsync(poid)`
* `Clear friend default` / `Clear hostile default` unset the default of an affiliation; the task org itself is
  kept - `ResetDefaultTaskOrgAsync(affiliation, cancellationToken)`
* `Export object set` keeps the selected task org, with its units and relationships, as an `ObjectSet` -
  `GetTaskOrgObjectSetAsync(poid)`
* `Import object set` imports it back, giving the engine the load timeout set in the panel -
  `ImportTaskOrgAsync(objectSet, timeout)`

### Loads that do not complete

The `ObjectSet` loads take a `TimeSpan timeout`. It is sent to the engine as the load's timeout (whole
seconds, rounded up; `TimeSpan.Zero` means no engine timeout), and the SDK waits a few seconds longer for the
answer. A load that the engine stops, or cuts off at the timeout, throws `StpObjectSetLoadException`, whose
`Report` is the engine's own account: which objects were applied, which one was in flight, and which were not
sent. The sample shows that report in the log:

```csharp
try
{
    string poid = await _stpRecognizer.ImportTaskOrgAsync(_lastTaskOrgObjectSet, timeout);
}
catch (StpObjectSetLoadException ex)
{
    StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, $"{ex.Method} did not complete. Engine report:");
    StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, ex.Report);
}
```

## Running the sample

Build and run it as described in the [main samples page](../README.md#running-the-samples). Then:

1. Select `Friend` to load the friendly task org and make it the default; the Task Org box shows its name
2. Sketch a point and speak the name of one of the task org's units: the symbol STP recognizes is that unit
3. Open `Task orgs`, select `Refresh`, then export the task org as an object set, and import it back
4. Set the load timeout to 0 to let the engine take as long as it needs, or to a small value on a large task
   org to see the report of a load that was cut off
