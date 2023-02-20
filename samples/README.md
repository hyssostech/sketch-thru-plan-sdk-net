# Sketch-thru-Plan .NET Samples

Samples that illustrate the foundational capabilities exposed by the SDK through simplified but meaningful code:

* [Quickstart sample](../quickstart) - Using the SDK and entering symbols via speech and sketch

* [Editing sample](./EditingSample) -  Symbol editing via speech and sketch, selection from the list of alternate interpretations, and manual user operations

* [Tasking sample](./TaskingSample) - Handling Tasks that are automatically recognized by STP as users place multiple related symbols on the map

* [Scenario Sample](./ScenarioSample) - Management of scenario data

* [Connection Sample][./ConnectionSample) - Connection to local area and remote instances of STP using TCP or WebSockets

* [Reactive Extensions Sample](./ReactiveSample) - Rx Observable caches of symbols, tasks, orbat/TO bound to controls
    

## Common sample code overview

All samples (but the quickstart, which is simpler) share a common structure and style, which is overviewed here. Specific additional functionality 
of each sample is described in their corresponding documentation.

## Prerequisites

* Sketch-thru-Plan (STP) Engine (v5.6.0+) running on localhost or an accessible machine
* A machine with a working microphone
* STP's Speech component running on the same machine as the app

The SDK nuget package supports both .Net 6 as well as .Net Standard 2.0 (compatible with a wide range of .Net Framework versions). 
The samples target .Net 6 though, so the following is required for running them:

* Compatible version of Visual Studio (2022+) and the .Net 6 SDK

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
StpSDKSample.exe StpApp:StpHost="10.2.10.70"
```

Notice that the name of the `appsettings.json` section containing the application parameters - `StpApp` - needs to be used as a prefix to each parameter, as shown in the example above

Parameters may also be set via environment variables, for example by setting a variable `StpApp__StpHost` to `10.2.10.70'. 

## Running the samples

