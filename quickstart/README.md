# Quickstart Overview


This sample app provides a bare bones but meaningful introduction to the capabilities of the Sketch-Thru-Plan .NET SKD. It focuses primarily on symbol creation, with placeholder rendering capabilities, meant to be replaced by production quality rendering in a real application. 

[Additional sample applications](../samples) with enhanced capabilities are available. The [Editing sample](../samples/EditingSample/), for example, extends this quickstart with foundational editing capabilities, providing a good next step.  


# Adding military symbols to a map via Speech and Sketch

The quickstart demonstrates how collected sketches and speech can be sent to Sketch-thru-Plan (STP) for interpretation. If successfully interpreted, the combined/fused speech and sketch are turned into military symbols representation by STP, and sent back to the app for display and rendering via asynchronous events.

## Prerequisites

* Sketch-thru-Plan (STP) Engine (5.6.0+) running on localhost or on an accessible machine
* A machine with a working microphone
* STP's Speech Component running on the same machine as the app

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

Notice that the name of the `appsettings.json` section containing the application parameters - `App` - needs to be used as a parameter prefix, as shown in the example above.

## Running the  sample

* Build the app using Visual Studio Community (or Visual Studio Code if preferred)
* Start STP 
    * Follow the install and operation instructions provided by Hyssos
    * For this sample, the STP Development Core configuration provides the required services, but the app works as well
    with the STP Desktop configuration
    * The sample already includes [STP's .NET SDK nuget package](https://www.nuget.org/packages/HyssosTech.Sdk.STP/), and is ready to be run. Other applications would need to install that package to gain access to the SDK.
* Launch the quickstart app 
    * A connection to the STP server is established and a form is displayed. 
    * If an error message is displayed, verify that STP is running on the server at the address and port configured above, 
    and that the port is not being blocked by a firewall
* **NOTE**: STP's Speech component must be running on the same box as the app, with access to a working microphone


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

Successful recognition of the symbol results in the display of some of the symbol properties, a summary of the alternative interpretations of the 
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

**STP recognizer initialization** - communication with STP is achieved via a recognizer object that takes the connector plugins as a parameter 

```cs
// Initialize the STP recognizer with the connector definition
_stpRecognizer = new StpRecognizer(stpConnector);
```

**Event subscription** - Before connecting to STP, it is important to subscribe to the events of interest. This information is used by the SDK to build the corresponding subscription parameters that tell STP which events/messages to send to this client app.

STP triggers events asynchronously as user actions are interpreted as military symbols (and other types of entities, not covered here). This quickstart subscribes to:

* Symbols
    * OnSymbolAdded - invoked whenever a new symbol is created as a result of successful combination of user speech and sketch
    * OnSymbolModified - invoked whenever the properties of a symbol are modified
    * OnSymbolDeleted - invoked whenever the a symbol is deleted/removed

    NOTE:  this app does not provide means for symbol deletion or modification on its own, but will display updates entered via other interfaces, for example Hyssos ArcGIS Pro addin. 
For a sample that does support this functionality directly, see the [Editing sample](../samples/EditingSample/).
* UI 
    * OnSpeechRecognized - provides feedback on the speech to text interpretation. This even is optional, but provides meaningful feedback to users. 
    * OnSketchIntegrated - invoked when strokes have been processed by STP. This event is useful for removing ink that either resulted in a successful symbol interpretation, or was dropped from consideration, to keep the interface clean
* Infrastructure events
    * OnStpMessage - is triggered by STP when a message needs to be brought to the user's attention, for example notices of internal failures.
    * OnConnectionError - is triggered when the communication to the STP engine is interrupted and cannot be restored.
    * OnShutdown - when the STP engine is stopped, a message is sent to clients advertising the fact. Clients then have the option to shut themselves down as well (as is done in this quickstart), or notify the user that speech and sketch input 
    is no longer available.




```cs
// Hook up to the events _before_ connecting, so that the correct message 
// subscriptions can be identified
_stpRecognizer.OnSymbolAdded += StpRecognizer_OnSymbolAdded;
_stpRecognizer.OnSymbolModified += StpRecognizer_OnSymbolModified;
_stpRecognizer.OnSymbolDeleted += StpRecognizer_OnSymbolDeleted;

_stpRecognizer.OnSpeechRecognized += StpRecognizer_OnSpeechRecognized;
_stpRecognizer.OnInkProcessed += StpRecognizer_OnSketchIntegrated;

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
        this.Invoke(new MethodInvoker(
            () => StpRecognizer_OnSymbolAdded(poid, stpItem, isUndo)));
        return;
    }

    // Get the recognized item as a military symbol
    // Not interested in other types of objects 
    if (stpItem is StpSymbol stpSymbol)
    {
        DisplaySymbol(stpSymbol);
    }
}
```

A key aspect to notice is that the SDK makes available generic `StpItem` references. This type represents common generic symbol content, and is the umbrella type for all symbols recognized by STP. STP is configurable, and can generate symbols for custom domains (e.g. Emergency Services), if setup to do so. Here we focus on the military symbology configuration.

Military symbols are represented by the `StpSymbol` subclass. Upon receiving an event, the generic `StpItem` reference needs to be cast to `StpSymbol` to yeld the military-specific properties.



**Connection to STP** - Once the SDK object is configured, the connection itself can be attempted. Connection failures are surfaced as exceptions. 
The name of the connecting app is provided as a parameter. This name identifies
the app's actions in messages and can be examined in logs, so it is recommended to set
it to a representative name.


```cs
try
{
    _stpRecognizer.ConnectAndRegister("StpSDKSample");
}
catch
{
    MessageBox.Show(
        "Failed to connect to STP. Please make sure it is running and try again", 
        "Could not connect to STP");
    Application.Exit();
}
```

NOTE: it is important that the event handlers be defined *before* connecting to STP. The event subscriptions are used by the SDK to generate specific message/channel subscriptions, so that just those messages a client is actually interested in are served by STP's pubsub infrastructure.

### Providing sketch events to STP

STP is map agnostic - any solution can be used as the underlying surface for sketching, provided that the latitude and longitude coordinates (in decimal degrees) of the sketches can be obtained.

In this sample, a map image for which the corner coordinates are provided is used as a standin for a real mapping surface.


STP requires two events to be raised when the user sketches: 

1. A pen down event that signals the start of a sketched gesture
2. The completed stroke that follows

While code in applications embedding the STP SDK will likely use different approaches, in this sample, a PictureBox is used to present the map image and invoke handlers when the events above take place. 

**Map initialization** - Each mapping system will require its own particular initialization. In this sample, PictureBox mouse events are handled to obtain the required sketch information.

```cs
        pictureMap.Image = _mapImage;
        pictureMap.MouseDown += PictureMap_MouseDown;
        pictureMap.MouseMove += PictureMap_MouseMove;
        pictureMap.MouseUp += PictureMap_MouseUp;
```

**Sending pen down on stroke start**

STP requires the latitude and longitude coordinates and the time (UTC in ISO-8601 format) to be provided as parameters for pen down notifications. 
A red "dot" is rendered to provide user feedback.

```cs
private void PictureMap_MouseDown(object sender, MouseEventArgs e)
{
    _stroke = new List<LatLon>();

    var geo = GeoCoordinatesAt(e.Location);
    _stroke.Add(geo);

    // Set the first anchor point
    _lastX = e.Location.X;
    _lastY = e.Location.Y;

    // Render a dot for first point  - might be the single one if just Point sketch
    using var g = pictureMap.CreateGraphics();
    using var brush = new SolidBrush(Color.Red);
    g.FillEllipse(brush, 
        e.Location.X - Thickness / 2, 
        e.Location.Y - Thickness / 2, 
        Thickness, Thickness);

    // Notify STP of the start of a stroke and activate speech recognition
    _stpRecognizer.SendPenDown(geo, DateTime.Now);
    _timeStart = DateTime.UtcNow;
}
```

**Collecting coordinates**

STP requires the user sketch to be provided when a stroke has been completed (on pen up). These need therefore to be captured as the mouse is moved. Users will also require feedback, so a line is rendered to provide that.

```cs
private void PictureMap_MouseMove(object sender, MouseEventArgs e)
{
    if (_stroke == null) 
        return;

    // Collect the coordinates as the user sketches to be able to send over to STP when done
    var geo = GeoCoordinatesAt(e.Location);
    _stroke.Add(geo);

    // Draw next segment
    using var g = pictureMap.CreateGraphics();
    using var penLine = new Pen(Color.Red, Thickness);
    g.DrawLine(penLine, new Point(_lastX, _lastY), new Point(e.Location.X, e.Location.Y));

    // Keep track of the end of the stroke to build the next segment
    _lastX = e.Location.X;
    _lastY = e.Location.Y;
}
```

**Sending the stroke**

Once a stroke is completed (on pen up), the full stroke is sent to STP for processing, informing the size of the visible map extent (in this case the PictureBox image) in pixels, Geo coordinates of the top,left and bottom,right corners of the extent, the stroke geo coordinates (accumulated while the mouse was moved), the time interval and intersected poids (discussed further below):

```cs
private void PictureMap_MouseUp(object sender, MouseEventArgs e)
{
    if (_stroke == null) return;

    _timeEnd = DateTime.Now;
    var pixBounds = new Size(pictureMap.Width, pictureMap.Height);
    var topLeftGeo = GeoCoordinatesAt(new Point(0, 0));
    var botRightGeo = GeoCoordinatesAt(new Point(pictureMap.Width, pictureMap.Height));

    // To support multimodal symbol editing, it is necessary for the app to
    // identify the existing elements that a stroke intersects, for example, 
    // a point or line over a unit that one wants to move, delete, or change 
    // attributes.
    // Here we just return null for simplicity sake. That will prevent editing 
    // of symbols placed on this app
    List<string> intersectedPoids = null;

    _stpRecognizer.SendInk(pixBounds,
                            topLeftGeo,
                            botRightGeo,
                            _stroke,
                            _timeStart, _timeEnd,
                            intersectedPoids);

    // Clear stroke now that is has been sent over to STP
    _stroke = null;
}
```

STP supports symbol editing by sketching a mark over a symbol and then speaking a value for a property of that symbol, for example, "echelon platoon" to set or change a unit's echelon. Other edits are also supported, for example speaking "delete this" to remove a symbol, or "move here", while sketching a line that starts inside a symbol and ends in the desired new position.

In order to support that, client apps are required to identify the symbols that are currently on display that are intersected by a stroke. This quickstart does not support that, and simply provides `null`. As a result, editing capabilities are *not* supported by the app. 
See the [Editing sample](../samples/EditingSample/) for an actual implementation of this capability.



**Speech**

Speech is collected and processed by STP's Speech component, which must be running alongside the app. 
Audio is activated when the app sends the Pen Down notifications, and remains open while the user is speaking, or a timeout elapses. 

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
On successful recognition, the TextBox is updated to show the actual language that got fused with a sketch to generate the symbol - that is shown in all caps to distinguish from raw recognition.

Lack of feedback is an indication that there is a problem with audio capture. 
Users should be directed to verify their microphones.
STP's Speech Component provides additional configuration and information to support this proces (see STP's Training Guide, included in the Engine install package). 


### Symbol rendering

This quickstart use a bare bones placeholder renderer which display single point symbols as circles colored according to their affiliation e.g. blue fro friendly, red for enemy. Tactical Graphics are displayed by simple lines.
