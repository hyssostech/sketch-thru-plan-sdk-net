# Tasking Sample Overview

This sample extends the [quickstart](../../quickstart) demonstration os Sketch-Thru-Plan sketch and speech creation of military plans, 
extending capabilities:

* Handling Tasks events, besides the symbol handling already performed by the quickstart

* Adding basic 2525 single point symbol rendering according to the standard 2525/APP6 standard

Limitations:

* Rendering shows added symbols, but does not update when symbols are deleted or modified

NOTE: while some basic rendering capabilities are provided, the main objective of this sample is to support the creation of tasks, 
which require the composition of multiple symbols, for example a Unit, an Axis of Advance, and an Objective. 
STP assumes that symbol interpretations will be rendered by a capable existing mapping system and rendering engine, 
part of the broader application STP is being integrated into. 

# Adding military symbols to a maps via Speech and Sketch

This sample, similarly to the [quickstart](../../quickstart), demonstrates how sketches (and optionally speech) collected by an app can
be sent for processing by Sketch-thru-Plan (STP) for interpretation. If successfully interpreted, the combined/fused speech and 
sketch are turned into military symbols representation by STP, and sent back to the app for display and rendering.

## Prerequisites
* Sketch-thru-Plan (STP) Engine (v5.5.1+) running on localhost or an accessible machine
* A machine with a working microphone
* STP's Speech component running on the same machine as the app

## Configuration settings

Default parameters are set in [appsettings.json](./appsettings.json), within an `App` section:

* StpHost - address of the machine executing the STP engine, e.g. localhost
* StpPort - port STP listens to - the default is 9555
* MapImagePath - path to a file containing the image of the map that is displayed by the app,
* MapTopLat - Latitude of the top left corner of the map image
* MapLeftLon - Longitude of the top left corner of the map image
* MapBottomLat - Latitude of the bottom right corner of the map image
* MapRightLon - Longitude of the bottom right corner of the map image

These settings can be overridden via command line parameters when running the app:

```
SimpleRendering.exe App:StpHost="10.2.10.70"
```

Notice that the name of the `appsettings.json` section containing the application parameters - `App` - needs to be used as a prefix, as 
shown in the example above

## Running the  sample

* Build the app using Visual Studio Community or Code
* Start STP 
    * Follow the install instructions provided by Hyssos
    * For this sample, the STP Server Advanced configuration provides the required services, but the app works as well
    with the STP Desktop configuration
* Launch the sample app 
    * A connection to the STP server is established and a form is displayed. 
    * If an error message is displayed, verify that STP is running on the server at the address and port configured above, 
    and that the port is not being blocked by a firewall
* **NOTE**: STP's Speech component must be running on the same box as the app, with access to a working microphone


## Entering tasks

Enter symbols by sketching and speaking as demonstrated in the quickstart.

To create a task, place related symbols on the map, for example:
    * Sketch a point (or small line) and speak "Infantry Company", or "Stryker Brigade"
    * Sketch an area at some distance of the Unit and speak "Objective Bravo" 
    * Sketch a line starting at the Unit and ending inside the Objective, and speak "Main Attack Boston", or "Supporting Attack Trenton"

Successful recognition of the task composing the three symbols results in the display of task properties, 
a summary of the alternative interpretations of the task.

For additional task examples, see the STP Training Guide (provided with the STP Engine installers).


## Brief code walkthrough

### Initialization 

**Connector Plugin** - The first step it to create a connection object that will provide the basic communication services to STP. In this quickstart app, we employ a sockets connector that communicates with STP's native OAA Publish Subscribe services. This plugin ships with the SDK.