* Build the app using Visual Studio Community or Code
* Start STP 
    * Follow the install and operation instructions provided by Hyssos
    * For this sample, the STP Development Core configuration provides the required services, but the app works as well
    with the STP Desktop configuration
    * The samples already include [STP's .NET SDK nuget package](https://www.nuget.org/packages/HyssosTech.Sdk.STP/), and are ready to be run. Other applications would need to install that package to gain access to the SDK.
* Launch the app 
    * A connection to the STP server is established and a form is displayed. 
    * If an error message is displayed, verify that STP is running on the server at the address and port configured above, 
    and that the port is not being blocked by a firewall
* **NOTE**: STP's Speech Component must be running on the same box as the app, with access to a working microphone


## Entering symbols

**Point, Line, Area (PLA) Input**

Enter symbols by sketching a single Point, Line or Area to indicate the location, and then speak to specify the type
of symbol that is desired, for example:

* Sketch a point (or small line) and speak "Infantry Company", or "Recon Platoon", or "Stryker Brigade"
* Sketch a line and speak "Phase Line Blue", or "Company Boundary", or "Main Attack Boston"
* Sketch an area and speak "Objective Bravo" or "Assembly Area"

NOTE: when entering PLA sketches, make sure that the corresponding option is selected on the top, left app settings dropdown. That sets STP's stroke segmentation to be
the fastest possible, with no waiting after each stroke is entered.

**Fully sketched / drawn Symbol Input**

Symbols can also be entered by drawing their appearance, according to standard 2525/APP6 specification, for example:

* A rectangle (in one or more strokes) with crossed internal lines (two more strokes) to represent an infantry unit

NOTE: when drawing symbols, the Drawing option of the top, left app settings drop-down must be selected. If attempting to draw while in PLA mode,
STP will start processing a stroke before the next stroke can be drawn, and just part of the drawing will usually be considered for recognition.

**Recognition Results**

Successful recognition of the symbol results in:

* The display of some symbol properties in an editable property grid
* Alternative symbol interpretations are loaded into a datagrid, supporting user selection
* Simple rendering showing the icon of the last recognized symbol on the map


## Brief code walkthrough

### Initialization 

**Connector Plugin** - The first step it to create a connection object that will provide the basic communication services to STP. In this quickstart app, we employ a sockets connector that communicates with STP's native OAA Publish Subscribe services. This plugin ships with the SDK.

Other plugins can be developed to implement different communication mechanisms, for example plain REST calls, or based on some event queue mechanism used by the backend infrastructure into which STP may have been embedded. An example of a websockets plugin serving JavaScript clients is posted [here](https://github.com/hyssostech/sketch-thru-plan-sdk-js/tree/main/plugins/connectors). While in a different language, that code illustrates the principles that could be used to generate a .NET version with similar capabilities.

```cs
// Create an STP connection object - using STP's native pub/sub system
var stpConnector = new StpOaaConnector(_logger, _appParams.StpHost, _appParams.StpPort);
```

**STP recognizer initialization** - communication with STP is achieved via recognizer object that takes the connector as a parameter 

```cs
// Initialize the STP recognizer with the connector definition
_stpRecognizer = new StpRecognizer(stpConnector);
```

**Event subscription** - Before connecting to STP, it is important to subscribe to the events of interest. This information is used by the SDK to build the corresponding subscription parameters that tell STP which events/messages to send to this client app.

STP triggers events asynchronously as user actions are interpreted as military symbols (and other types of entities, not covered here). The samples subscribe to different sets of the following events:

* Symbols
    * OnSymbolAdded - invoked whenever a new symbol is created as a result of successful combination of user speech and sketch
    * OnSymbolModified - invoked whenever the properties of a symbol are modified
    * OnSymbolDeleted - invoked whenever the a symbol is deleted/removed
* Edit Operations
    * OnSymbolEdited - invoked whenever a UI operation (e.g. selection) is triggered
    * OnMapOperation - invoked whenever a map operation such as zoom or pan is triggered
* Tasking
    * OnTaskAdded - invoked whenever a new task is created as a result of successful combination of multiple individual symbols (e.g. a Unit, an Axis of Advance 
    and an Objective area)
    * OnTaskModified - invoked whenever the properties of a task are modified
    * OnTaskDeleted - invoked whenever the a task is deleted/removed
* Task Org / ORBAT
    * OnTaskOrgUnitAdded invoked whenever a new task org unit is created
    * OnTaskOrgUnitModified - invoked whenever the properties of a task org unit are modified
    * OnTaskOrgUnitDeleted - invoked whenever the a task org unit is deleted/removed
    * OnTaskOrgRelationshipAdded invoked whenever a new task org relationship is created
    * OnTaskOrgRelationshipModified - invoked whenever the properties of a task org relationship are modified
    * OnTaskOrgRelationshipDeleted - invoked whenever the a task org relationship is deleted/removed
* Interface feedback 
    * OnSpeechRecognized - provides feedback on the speech to text interpretation. This event is optional, but provides meaningful feedback to users
    * OnListeningStateChanged - indicates when audio starts to be collected, and when it is no longer collected
    * OnSketchRecognized - indicates that STP has analyzed the strokes and produced an interpretation that is going to be potentially fused with speech next
    * OnSketchIntegrated - indicates that strokes have been successfully fused with speech. This event is useful for removing ink that either resulted in a successful symbol interpretation, or was dropped from consideration, to keep the interface clean
* Infrastructure events
    * OnStpMessage - is triggered by STP when a message needs to be brought to the user's attention, for example notices of disconnection, communication failures.
    * OnConnectionError - is triggered when the communication to the STP engine is interrupted and cannot be restored.
    * OnShutdown - when the STP engine is stopped, a message is sent to clients advertising the fact. Clients may have the option to shut themselves down as well (as is done in the samples), or notify the user.


**Event handling**

As an example of STP event handling, new symbol notifications can be handled as illustrated below. Similar code is employed to handle other STP events. Refer to the source of each sample for additional details: 

```cs
private void StpRecognizer_OnSymbolAdded(string poid, StpItem stpItem, bool isUndo)
{
    // Get the recognized item as a military symbol - not interested in other types of objects 
    if (stpItem is StpSymbol stpSymbol)
    {
        _currentSymbol = stpSymbol;
        DisplaySymbol(stpSymbol);
    }
}
```

A key aspect to notice is that the SDK makes available generic `StpItem` references. This type represents common generic symbol contents, and is the umbrella type for all symbols recognized by STP. STP is configurable, and can generate symbols for custom domains (e.g. Emergency Services), if setup to do so. Here we focus on the military symbology configuration.

Military symbols are represented by the `StpSymbol` subclass. Upon receiving an event, the generic `StpItem` reference needs to be cast to `StpSymbol` as shown above to yield the military-specific properties.


**Connection to STP** - Once the SDK object is configured, the connection itself can be attempted. Connection failures may be surfaced as exceptions. 
The name of the connecting app is provided as a parameter. This name identifies
the app's actions in messages and can be examined in logs, so it is recommended to set
it to a representative name.


```cs
bool success;
try
{
    success = _stpRecognizer.ConnectAndRegister("<AppName>");
}
catch
{
    success = false;
}
```

NOTE: it is important that the event handlers be defined *before* connecting to STP. The event subscriptions are used by the SDK to generate specific message/channel subscriptions, so that just those messages a client is actually interested in are served by STP's pubsub infrastructure.

**Sketch events processing**

STP operates primarily by fusing speech with sketched gestures that indicate where users intend to place symbols. 
A key requirement is therefore that client apps make available this information for STP to process.

For convenience, samples share a common [Mapping](../plugins/Mapping/SimpleMapPlugin/) functionality, that handles the lower-level pen/mouse 
events and symbol rendering, and expose them as higher-level events and methods.
Further details are provided in that component's [documentation](../plugins/Mapping/SimpleMapPlugin/README.md).

The samples subscribe to `OnPenDown` and `OnStrokeComplete` exposed by the `Mapping` class to be notified of the skecth-related events that need to be relayed to STP.

```cs
// Hook up to the map handler
_mapHandler = new Mapping(pictureMap, _appParams);
_mapHandler.OnPenDown += MapHandler_OnPenDown;
_mapHandler.OnStrokeCompleted += MapHandler_OnStrokeCompleted;
```

The handlers relay the pen down and stroke over to STP via the SDK. 

```cs
private void MapHandler_OnPenDown(object sender, LatLon geoPoint)
{
    // Notify STP of the start of a stroke and activate speech recognition
    _stpRecognizer.SendPenDown(geoPoint, DateTime.Now);
}
```

Completed strokes are similarly relayed to STP. The samples use a simple mechanism to detect intersection with placed symbols, encapsulated in the commom `Mapping` class, and available via the `IntersectedSymbols` method.
That methods takes a list of current symbols and returns the unique ids of those that do get intersected by the latest stroke.

```cs
private void MapHandler_OnStrokeCompleted(object sender, Mapping.PenStroke penStroke)
    // To support multimodal symbol editing, it is necessary for the app to
    // identify the existing elements that a stroke intersects, for example, 
    // a point or line over a unit that one wants to move, delete, or change 
    // attributes
    List<string> intersectedPoids =
        _currentSymbol is null
            ? null
            : _mapHandler.IntesectedSymbols(_currentSymbols);

    // Send sketch to STP for processing and potential fusion with speech
    _stpRecognizer.SendInk(penStroke.PixelBounds,
                            penStroke.TopLeftGeo,
                            penStroke.BotRightGeo,
                            penStroke.Stroke,
                            penStroke.TimeStart,
                            penStroke.TimeEnd,
                            intersectedPoids);
}
```

The sketches sent over to STP are potentially fused with speech by the Engine, which 
will then send possible interpretations (asynchronously) to all client apps, through the `OnSymbolAdded`, `OnSymbolModified`, 
`OnSymbolDeleted`, and sometimes `OnSymbolEdited` or `OnMapOperation`.

**Display and rendering**

Samples display information received from STP in different ways, but in general events received are listed in a log window,
besides being displayed in other UI elements such as a list of alternates, or property panels. 

Rendering proper is handled by the common [Mapping](../plugins/Mapping/SimpleMapPlugin) functionality, via the `RenderSymbol` method:

```cs
_mapHandler.ClearMap();
...
_mapHandler.RenderSymbol(stpSymbol);
```

Some of the samples render just the most current symbol, others may render multiple, so the details around this call may vary,
with the map being cleared at different opportunities, for example.

**Other user feedback**

To provide users visibility into the collection and interpretation of sketch and speech, a few events can are made available by STP:

* `OnListeningStateChanged` - provides an indication of the state of the audio capture device - listening to user input, or off
* `OnSpeechRecognized` - communicates the results of the speech transcription. Displaying that is useful to indicate
to users that they speech was processed, and show what the different interpretations were. 
Once STP has fused the speech and sketch and identified an intended symbol, the speech that resulted in the best match is then propagated, this time in ALL CAPS
* `OnSketchRecognized` - indicates that user sketches have been analyzed and that interpretations have been produced,
which will be matched to speech next to extract a combined/fused interpretation
* `OnSketchIntegrated` - indicates that user sketches have been successfully fused into some interpretation,
and can therefore be removed from display to avoid cluttering the interface.

These events and their role in providing users visibility into STP's processing are further detailed in the [Editing Sample](./EditingSample). 


**Infrastructure events**

While removing the strokes is a common strategy, other approaches are also possible and have been implemented in
other apps that are STP-enabled, for example:
* Leaving all the previous sketches on display, and offering an UI control to clean them out at once on demand
* Placing the strokes on a separate layer that users can opt to turn on and off

`OnConnectionError` is triggered when there is a failure communicating with the STP Engine. That in general
is a fatal error, as it becomes no longer possible to use speech and sketch. 
Other approaches are possible, such as attempting to restore the functionality, or falling back to manual 
planning.

The samples terminate on such events.

```cs
private void StpRecognizer_OnConnectionError(StpCommunicationException sce)
{
    MessageBox.Show(
        "Connection to STP was lost. Verify that STP is running and restart this app", 
        "Connection Lost", MessageBoxButtons.OK);
    Application.Exit();
}
```

`OnStpMessage` conveys messages generated by STP internally, expressing some condition requiring user attention, such as
the failure of a critical component. 

```cs
private void StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel level, string msg)
{
    ShowStpMessage(msg);
}
```

`OnShutdown` is issued by STP when it has been terminated. This gives dependent applications the 
opportunity to shut themselves off, or to otherwise warn users.

The samples turn themselves off upon receiving this notification.

```cs
private void StpRecognizer_OnShutdown()
{
    Application.Exit();
}
