# Scenario Sample Overview


This sample extends the [Tasking sample](../TaskingSample) adding capabilities to manipulate scenario data:

* Creating a new blank scenario
* Joining a scenario that is already loaded in STP
* Saving a scenario to persistent storage
* Loading a scenario from persisted storage

Here just the particular aspects illustrated by the sample are described.
Details shared by all samples are described in the [main samples page](../README.md). 
Common rendering and other map operations are described in the 
[Mapping class documentaiton](../../Plugins/Mapping/SimpleMapPlugin/README.md)

## STP Scenarios

STP scenarios are collections of related objects, e.g. associated with a particular mission. These objects may
include Symbols of various affiliations, Tasks, and Task Org/ORBATs.

STP supports collaboration, making it possible for multiple (disparate) user interface apps connected 
to the STP Engine to receive notification of each other actions, as users speak and sketch to create 
and edit symbols and tasks. The Engine is therefore loaded with a scenario at each time, which can be thought 
of as a collaborative session. 
In this document the terms `scenario` and `session` have very similar meanings in most contexts and can
be used interchangeably.

In this sample, the SDK capabilities for dealing with scenarios is presented.

NOTE: in this sample, entities that are removed, created, changed as a result of the scenario operations
are displayed on the user interface as the operations unfold. 
This is a simple approach that works well enough for small number of symbols.
For realistic, larger scenarios, the delays introduced by the piece-meal display will be normally
too noticeable.
A different design can be consider, in which progress is shown in a more economical fashion, 
with the full details becoming available after the conclusion of the operation.

## Startup check

Since an app may connect to STP when a scenario might have been already loaded or populated by other 
components, apps may consider checking STP's state and offering users the option to join an existing
session or create a new one.

In this sample, the `Connect()` method is extended to test and present users the option.
`HasActiveScenarioAsync()` is used to check if STP already has a loaded scenario or not.
Additional details can be retrieved via the `GetActiveScenarioDescriptionAsync()` method,
which returns a `PlanningScenario` object with additional scenario properties.

`DoJoinScenarioAsync()` and `DoNewScenarioAsync()` are discussed in further detail throughout
this document.

```cs
// Offer to join ongoing session if there is one, or start new scenario otherwise
if (await _stpRecognizer.HasActiveScenarioAsync())
{
    if (DialogResult.Yes == MessageBox.Show(
        $"Join current STP scenario? Yes to Join, No to reset to a new scenario", 
        "Scenario Option", 
        MessageBoxButtons.YesNo))
    {
        await DoJoinScenarioAsync();
        return true;
    }
}

// Start new empty scenario
await DoNewScenarioAsync();
```

## Creating new blank scenarios

`CreateNewScenarioAsync()` initializes a new scenario in STP. Any previous loaded scenario is discarded.
As a consequence, multiple delete operations may be issued by the Engine, to clear out the context
of potentially multiple apps that may be connected to the Engine session.

```cs
private async Task DoNewScenarioAsync()
{
    await PerformLongOp( async () =>
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string name = $"StpSDKSample{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Creating new scenario: {name}");

        // Launch operation
        CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromSeconds(TimeOutSec));
        await _stpRecognizer.CreateNewScenarioAsync(name, cts.Token);
    });
}
```

The optional `CancellationToken` parameter provides means to establish a time out. 
If not successfully completed before the set cancellation time, a `OperationCanceledException` is thrown.
If no token is provided, then a 10 seconds default is used. This is enough for regular STP operations, 
but may not be sufficient for scenario operations, where a larger volume of entities has to be processed.

In this sample, a common timeout message is displayed for any of the scenario operations, within the 
`PerformLongOp()` sample code utility method.
This method invokes a scenario operation provided as a parameter in a context where buttons are disabled
and progress indication is provided - in this case just via a Wait cursor.

The actual operation if performed within a separate thread, but in this simple app, that is not strictly necessary,
as the intention is to keep users from using the UI in the first place.

