# Tasking Sample Overview


This sample extends the [Editing Sample](../EditingSample), adding:

* Task Handling
    * Task alternate selection
    * Manual task updates and deletion
* Resetting STP

Here just the particular aspects illustrated by the sample are described.
Details shared by all samples are described in the [main samples page](../README.md). 
Common rendering and other map operations are described in the [Mapping class documentaiton](../../Plugins/Mapping/SimpleMapPlugin/README.md)

## Handling Tasks

STP identifies tasks automatically, without user intervention, based on doctrinal combinations of symbols. For example (other examples can be found in STP's Training Guide, available in the Documentation that is part of the install package):

1. Place a unit on the map by sketching a Point and speaking "Infantry Company"
1. Place an objective by sketching a circular Area at some distance from the unit and speaking "Objective Alpha"
1. Connect the unit with the objective with a Line, and speak "Main Attack"

NOTE: task interpretation was trained on examples provided by subject matter experts. Recognition is therefore tuned to visual representations
that expert users would recognize as representing tasks. If symbols are lumped together, or are not properly placed (e.g. if an axis does not clearly 
originate at a unit, or terminate at an objective), then recognition may be incorrect or not present. 
Tasks need to make visual sense - if a user cannot tell clearly what the tasks is, STP will not either.

STP identifies one or more possible tasks as these symbols are entered, and sends a ranked list of alternates back to the client application via asynchronous events.


* OnTaskAdded - invoked whenever a new task is created as a result of successful combination of multiple individual symbols
(e.g. a Unit, an Axis of Advance  and an Objective area)
* OnTaskModified - invoked whenever the properties of a task are modified
* OnTaskDeleted - invoked whenever the a task is deleted/removed

This sample displays task information in a log window, and property grid, besides populating the data grid used for alternate selection 
(discussed further down).

```csharp
private void StpRecognizer_OnTaskAdded(string poid, StpTask stpTask, List<string> taskPoids, bool isUndo)
{
    _currentTask = stpTask;
    DisplayTask(_currentTask);
}

private void StpRecognizer_OnTaskModified(string poid, StpTask stpTask, List<string> tgPoids, bool isUndo)
{
    StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
    string msg = $"TASK MODIFIED:\t{stpTask.Poid}\t{stpTask.FullDescription}";
    StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
    _currentTask = stpTask;
    DisplayTask(_currentTask);
}

private void StpRecognizer_OnTaskDeleted(string poid, bool isUndo)
{
    StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
    string msg = $"Task DELETED:\t{poid}";
    StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
    _currentTask = null;
    DisplayTask(_currentTask);
}
```

### Task alternate selection 

This sample extends the [Editing Sample's'](../EditingSample) symbol alternate handling, displaying task alternates
whenever a new or updated task is detected. The same data grid is used, but to display  task alternates rather than just symbol alternates. 

Selection of an alternate task is supported by STP via the `ConfirmTask` SDK method. It requires the unique identifier of the task
for which a lower ranked interpretation is being selected, as well as the `Order` property for that item. The Order/rank is in general
zero for the best/most likely interpretation, 1 for the second best and so on. This property is defined for each of the tasks
in the list of Alternates that are returned by STP. Selection should therefore identify the Order of the element that the user
selected, and send that over to STP for processing.

```csharp
private void DataGridViewAlternates_RowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
{
    // Update the symbol with the corresponding interpretation when row is selected
    if (e.StateChanged == DataGridViewElementStates.Selected)
    {
        var item = e.Row.DataBoundItem as StpItem;
        if (item != null)
        {
            // Inform STP that the item with this rank order was chosen by the user
            // Note that the first row corresponds to the currently selected / 
            // displayed symbol 
            // No need to chose it again if the user selected the first row
            if (item is StpSymbol && e.Row.Index > 0)
            {
                // STP will issue a symbol update notification (OnSymbolModified) with 
                // this element first in the list of alternates
                _stpRecognizer.ChooseAlternate(item.Poid, item.Order);
            }
            else if (item is StpTask)
            {
                // For tasks, even selecting the first / best item is a valid option, 
                // as that will then be confirmed andn in many cases generate an 
                // anticipated unit at the target location (e.g. the objective 
                // being attacked)
                // Task confirmation will cause STP to issue a task update notification 
                // (OnTaskModified) with the chosen element as the single selected task
                _stpRecognizer.ConfirmTask(item.Poid, item.Order);
            }
        }
    }
}

```

Unlike symbols, the chosen Task becomes the single interpretation used henceforth. STP may, depending on the task, automatically place
an anticipated unit symbol at the target location (e.g. objective centroid) to indicate that this is a future location for the tasked
unit, should the task be successful. The anticipated unit can in turn be the Who in a subsequent task that the unit is to perform.


### Manual task updates and deletions

When users add, modify or remove elements using conventional UI controls, these operations need to be conveyed to STP, so that the
state remains consistent within the Engine itself and across all client apps.

This sample extends the manual symbol operations introduced in [Editing sample](../EditingSample) with task 
deletion and updates.
Once again, an SDK method - `DeleteTask` - is invoked to notify STP. 
The app then handles the corresponding asynchronous `OnTasklRemoved` event 
that is broadcast by STP as a response.


```csharp
    private void ButtonDelete_Click(object sender, EventArgs e)
    {
        // Bail if no symbol (cleaner if Delete button were disabled)
        if (propertyGridResult.SelectedObject is null)
        {
            return;
        }

        // Get id of what is loaded
        string currentPoid = (propertyGridResult.SelectedObject as RootVM).Id;

        // Delete task or symbol, depending on what is currently displayed
        if (propertyGridResult.SelectedObject is TaskVM)
        {
            // Inform STP that this task should be deleted
            // STP will issue a symbol deletion notification (OnTaskRemoved),  
            // which will cause the actual eventual removal
            _stpRecognizer.DeleteTask(currentPoid);

        }
        else
        {
            // Inform STP that this symbol should be deleted
            // STP will issue a symbol deletion notification (OnSymbolRemoved),  
            // which will cause the actual eventual removal
            _stpRecognizer.DeleteSymbol(currentPoid);
        }
    }
}
```

Task updates are performed via the `TaskUpdate` SDK method, which will in turn cause STP to
eventually generate an asynchronous `OnTaskUpdated` event to be broadcast as a response.

```csharp
private void ButtonUpdate_Click(object sender, EventArgs e)
{
    // Bail if no symbol 
    if (propertyGridResult.SelectedObject is null)
    {
        return;
    }

    // Delete task or symbol, depending on what is currently displayed
    if (propertyGridResult.SelectedObject is TaskVM taskVM)
    {
        // Inform STP that this task should be deleted
        // STP will issue a symbol deletion notification (OnTaskModified), 
        // which will cause the actual eventual update
        _stpRecognizer.UpdateTask(taskVM.Id, (StpTask)taskVM.AsStpItem());

    }
    else if (propertyGridResult.SelectedObject is SymbolVM symbolVM)
    {
        // Inform STP that this symbol should be updated
        // STP will issue a symbol deletion notification (OnSymbolModified), 
        // which will cause the actual eventual update
        _stpRecognizer.UpdateSymbol(symbolVM.Id, symbolVM.AsStpItem());
    }
    else
    {
        System.Diagnostics.Debug.Fail("Unexpected ViewModel item type");
    }
}
```

A similar mechanism is employed to manually add new tasks (`AddTask`).
In case of new tasks, `AddTask` takes an object populated with the desired properties, as manually specified by the user. 
For convenience, the SDK provides a `DefendInPlaceTask` factory method that creates a minimal Defend In Place task to which users may 
add attributes and customize to manually build other tasks.

```csharp
StpTask.DefendInPlaceTask(whoSymbol.Poid)
```
## Resetting STP

STP's task recognizer takes in consideration the context of multiple symbols, and the potential that they might be combined with other symbols
as higher order Task constructs.
The interpretation is visual, and a cluttered display, with overlapping or stacked up symbols, which would make it difficult for users to clearly 
determine which tasks are intended, will likewise prevent the recognizer from performing at its best.

This sample provides a way to quickly reset the state so that other tasks can be tried out without having to restart STP and the application.
This is usually a drastic step, that needs to be used carefully, as it affects all clients that are connected to STP. 
Upon receiving a reset command, STP will propagate individual deletion events to all clients.

Reset has a function in the initialization of new scenarios, but that aspect is going to be addressed in a separate sample.


```csharp
private void ResetScenario()
{
    // Reset STP scenario - all symbols are deleted and STP is returned to a clean state
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    _stpRecognizer.ResetStpScenarioAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

    // Clear log window
    textBoxLog.Clear();

    // Clear the map display
    _mapHandler.ClearMap();

    // Clear any previous STP state
    _currentSymbols = new();
}
```

## Sketching and rendering handling

Since tasks are composed of multiple symbols, the sample keeps a list (`_currentSymbols`) of the symbols entered thus far.
This list is updated as symbols are added, removed or modified.

Whenever required, the sample redraws all current symbols to keep the display updated as STP notifies of new symbols, updates and removals.
This works for small number of symbols only, for the illustrative purposes of the sample.

For additional details on rendering and other map operations see the common [Mapping class](../../Plugins/Mapping/SimpleMapPlugin)