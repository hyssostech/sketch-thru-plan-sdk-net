# Simple mapping plugin

STP is map agnostic - any solution can be used as the underlying surface for sketching and rendering, provided that the latitude and longitude
coordinates (in decimal degrees) of the sketches can be obtained.

This plugin implements basic mapping services for the purposes of providing capabilities for the STP SDK samples.
Internally a PictureBox with a static image is used.
Coordinate conversion is performed based on top,left and bottom, right coordinates provided as parameters,
without considering Earth's curvature. 

## Sketch events

One of the main functionalities provided is the capture of user sketching actions over the map.
Mouse/stylus user actions handled by this class
are exposed as a `pen down` and `pen stroke` events. 
The [sample apps](../../../samples/) subscribe to these events, and then send them over to STP via the SDK (see sample app documentation for details). 


```cs
/// <summary>
/// Event triggered at the start of a sketched stroke (pen/mouse down)
/// </summary>
public event EventHandler<LatLon> OnPenDown;
/// <summary>
/// Event triggered at the end of a sketched stroke (pen/mouse up)
/// </summary>
public event EventHandler<PenStroke> OnStrokeCompleted;
```

On stroke completion, the definition of the current map region/extent (fixed in this simple implementation) is returned, along with the
stroke geo coordinates and timing.

```cs
/// <summary>
/// Pen stroke class
/// </summary>
public class PenStroke
{
    /// <summary>
    /// Size of the map region/extent in pixels
    /// </summary>
    public Size PixelBounds { get; set; }
    /// <summary>
    /// Top, left geo coordinate of the map region/extent
    /// </summary>
    public LatLon TopLeftGeo { get; set; }
    /// <summary>
    /// Bottom, right geo coordinate of the map region/extent
    /// </summary>
    public LatLon BotRightGeo { get; set; }
    /// <summary>
    /// Sketch/stroke points in geo coordinates
    /// </summary>
    public List<LatLon> Stroke { get; set; }
    /// <summary>
    /// Time the stroke started (pen/mouse down)
    /// </summary>
    public DateTime TimeStart { get; set; }
    /// <summary>
    /// Time the stroke ended (pen/mouse up)
    /// </summary>
    public DateTime TimeEnd { get; set; }
}
```

## Stroke intersection detection

STP supports symbol editing by sketching a mark over a symbol and then speaking a value of a property of that symbol, 
for example, "echelon platoon" to set or change a unit's echelon. Other edits are also supported, for example speaking "delete this" to remove a symbol, or "move here", while sketching a line that starts inside a symbol and ends in the desired new position.

In order to support that, client apps are required to identify the symbols that are currently on display that get intersected by a stroke. 
This plugin includes a simple mechanism for illustrative purposes. A real application requires a mechanism 
that is optimized and able to handle a larger number of symbols. 

```cs
public List<string> IntesectedSymbols(List<StpSymbol> symbols)
{
    List<string> intersectedPoids = new();
    if (symbols is null)
    {
        return null;
    }

    // Basic implementation, that renders each symbol on its own layer and 
    // performs a bitwise comparison between the rendered bits and the stroke 
    // rendering - stroke and symbol intersect if they share a non-empty byte 
    // at the same position
    byte[] strokeBytes = null;
    Rectangle strokeBounds = BoundingRect(_strokesPixels.Last(), StrokeWidth);
    foreach (var symbol in symbols)
    {
        // Render symbol on its own overlay 
        Image bmp = new Bitmap(_inkOverlay.Width, _inkOverlay.Height);
        List<Rectangle> symbolBounds = RenderSymbolOnOverlay(symbol, bmp);
        // If stroke bounds overlap with the symbol's, check in more detail
        if (symbolBounds.Select(b => b.IntersectsWith(strokeBounds)).Any())
        {
            if (strokeBytes is null)
            {
                // Cache the stroke bytes if this is the first time checking its contents
                strokeBytes = GetImageBytes((Bitmap)_inkOverlay);
            }
            // Get the rendered symbol image as bytes
            byte[] symbolBytes = GetImageBytes((Bitmap)bmp);

            // Check for intersection with stroke by comparing byte by byte
            bool intersects = Intersect(symbolBytes, strokeBytes);

            // Add poid to list if intersects
            if (intersects)
            {
                intersectedPoids.Add(symbol.Poid);
            }
        }
    }
    // Null if no intersection
    return intersectedPoids.Count == 0 ? null : intersectedPoids;
}
```

## Symbol rendering

In this sample plugin, three images are used to hold 1) the background map, 2) rendered symbols and 3) sketched ink. That makes it possible to 
erase the ink and keep 
added symbols visible, and to revert to an empty map if the user chooses to reset.

To get an image representing a (single point) symbol, the `StpSymbol` `Bitmap` method is used. Under the hood this method invokes JMSML SVG functionality. Single point symbol images are built using the SVG representations 
provided by the
[Joint Military Symbology XML / JMSML](https://github.com/Esri/joint-military-symbology-xml) project.


```cs
const int RenderSize = 100;
Image symbolImage = stpSymbol.Bitmap(RenderSize, RenderSize);
```

STP returns location coordinates set according to the 2525/APP6 standard anchor points. These are normally what is required to drive a standards-based military
renderer. 
In this simple implementation, multipoint symbols (e.g. Tactical Graphics) are rendered as simple lines. 
The SDK provides a utility method - `GetLinearSymbolCoords` - that convert most symbols' anchor points to a simpler linearized representation.

```cs
// Get anchor points dumbed down to a line
var coords = stpSymbol.GetLinearSymbolCoords();
for (int i = 0; i < coords.Count - 1; i++)
{
    bounds.Add(DrawLineSegment(coords[i], coords[i + 1], CoordSpace.Geo, overlay, color));
}
```

## SVG content location

The content of the `svg` JMSML folder needs to be available somewhere accessible to the app. STP installs these files
in `C:\ProgramData\STP\JMS\SVG` by default. If running the STP Engine on the same machine as the app, nothing needs to be changed. If running the app on a different machine,
then download the required [SVG metadata](../../../samples/svg.zip) and extract into an accessible location. 

If the content is extracted to a location that is not the default `C:\ProgramData\STP\JMS\` (preferred), then the recognizer will
need to be configured by setting the `JMSSVGPath` as part of the client app initialization:

```cs
_stpRecognizer.JMSSVGPath = @"<path to the folder containing the required SVG definition>"
```