```cs
private async Task PerformLongOp(Func<Task> action)
{
    try
    {
        // Set wait cursor and disable all buttons
        Application.UseWaitCursor = true;
        Application.DoEvents();
        groupBoxScenario.Enabled = false;

        // Perform the action in its own thread - side effects will be handled on the UI thread, as panels and map are updated
        await Task.Run(async () => 
            await action()
        );
    }
    catch (OperationCanceledException)
    {
        _logger.LogWarning($"Operation timed out after {TimeOutSec}");
        MessageBox.Show("Operation is taking too long. Please retry if needed", "Timeout", MessageBoxButtons.OK);
    }
    catch (Exception ex)
    {
        _logger.LogError($"Operation failed: {ex}");
        MessageBox.Show($"Operation failed: {ex.Message}", "Error Performing Scenario Operation", MessageBoxButtons.OK);
    }
    finally
    {
        // Restore cursor and buttons
        Application.UseWaitCursor = false;
        Application.DoEvents();
        groupBoxScenario.Enabled = true;
    }
}
```



XXXXXXXXXXXXXXXXXXXXXXXXXX
NOTES: timeout may not interrupt the op - cancellation does not extend to side effects
Interrupted ops may still continue - no real means to cancel

XXXXXXXXXXXXXXXXXXXXXXXXXX


## Joining a loaded scenario / session

`JoinScenarioSessionAsync()` retrieves the current STP scenario content and gets it loaded into 
a local app.
The app's usual Symbol and Task event handlers are invoked, as if the symbols had just been 
received from STP as a result of some user action on the UI.

```cs
private async Task DoJoinScenarioAsync()
{
    await PerformLongOp( async () =>
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Joining scenario");

        // Launch operation
        CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromSeconds(TimeOutSec));
        await _stpRecognizer.JoinScenarioSessionAsync(cts.Token);
    });
}
```

## Saving scenarios to external/persistent storage

`GetScenarioContentAsync()` returns a string representation of the current STP
scenario.
The string is formatted according to a serialized representation of STP native
internal formats, and is similar to JSON. 
The details of this particular format are outside the scope of the samples.

Other representations can also be produced, for example in the
 [C2SIM interoperability standard](https://github.com/OpenC2SIM/OpenC2SIM.github.io) 
xml format. That is covered elsewhere in the SDK documentation.

For the purposes of this sample, the returned data is just saved to disk, but could of course be added
to any other structure controlled by a platform to which STP is being added. 

```cs
private async Task DoSaveScenarioAsync(string filePath)
{
    await PerformLongOp(async () =>
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Saving scenario to {filePath}");

        // Get the current contents
        string content = await _stpRecognizer.GetScenarioContentAsync();

        // Save to file
        await File.WriteAllTextAsync(filePath, content);
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
    });
}
```

## Loading Scenarios from external/persistent storage

`LoadNewScenarioAsync()` takes the serialized content described above and loads it into STP.
As the scenario is being loaded, STP issues the usual Symbol and Task creation events,
essentially replaying messages in the order in which users originally placed these symbols. 

As is the case with saving, the SDK is also able to import scenarios represented 
according to the [C2SIM interoperability standard](https://github.com/OpenC2SIM/OpenC2SIM.github.io) 
xml format. That is covered elsewhere in the SDK documentation.

For the purposes of this sample, data is just read from a file, but could of course be retrieved
from any other structure controlled by a platform to which STP is being added. 

```cs
private async Task DoLoadScenarioAsync(string filePath)
{
    await PerformLongOp( async () =>
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Loading new scenario from {filePath}");

        // Load the file contents
        string content = File.ReadAllText(filePath).Replace("\n", string.Empty).Replace("\r", string.Empty);

        // Launch operation
        CancellationTokenSource cts = new();
        cts.CancelAfter(TimeSpan.FromSeconds(TimeOutSec));
        await _stpRecognizer.LoadNewScenarioAsync(content, cts.Token);
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
    });
}
```