Other plugins can be developed to implement different communication mechanisms, for example plain REST calls, or based on some event queue mechanism used by the backend infrastructure into which STP may have been embedded. An example of a websockets plugin serving JavaScript clients is posted [here](https://github.com/hyssostech/sketch-thru-plan-sdk-js/tree/main/plugins/connectors). While in a different language, that code illustrates the principles that could be used to generate a .NET version with similar capabilities.

```cs
// Create an STP connection object - using STP's native pub/sub system
var stpConnector = new StpOaaConnector(_logger, _appParams.StpHost, _appParams.StpPort);
```

**STP recognizer initialization** - communication with STP is achieved via recognizer object that takes the connector and optional speech plugins as parameters 

```cs
// Initialize the STP recognizer with the connector definition
_stpRecognizer = new StpRecognizer(stpConnector);
```

**Rendering** 

For the purposes of this sample, an to illustrate a very simple approach to rendering, images are built using the SVG representations provided by the
[Joint Military Symbology XML / JMSML](https://github.com/Esri/joint-military-symbology-xml) project.

The content of the `svg` JMSML folder needs to be available somewhere accessible to the app. STP installs these files
in `C:\ProgramData\STP\JMS\SVG` by default. 

If running the STP Engine on the same machine, nothing needs to be changed. If running the app on a different machine,
then download the required [SVG metadata](../svg.zip) and extract into an accessible location. 

If the content is extracted to a location that is not the default `C:\ProgramData\STP\JMS\` (preferred), then the recognizer will
need to be configured by setting the `JMSSVGPath` as part of the initialization:

```cs
_stpRecognizer.JMSSVGPath = @"<path to the folder containing the required SVG definition>"
```

## Event subscription

_Before_ connecting to STP, it is important to subscribe to the handlers of interest. This information is used by the SDK to build the corresponding subscription parameters that tell STP which events/messages to send to this client app.

STP triggers events asynchronously as user actions are interpreted as military symbols (and other types of entities, not covered here). This quickstart subscribes to:

* Tasks
    * OnTaskAdded - invoked whenever a new task is created as a result of successful combination of related symbols
    * OnTaskModified - invoked whenever the properties of a task are modified
    * OnTaskDeleted - invoked whenever the a task is deleted/removed
* Task Org / ORBAT
    * OnTaskOrgUnitAdded
    * OnTaskOrgUnitModified
* UI 
    * OnInkProcessed - invoked when strokes have been processed by STP. This event is useful for removing ink that either resulted in a successful symbol interpretation, or was dropped from consideration, to keep the interface clean
    * OnSpeechRecognized - provides feedback on the speech to text interpretation. This even is optional, but provides meaningful feedback to users. 
* Infrastructure events
    * OnStpMessage - is triggered by STP when a message needs to be brought to the user's attention, for example notices of disconnection, communication failures.
    * OnConnectionError - is triggered when the communication to the STP engine is interrupted and cannot be restored.
    * OnShutdown - when the STP engine is stopped, a message is sent to clients advertising the fact. Clients may have the option to shut themselves down as well (as is done in this quickstart), or notify the user.

As an example of STP event handling, task notifications can be handled as illustrated below. Similar code is employed to handle other 
STP events. Refer to the source for additional details - [Forms1.cs](./Form1.cs): 

```cs
// Hook up to the events _before_ connecting, so that the correct message subscriptions can be identified
// A new symbol has been recognized and added
_stpRecognizer.OnTaskAdded += (poid, stpTask,taskPoids, isUndo) =>
{
    // Get the recognized item as a military symbol - not interested in other types of objects 
    DisplayTask(stpTask);
};
_stpRecognizer.OnTaskModified += (poid, stpTask,tgPoids, isUndo) =>
{
    ShowStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
    string msg = $"TASK MODIFIED:\t{stpTask.Poid}\t{stpTask.FullDescription}";
    ShowStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
};
_stpRecognizer.OnTaskDeleted += (poid, isUndo) =>
{
    ShowStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
    string msg = $"Task DELETED:\t{poid}";
    ShowStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
};
...
```
## Connecting to STP

After all desired events are subscribed to, connection to STP can be attempted.`ConnectAndRegister` is invoked with a name that represents the 
connecting app.

```cs
// Attempt to connect
bool success;
try
{
    success = _stpRecognizer.ConnectAndRegister("StpSimpleRendering");
}
catch
{
    success = false;
}

// Nothing else to do if connection failed
if (!success)
{
    // Or notify user and exit here instead of the invoker
    return false;
}
```

## Sketch handling

In this sample, mapping operations are isolated into a [Mapping class](Mapping.cs). Mouse/stylus user actions handled by this class
are exposed as a `pen down` and `pen stroke` events. These in turn are transmitted to STP for processing via the SDK.

```cs
// Hook up to the map handler
_mapHandler = new Mapping(pictureMap, _appParams);
_mapHandler.OnPenDown += (sender, geoPoint) => {
    // Notify STP of the start of a stroke and activate speech recognition
    _stpRecognizer.SendPenDown(geoPoint, DateTime.Now);
};
_mapHandler.OnStrokeCompleted += (sender, penStroke) =>
{
    _stpRecognizer.SendInk(penStroke.PixelBounds,
                            penStroke.TopLeftGeo,
                            penStroke.BotRightGeo,
                            penStroke.Stroke,
                            penStroke.TimeStart,
                            penStroke.TimeEnd,
                            penStroke.IntersectedPoids);
};
```

Internally, `Mapping` handles mouse events in a way similar to the [quickstart](../quickstart). Please refer to that sample's 
documentation for additional detail.

### Symbol rendering

STP returns coordinates set according to the 2525/APP6 standard anchor points. These are normally what is required to drive a standards-based renderer. 

In this sample, three images are used to hold 1) the background map, 2) rendered symbols and 3) sketched ink. That makes it possible to erase the ink and keep 
added symbols visible, and to revert to an empty map if the user selects the "Reset STP" button" (more details further down).

To get an image representing the a symbol, the `Bitmap` method is used. Under the hood this method invokes JMSML SVG functionality 
(as already previously discussed)
to create a representation that matches the symbol's properties. 

```cs
const int RenderSize = 100;
Image symbolImage = stpSymbol.Bitmap(RenderSize, RenderSize);
```

## Resetting STP

The STP Engine is designed to handle one plan creation session (or scenario) at a time, to which multiple users may connect to
collaborate. State of an ongoing scenario is kept until explicitly reset by a user. 

This app forces a reset on startup, so that users have a clean slate to test out tasking, without concern for symbols that
may have been placed previously. 

*NOTE*: if the app is restarted, it will show the symbol delete notifications that STP issues to all clients when it receives a reset 
command. These messages will appear on the log window at startup.

Reset makes task testing simple, as the context is restarted afresh, but erases all symbols from all connected clients, so would not work in 
practice to support collaborative planning, which is a foundational STP capability.

```cs
// Clear any previous STP state to start with a clean slate
_stpRecognizer.ResetStpScenario();
```

The app also resets the scenario and clear log and map when the `Reset STP` button is selected.

See other samples for discussion and code demonstrating how an app can join an ongoing session.

