# Quickstart Overview


This sample app provides a bare bones but meaningful introduction to the capabilities of the Sketch-Thru-Plan .NET SKD. I focuses primarily on symbol creation, with placeholder rendering capabilities, meant to be replaced by production quality rendering in a real application. 


# Adding military symbols to a maps via Speech and Sketch

The quickstart demonstrates how sketches (and optionally speech) collected by an app can be sent for processing by Sketch-thru-Plan (STP) for interpretation. If successfully interpreted, the combined/fused speech and sketch are turned into military symbols representation by STP, and sent back to the app for display and rendering.

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
StpSDKSample.exe App:StpHost="10.2.10.70"
```

Notice that the name of the `appsettings.json` section containing the application parameters - `App` - needs to be used as a prefix, as shown in the example above

## Running the  sample

* Build the app using Visual Studio Community or Code
* Start STP 
    * Follow the install instructions provided by Hyssos
    * For this sample, the STP Server Core configuration provides the required services, but the app works as well
    with STP Desktop or STP Server Advanced configurations
* Launch the quickstart app 
    * A connection to the STP server is established and a form is displayed. 
    * If an error message is displayed, verify that STP is running on the server at the address and port configured above, 
    and that the port is not being blocked by a firewall
* **NOTE**: STP's Speech component must be running on the same box as the app, with access to a working microphone


## Entering symbols

** Point, Line, Area (PLA) Input**

Enter symbols by sketching a single Point, Line or Area to indicate the location, and then speak to specifiy the type
of symbol that is desired, for example:

    * Sketch a point (or small line) and speak "Infantry Company", or "Recon Platoon", or "Stryker Brigade"
    * Sketch a line and speak "Phase Line Blue", or "Company Boundary", or "Main Attack Boston"
    * Sketch an area and speak "Objective Bravo" or "Assembly Area"

NOTE: when entering PLA sketches, select the corresponding option on the top, left dropdown. That sets STP's stroke segmentation to be
the fastest possible, with no waiting after each stroke is entered.

** Fully sketched / drawn Symbol Input**

Symbols can also be entered by drawing their appearance, according to standard 2525/APP6 specification, for example:
    * A rectangle(in one or more strokes) with crossed internal lines (two more strokes) to represent an infantry unit

NOTE: when drawing symbols, the Drawing option on the top, left drop-down must be selected. If attempting to draw while in PLA mode
STP will start processing before the next stroke can be drawn, and just part of the drawing will be considered for recognition.

** Recognition Results**

Successful recognition of the symbol results in the display of some symbol properties, a summary of the alternative interpretations of the 
symbol, and very simple rendering - colored dots and lines representing units and Tactical Graphics. These are replaced by the recognition results 
of the next symbol that is entered.


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

**Event subscription** - Before connecting to STP, it is important to subscribe to the handlers of interest. This information is used by the SDK to build the corresponding subscription parameters that tell STP which events/messages to send to this client app.

STP triggers events asynchronously as user actions are interpreted as military symbols (and other types of entities, not covered here). This quickstart subscribes to:

* Symbols
    * OnSymbolAdded - invoked whenever a new symbol is created as a result of successful combination of user speech and sketch
    * OnSymbolModified - invoked whenever the properties of a symbol are modified
    * OnSymbolDeleted - invoked whenever the a symbol is deleted/removed
* Tasks
    * OnTaskAdded - invoked whenever a new task is created as a result of successful combination of related symbols
    * OnTaskModified - invoked whenever the properties of a task are modified
    * OnTaskDeleted - invoked whenever the a task is deleted/removed
* UI 
    * OnInkProcessed - invoked when strokes have been processed by STP. This event is useful for removing ink that either resulted in a successful symbol interpretation, or was dropped from consideration, to keep the interface clean
    * OnSpeechRecognized - provides feedback on the speech to text interpretation. This even is optional, but provides meaningful feedback to users. 
* Infrastructure events
    * OnStpMessage - is triggered by STP when a message needs to be brought to the user's attention, for example notices of disconnection, communication failures.
    * OnConnectionError - is triggered when the communication to the STP engine is interrupted and cannot be restored.
    * OnShutdown - when the STP engine is stopped, a message is sent to clients advertising the fact. Clients may have the option to shut themselves down as well (as is done in this quickstart), or notify the user.


```cs
// Hook up to the events _before_ connecting, so that the correct message subscriptions can be identified
// A new symbol has been recognized and added
_stpRecognizer.OnSymbolAdded += StpRecognizer_OnSymbolAdded;
_stpRecognizer.OnSymbolModified += StpRecognizer_OnSymbolModified;
_stpRecognizer.OnSymbolDeleted += StpRecognizer_OnSymbolDeleted;

_stpRecognizer.OnTaskAdded += StpRecognizer_OnTaskAdded;
_stpRecognizer.OnTaskModified += StpRecognizer_OnTaskModified;
_stpRecognizer.OnTaskDeleted += StpRecognizer_OnTaskDeleted;
_stpRecognizer.OnSpeechRecognized += StpRecognizer_OnSpeechRecognized;
_stpRecognizer.OnInkProcessed += StpRecognizer_OnInkProcessed;

_stpRecognizer.OnStpMessage += StpRecognizer_OnStpMessage;
_stpRecognizer.OnConnectionError += StpRecognizer_OnConnectionError;
_stpRecognizer.OnStpMessage += StpRecognizer_OnStpMessage;
_stpRecognizer.OnShutdown += StpRecognizer_OnShutdown;
```

As an example of STP event handling, new symbol notifications can be handled as illustrated below. Similar code is employed to handle other STP events. Refer to the source for additional details - [Forms1.cs](./Form1.cs): 

```cs
private void StpRecognizer_OnSymbolAdded(string poid, StpItem stpItem, bool isUndo)
{
    if (this.InvokeRequired)
    {   // recurse on GUI thread if necessary
        this.Invoke(new MethodInvoker(() => StpRecognizer_OnSymbolAdded(poid, stpItem, isUndo)));
        return;
    }

    // Get the recognized item as a military symbol - not interested in other types of objects 
    if (stpItem is StpSymbol stpSymbol)
    {
        DisplaySymbol(stpSymbol);
    }
}
```

A key aspect to notice is that the SDK makes available generic `StpItem` references. This type represents common generic symbol contents, and is the umbrella type for all symbols recognized by STP. STP is configurable, and can generate symbols for custom domains (e.g. Emergency Services), if setup to do so. Here we focus on the military symbology configuration.

Military symbols are represented by the `StpSymbol` subclass. Upon receiving an event, the generic `StpItem` reference needs to be cast to `StpSymbol` to yeld the military-specific properties.

```cs
    // Get the recognized item as a military symbol - not interested in other types of objects 
    if (stpItem is StpSymbol stpSymbol)
    {
        DisplaySymbol(stpSymbol);
    }
```


**Connection to STP** - Once the SDK object is configured, the connection itself can be attempted. Connection failures are surfaced as exceptions (generated by javascript Promise rejections)


```cs
try
{
    _stpRecognizer.ConnectAndRegister("StpSDKSample");
}
catch
{
    MessageBox.Show("Failed to connect to STP. Please make sure it is running and try again", "Could not connect to  agents");
    Application.Exit();
}
```

*NOTE* it is important that the event handlers be defined *before* connecting to STP. The events that are handled will be used to generate specific message subscriptions, so that just those messages a client is actually interested in are served by STP's pubsub infrastructure.

### Providing sketch events to STP

STP is map agnostic - any solution can be used as the underlying surface for sketching, provided that the latitude and longitude coordinates (in decimal degrees) of the sketches can be obtained.

In this sample, a map image for which the corner coordinates are provided is used as a standin for a real mapping surface.


STP requires two events to be raised when the user sketches: 

1. A pen down event that signals the start of a sketched gesture
2. The completed stroke that follows

For convenience, it is also useful to be able to detect when STP-placed features are selected, so that additional information or actions can be performed as a response.

While code in applications embedding the STP SDK will likely use different approaches, in this sample, a PictureBox is used to present the map image and invoke handlers when the events above take place. 

**Map initialization** - Each mapping system will require its own particular initialization. In this sample, the constructor takes the API key, the  Id of the HTML div where map is presented, and the initial center and zoom.

```cs
        pictureMap.Image = _mapImage;
        pictureMap.MouseDown += PictureMap_MouseDown;
        pictureMap.MouseMove += PictureMap_MouseMove;
        pictureMap.MouseUp += PictureMap_MouseUp;
```

**Sending pen down on stroke start** - The pen down latitude and longitude location and the time (UTC in ISO-8601 format) are provided as parameters. Notice that the sample also starts speech recognition at the time a stroke is started (see additional details further down)

```cs
private void PictureMap_MouseDown(object sender, MouseEventArgs e)
{
    _stroke = new List<LatLon>();

    var geo = GeoCoordinatesAt(e.Location);
    _stroke.Add(geo);

    // Notify STP of the start of a stroke and activate speech recognition
    _stpRecognizer.SendPenDown(geo, DateTime.Now);
    _timeStart = DateTime.Now;
}
```

**Collecting coordinates** - STP will require the user sketch to be provided when a stroke has been completed (on pen up). These need therefore to be captured as the mouse is moved. Users will also require feedback, so a line is drawn to provide that.

```cs
private void PictureMap_MouseMove(object sender, MouseEventArgs e)
{
    if (_stroke == null) 
        return;

    // Collect the coordinates as the user sketches to be able to send over to STP when done
    var geo = GeoCoordinatesAt(e.Location);
    _stroke.Add(geo);

    if (_lastX == -1)
    {
        _lastX = e.Location.X;
        _lastY = e.Location.Y;
    }

    using (var penLine = new Pen(Color.Red, 4))
    using (var g = pictureMap.CreateGraphics())
    {
        g.DrawLine(penLine, new Point(_lastX, _lastY), new Point(e.Location.X, e.Location.Y));
    }

    _lastX = e.Location.X;
    _lastY = e.Location.Y;
}
```

**Sending the stroke** - Once the stroke is completed (on pen up), the full stroke is sent to STP for processing, informing the size of the visible map extent (in this case the PictureBox image) in pixels, Geo coordinates of the top,left and bottom,right corners of the extent, the stroke geo coordinates (accumulated while the mouse was moved), the time interval and intersected poids (discussed further below):

```cs
private void PictureMap_MouseUp(object sender, MouseEventArgs e)
{
    if (_stroke == null) return;

    _timeEnd = DateTime.Now;
    var pixBounds = new Size(pictureMap.Width, pictureMap.Height);
    var topLeftGeo = GeoCoordinatesAt(new Point(0, 0));
    var botRightGeo = GeoCoordinatesAt(new Point(pictureMap.Width, pictureMap.Height));

    // To support multimodal symbol editing, it is necessary for the app to identify the existing elements
    // that a stroke intersects, for example, a point or line over a unit that one wants to move, delete,
    // of change attributes.
    // Here we just return null for simplicity sake. That will prevent editing of symbols placed on this app
    List<string> intersectedPoids = null;

    _stpRecognizer.SendInk(pixBounds,
                            topLeftGeo,
                            botRightGeo,
                            _stroke,
                            _timeStart, _timeEnd,
                            intersectedPoids);
    _stroke = null;
    _lastX = _lastY = -1;
}
```

STP supports symbol editing by sketching a mark over a symbol and then speaking a value for a property of that symbol, for example, "echelon platoon" to set or change a unit's echelon. Other edits are also supported, for example speaking "delete this" to remove a symbol, or "move here", while sketching a line that starts inside a symbol and ends in the desired new position.

In order to support that, client apps are required to identify the symbols that are currently on display that are intersected by a stroke. This quickstart does not support that, and simply provides `null`. As a result, editing capabilities are *not* supported by the app. 

**Speech** - Speech is collected and processed by STP's Speech component, which must be running alongside the app. 

To provide users feedback, this quickstart handles STP's speech recognition events, displaying the alternate transcriptions on a TextBox. 

```cs
private void StpRecognizer_OnSpeechRecognized(List<string> speechList)
{
    if (speechList != null && speechList.Count > 0)
    {
        // Show just top 5 to avoid best being hidden by scroll
        int max = speechList.Count > 5 ? 5 : speechList.Count;
        string concat = string.Join(" | ", speechList.GetRange(0, max));
        ShowSpeechReco(concat);
    }
}
```
On successfull recognition, the TextBox is updated to show the actual language that got fused with a sketch to generate the symbol - that is shown in all caps to distinguish from raw recognition.


### Symbol rendering

STP returns coordinates set according to the 2525/APP6 standard anchor points. These are normally what is required to drive a standards-based renderer. 

This quickstart use a bare bones placeholder renderer which display single point symbols as circles colored according to their affiliation e.g. blue fro friendly, red for enemy. Tactical Graphics are displayed by simple lines.
