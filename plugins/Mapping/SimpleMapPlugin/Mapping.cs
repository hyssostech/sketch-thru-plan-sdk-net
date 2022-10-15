using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace StpSDK.Mapping;
public class Mapping : IMapping
{
    #region Public events
    /// <summary>
    /// Event triggered at the start of a sketched stroke (pen/mouse down)
    /// </summary>
    public event EventHandler<LatLon> OnPenDown;
    /// <summary>
    /// Event triggered at the end of a sketched stroke (pen/mouse up)
    /// </summary>
    public event EventHandler<PenStroke> OnStrokeCompleted;
    #endregion

    #region Public properties
    /// <summary>
    /// Pen width in pixes used to draw the sketched strokes. Defaults to 4
    /// </summary>
    public int StrokeWidth { get; set; }
    /// <summary>
    /// Color of a stroke as the user is sketching. Defaults to Red
    /// </summary>
    public Color StrokeSketchingColor { get; set; }

    /// <summary>
    /// Color of a stroke that has been processed. Defaults to Orange
    /// </summary>
    public Color StrokeProcessedColor { get; set; }
    /// <summary>
    /// Rendered symbol image size. Defaults to 100 x 100
    /// </summary>
    public Size SymbolRenderSize { get; set; }

    // Top, left map geo coordinates
    public LatLon TopLeftGeo => ControlToGeo(new Point(0, 0));
    // Bottom, right map geo coordinates
    public LatLon BotRightGeo => ControlToGeo(new Point(_mapControl.Width, _mapControl.Height));

    public BindingList<StpSymbol> DataSource 
    { 
        get => _dataSource; 
        set => SetDataSource(value); 
    }
    #endregion

    #region Private properties
    private ILogger _logger;

    private PictureBox _mapControl;
    private LatLon _mapTopLeft;
    private LatLon _mapBottomRight;

    private BindingList<StpSymbol> _dataSource;

    private Image _mapImage;
    private Image _symbolOverlay;
    private Image _inkOverlay;
    private Image _staleInkOverlay;
    private Bitmap _frameBuffer;

    /// <summary>
    /// State of stroke capture - true after pen/mousedown 
    /// </summary>
    private bool _isSketching;
    /// <summary>
    /// The current stroke we are building to eventually pass to STP
    /// </summary>
    private List<LatLon> _geoStroke;
    /// <summary>
    /// Stroke's screen coordinates for redrawing 
    /// </summary>
    private List<List<Point>> _strokesPixels;
    /// <summary>
    /// Last captured point, to make it easier to extend a stroke as the mouse moves
    /// </summary>
    private Point _lastPoint;
    /// <summary>
    /// Start and End times of current stroke
    /// </summary>
    private DateTime _timeStart, _timeEnd;
    #endregion

    #region Construction / teardown
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="mapControl"></param>
    /// <param name="mapImagePath"></param>
    /// <param name="mapTopLeft"></param>
    /// <param name="mapBottomRight"></param>
    public Mapping(ILogger logger,
        PictureBox mapControl,
        string mapImagePath,
        LatLon mapTopLeft, LatLon mapBottomRight)
    {
        _logger = logger;
        _mapControl = mapControl;
        _mapTopLeft = mapTopLeft;
        _mapBottomRight = mapBottomRight;

        _strokesPixels = new List<List<Point>>();


        // Load map provided as parameter and create image overlays of the same size
        _mapImage = Image.FromFile(mapImagePath);
        _symbolOverlay = new Bitmap(_mapImage.Size.Width, _mapImage.Size.Height);
        _inkOverlay = new Bitmap(_mapImage.Size.Width, _mapImage.Size.Height);
        _staleInkOverlay = null;

        // Handle sketching and resize events
        _mapControl.MouseDown += MapControl_MouseDown;
        _mapControl.MouseMove += MapControl_MouseMove;
        _mapControl.MouseUp += MapControl_MouseUp;
        _mapControl.SizeChanged += MapControl_SizeChanged;

        // Load initial map image
        LoadMapImage();

        // Default rendering parameters
        StrokeWidth = 4;
        StrokeSketchingColor = Color.Red;
        StrokeProcessedColor = Color.Orange;
        SymbolRenderSize = new Size(100, 100);
    }
    #endregion

    #region Sketching / mouse event handling
    /// <summary>
    /// The user has pressed the mouse/stylus to the map
    /// </summary>
    /// <remarks>
    ///  Capture the current position/time and call the SDK API SendPenDown() to 
    ///  inform STP system that we are in the process of drawing a stroke and to not segment until complete
    ///</remarks>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MapControl_MouseDown(object sender, MouseEventArgs e)
    {
        _isSketching = true;

        // Clear stale ink that may have not been integrated
        _staleInkOverlay = null;

        // Start stroke collection
        _timeStart = DateTime.Now;
        _geoStroke = new List<LatLon>();

        var geo = ControlToGeo(e.Location);
        _geoStroke.Add(geo);

        // Add a new segment to the pixel cache (may be drawing) and store the first anchor point
        _strokesPixels.Add(new List<Point>());
        _lastPoint = e.Location;
        _strokesPixels.Last().Add(ToImageSpace(_lastPoint, CoordSpace.Control));

        // Render a dot in case this is a single click (point) gesture
        Rectangle bounds = DrawPoint(e.Location, CoordSpace.Control, _inkOverlay, StrokeSketchingColor);
        UpdateMapImage(new List<Rectangle> { bounds });

        OnPenDown?.Invoke(this, geo);
    }

    /// <summary>
    /// The user is dragging/sketching the current stroke
    /// </summary>
    /// <remarks>
    ///  Add each new point to the current stroke and update the map display to show the new ink
    ///  </remarks>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MapControl_MouseMove(object sender, MouseEventArgs e)
    {
        // Skip moves that were not preceded by a pen down that initialized a stroke capture
        if (!_isSketching)
        {
            return;
        }

        // Collect the coordinates as the user sketches to be able to send over to STP when done
        var geo = ControlToGeo(e.Location);
        _geoStroke.Add(geo);

        // Render next segment
        Rectangle bounds = DrawControlSegmentInk(_lastPoint, e.Location, _inkOverlay, StrokeSketchingColor);
        UpdateMapImage(new List<Rectangle> { bounds });

        // Keep track of the end of the stroke to build the next segment
        _lastPoint = e.Location;
        _strokesPixels.Last().Add(ToImageSpace(_lastPoint, CoordSpace.Control));
    }

    /// <summary>
    /// The user has lifted the mouse/stylus
    /// </summary>
    /// <remarks>
    /// Send the current stroke and timing to STP for recognition
    /// If the stroke can be fused with speech, STP will issue a symbol event
    /// e.g. OnSymbolAdded, OnSymbolModified, OnSymbolRemoved
    /// </remarks>
    /// <param name="sender"></param>
    /// <param name="e"></param>    
    private void MapControl_MouseUp(object sender, MouseEventArgs e)
    {
        if (_geoStroke == null) return;

        _timeEnd = DateTime.Now;
        PenStroke penStroke = new()
        {
            PixelBounds = new Size(_mapControl.Width, _mapControl.Height),
            TopLeftGeo = TopLeftGeo,
            BotRightGeo = BotRightGeo,
            Stroke = _geoStroke,
            TimeStart = _timeStart,
            TimeEnd = _timeEnd,
        };
        OnStrokeCompleted?.Invoke(this, penStroke);

        // No longer capturing
        _isSketching = false;
    }

    /// <summary>
    /// Recreate frame when to match control size as it is changed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MapControl_SizeChanged(object sender, EventArgs e)
    {
        LoadMapImage();
    }
    #endregion

    #region Data source methods
    /// <summary>
    /// Set a data source that will drive rendering
    /// </summary>
    /// <param name="value"></param>
    private void SetDataSource(BindingList<StpSymbol> value)
    {
        // Remove events from previous source, if any
        if (_dataSource != null)
        {
            _dataSource.ListChanged -= DataSource_ListChanged;
        }
        // Update the source and setup the event listener
        _dataSource = value;
        _dataSource.ListChanged += DataSource_ListChanged;
    }
    /// <summary>
    /// Handle data source change events
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DataSource_ListChanged(object sender, ListChangedEventArgs e)
    {
        
        // Clear all current items to get back to an empty background
        ClearMap();

        // Render current symbols back again
        // NB: even when e.ListChangedType is Reset, there might be items to be rendered
        // TODO: this is overkill - there may be changes that do not require a full redraw
        // and those that require a redraw could be handled so as to affect just the regions
        // around the changed symbols
        foreach (var symbol in _dataSource)
        {
            RenderSymbol(symbol);
        }
    }
    #endregion

    #region Rendering
    /// <summary>
    /// Render symbol and update map image
    /// </summary>
    /// <param name="stpSymbol"></param>
    /// <param name="overlay">Image to render the symbol onto - defaults to the standard symbol overlay</param>
    /// <returns>Bounding rectangles around the rendered symbol</returns>
    public void RenderSymbol(StpSymbol stpSymbol, Image overlay = null)
    {
        // Get it rendered on the overlay
        var bounds = RenderSymbolOnOverlay(stpSymbol, overlay);
        
        // Update actual map image
        UpdateMapImage(bounds);
    }

    /// <summary>
    /// Render symbol on overlay
    /// </summary>
    /// <param name="stpSymbol"></param>
    /// <param name="overlay">Image to render the symbol onto - defaults to the standard symbol overlay</param>
    /// <returns>Bounding rectangles around the rendered symbol</returns>
    private List<Rectangle> RenderSymbolOnOverlay(StpSymbol stpSymbol, Image overlay = null)
    {
        if (stpSymbol == null)
        {
            return new List<Rectangle>();
        }

        List<Rectangle> bounds = new();

        Color color = Color.Black;
        switch (stpSymbol.Affiliation)
        {
            case Affiliation.friend:
            case Affiliation.assumedfriend:
                color = Color.Blue;
                break;
            case Affiliation.hostile:
                color = Color.Red;
                break;
            case Affiliation.neutral:
                color = Color.Green;
                break;
            case Affiliation.unknown:
                color = Color.Red;
                break;
        }

        if (overlay is null)
        {
            overlay = _symbolOverlay;
        }
        if (stpSymbol.GeometryType == StpSymbol.GeometryTypeEnum.POINT)
        {
            Point centroid = GeoToImage(stpSymbol.Location.Coords[0]);
            Image symbolImage = stpSymbol.Bitmap(SymbolRenderSize.Width, SymbolRenderSize.Height);
            if (symbolImage != null)
            {
                // Center of rendered symbol is slightly to the left (38% of the width)
                int x = centroid.X - (int)(SymbolRenderSize.Width * 0.38);
                // In the middle height-wise
                int y = centroid.Y - SymbolRenderSize.Height / 2;
                using var g = Graphics.FromImage(overlay);
                g.DrawImage(symbolImage, x, y,
                    SymbolRenderSize.Width, SymbolRenderSize.Height);
                bounds.Add(BoundingRect(centroid, SymbolRenderSize));
            }
            else
            {
                // Fall back to circle fitting a rectangle centered at the point
                bounds.Add(DrawPoint(centroid, CoordSpace.Image, overlay, color, StrokeWidth * 2));
            }
        }
        else if (stpSymbol.Location.Coords.Count > 1)
        {
            // Get anchor points dumbed down to a line
            var coords = stpSymbol.GetLinearSymbolCoords();
            for (int i = 0; i < coords.Count - 1; i++)
            {
                bounds.Add(DrawLineSegment(coords[i], coords[i + 1], CoordSpace.Geo, overlay, color));
            }
        }
        return bounds;
    }

    /// <summary>
    /// Draw ink segment given in control coordinates to overlay
    /// </summary>
    /// <param name="point1"></param>
    /// <param name="p2"></param>
    /// <param name="overlay"></param>
    /// <param name="color"></param>
    /// <param name="width"></param>
    /// <returns>Bounding rectangle in image space</returns>
    private Rectangle DrawControlSegmentInk(Point point1, Point p2, Image overlay, Color color, int width=0)
        => DrawLineSegment(point1, p2, CoordSpace.Control, overlay, color, width);

    /// <summary>
    /// Draw ink segment given in geo coordinates to overlay
    /// </summary>
    /// <param name="point1"></param>
    /// <param name="p2"></param>
    /// <param name="overlay"></param>
    /// <param name="color"></param>
    /// <param name="width"></param>
    /// <returns>Bounding rectangle in image space</returns>
    private Rectangle DrawGeoSegmentInk(Point point1, Point p2, Image overlay, Color color, int width=0)
        => DrawLineSegment(point1, p2, CoordSpace.Geo, overlay, color, width);

    /// <summary>
    /// Draw a line segment to overlay, converting from given space to image space
    /// </summary>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    /// <param name="fromSpace">Coordinate space of the given points</param>
    /// <param name="overlay"></param>
    /// <param name="color"></param>
    /// <param name="width"></param>
    /// <returns>Bounding rectangle in image space</returns>
    private Rectangle DrawLineSegment(object point1, object point2, CoordSpace fromSpace, Image overlay, Color color, int width=0)
    {
        if (width <= 0)
        {
            width = StrokeWidth;
        }
        var p1 = ToImageSpace(point1, fromSpace);
        var p2 = ToImageSpace(point2, fromSpace);
        using (var g = Graphics.FromImage(overlay))
        {
            using var penLine = new Pen(color, width);
            g.DrawLine(penLine, p1, p2);
        }
        return BoundingRect(poly: new List<Point>() { p1, p2 }, width);
    }

    /// <summary>
    /// Draw ink point (dot) 
    /// </summary>
    /// <param name="p"></param>
    /// <param name="fromSpace">Coordinate space of the given points</param>
    /// <param name="overlay"></param>
    /// <param name="color"></param>
    /// <returns>Bounding rectangle</returns>
    private Rectangle DrawPoint(Point p, CoordSpace fromSpace, Image overlay, Color color, int width=0)
    {
        if (width <= 0)
        {
            width = StrokeWidth;
        }
        // Convert from control/picture box to image space
        var point1 = ToImageSpace(p, fromSpace);
        using (var g = Graphics.FromImage(overlay))
        {
            // Single point (or first point of a stroke) - render a dot
            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, point1.X - width / 2, point1.Y - width / 2, width, width);
        }
        return BoundingRect(point1, width);
    }

    /// <summary>
    /// Create a frame onto which elements are drawn and get that loaded into the control
    /// </summary>
    private void LoadMapImage()
    {
        _frameBuffer = new Bitmap(_mapControl.Width, _mapControl.Height);
        UpdateMapImage(new List<Rectangle>() { new Rectangle(0, 0, _mapImage.Size.Width, _mapImage.Size.Height) });
    }

    /// <summary>
    /// Remove the ink strokes and symbols from the display - just the background map is shown
    /// </summary>
    public void ClearMap()
    {
        // Clean symbol overlay 
        _symbolOverlay = new Bitmap(_mapImage.Size.Width, _mapImage.Size.Height);

        // Clear ink takes care of recreating that layer and showing the background map
        ClearInk();

        // Redraw the whole map
        UpdateMapImage(new List<Rectangle>() { new Rectangle(0, 0, _mapImage.Size.Width, _mapImage.Size.Height) });
    }

    /// <summary>
    /// Remove the ink strokes from the display - a symbol fusing the ink has been added
    /// </summary>
    public void ClearInk()
    {
        // Get the bounds of the current ink strokes
        // Cold go tighter by generating the bounds for each line segment
        List<Rectangle> inkBounds = _strokesPixels
            .Select(stroke => BoundingRect(stroke, StrokeWidth))
            .ToList();

        // Clean ink overlay 
        _inkOverlay = new Bitmap(_mapImage.Size.Width, _mapImage.Size.Height);

        // Reset the stroke cache and overlay
        _strokesPixels = new List<List<Point>>();
        _staleInkOverlay = null;

        // Redraw other layers in the now removed ink regions
        UpdateMapImage(inkBounds);
    }

    /// <summary>
    /// CHange the ink color to indicate for example that it has been already processed by the sketch recognizer
    /// </summary>
    public void MarkInkAsProcessed()
    {
        // Create new overlay for stale ink
        _staleInkOverlay = new Bitmap(_mapImage.Size.Width, _mapImage.Size.Height);

        // Redraw in different color
        List<Rectangle> allBounds = new();
        foreach (var stroke in _strokesPixels)
        {
            for (int i = 0; i < stroke.Count - 1; i++)
            {
                Rectangle bounds = DrawLineSegment(stroke[i], stroke[i + 1], CoordSpace.Image, _staleInkOverlay, StrokeProcessedColor);
                allBounds.Add(bounds);
            }
        }

        // Clean regular ink overlay now that its contents were transferred to stale ink
        _inkOverlay = new Bitmap(_mapImage.Size.Width, _mapImage.Size.Height);

        // Update image
        UpdateMapImage(allBounds);
    }

    /// <summary>
    /// Redraw multiple patches of map image with potentially modified overlays and refresh control image
    /// </summary>
    /// <param name="boundsList">Areas to refresh - image space</param>
    private void UpdateMapImage(List<Rectangle> boundsList)
    {
        // Draw each patch within which there were changes
        foreach (var bounds in boundsList)
        {
            UpdateFrame(bounds);
        }

        // Make the new frame visible in a single move to avoid flickering
        RefreshFrame(_frameBuffer);
    }
    
    /// <summary>
    /// Redraw frame within one patch 
    /// </summary>
    /// <param name="bounds">Area to refresh - image space</param>
    private void UpdateFrame(Rectangle bounds)
    {
        // Calculate the scaled and translated position the image will take when displayed in the control
        // Upper left, Upper right, Lower left (but Point take x, y as parameters, so reversed)
        Point[] destPts = {
            ImageToControl(bounds.Location),
            ImageToControl(new Point(bounds.Right, bounds.Top)),
            ImageToControl(new Point(bounds.Left, bounds.Bottom)),
        };

        // Redraw the background and overlays on a new frame
        using var g = Graphics.FromImage(_frameBuffer);
        g.DrawImage(_mapImage, destPts, bounds, GraphicsUnit.Pixel);
        g.DrawImage(_symbolOverlay, destPts, bounds, GraphicsUnit.Pixel);
        if (_staleInkOverlay != null)
        {
            g.DrawImage(_staleInkOverlay, destPts, bounds, GraphicsUnit.Pixel);
        }
        else
        {
            g.DrawImage(_inkOverlay, destPts, bounds, GraphicsUnit.Pixel);
        }
    }

    /// <summary>
    /// Replace the map control image with a new one
    /// </summary>
    /// <param name="newFrame"></param>
    private void RefreshFrame(Bitmap newFrame)
    {
        if (_mapControl.InvokeRequired)
        {   // recurse on GUI thread if necessary
            _mapControl.Invoke(new MethodInvoker(() => RefreshFrame(newFrame)));
            return;
        }
        _mapControl.Image = newFrame;
    }
    #endregion

    #region Map operations
    /// <summary>
    /// Zoom centered at a point
    /// </summary>
    /// <param name="center"></param>
    /// <param name="factor"></param>
    public void Zoom(LatLon center, double factor)
    {
        // TODO:
    }

    /// <summary>
    /// Zoom so that area takes the most of the available extent
    /// </summary>
    /// <param name="topLeft"></param>
    /// <param name="botRight"></param>
    public void Zoom(LatLon topLeft, LatLon botRight)
    {
        // TODO:
    }

    /// <summary>
    /// Pan map in the direction and length indicated by the init-end vector
    /// </summary>
    /// <param name="init"></param>
    /// <param name="end"></param>
    public void Pan(LatLon init, LatLon end)
    {
        // TODO:
    }

    /// <summary>
    /// Render a symbol in a way that highlights it
    /// </summary>
    /// <param name="stpSymbol"></param>
    public void Highlight(StpSymbol stpSymbol)
    {
        // TODO:
    }
    #endregion

    #region Symbol + stroke intersection
    /// <summary>
    /// List of unique identifiers of symbols that are intersected by a stroke
    /// </summary>
    /// <param name="symbols">List of current symbols</param>
    /// <returns></returns>
    public List<string> IntesectedSymbols(List<StpSymbol> symbols)
    {
        List<string> intersectedPoids = new();
        if (symbols is null)
        {
            return null;
        }

        // Basic implementation, that renders each symbol on its own and performs a bitwise comparison
        // between the rendered bits and the stroke rendering - stroke and symbol intersect if they 
        // share a non-empty byte at the same position
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

    /// <summary>
    /// Intersection of two bitmaps of the same size
    /// </summary>
    /// <remarks>
    /// Brute force approach that compares images byte by byte. 
    /// Images intersect if there are two non-blank/white bytes at the same position
    /// </remarks>
    /// <param name="image1Bytes"></param>
    /// <param name="image2Bytes"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private bool Intersect(byte[] image1Bytes, byte[] image2Bytes)
    {
        // Sanity check
        if (image1Bytes.Length != image2Bytes.Length)
        {
            throw new ArgumentException("Image bytes to check for intersection must have the same size");
        }

        // Intersect if bytes are not empty at the same position
        for (int i = 0; i < image1Bytes.Length; i++)
        {
            if (image1Bytes[i] != 0 && image2Bytes[i] != 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Get byte content of an image 
    /// </summary>
    /// <param name="image"></param>
    /// <returns></returns>
    private byte[] GetImageBytes(Bitmap image)
    {
        var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
            ImageLockMode.ReadOnly, image.PixelFormat);
        var bytes = new byte[data.Height * data.Stride];
        Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
        image.UnlockBits(data);
        return bytes;
    }
    #endregion

    #region Coordinate transformations
    /// <summary>
    /// Coordinate spaces
    /// </summary>
    enum CoordSpace { Control, Image, Geo };

    /// <summary>
    /// COnvert from a given coord space to image space
    /// </summary>
    /// <param name="p"></param>
    /// <param name="space"></param>
    /// <returns></returns>
    private Point ToImageSpace(object p, CoordSpace space)
    {
        switch (space)
        {
            case CoordSpace.Control:
                return ControlToImage((Point)p);
            case CoordSpace.Geo:
                return GeoToImage((LatLon)p);
            case CoordSpace.Image:
                return (Point)p;
        }
        throw new ArgumentException("Unexpected coord space");
    }

    /// <summary>
    /// Convert picture box coordinate to geo 
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    private LatLon ControlToGeo(Point p) => ImageToGeo(ControlToImage(p));

    /// <summary>
    /// Convert screen to map coordinates
    /// </summary>
    /// <remarks>
    /// Uses an approximation that will only be acceptable on small map regions 
    /// not affected by earth's curvature</remarks>
    /// <param name="location"></param>
    /// <returns></returns>
    private LatLon ImageToGeo(Point location)
    {
        double mapHeightDegrees = _mapBottomRight.Lat - _mapTopLeft.Lat;
        double mapWidthDegrees = _mapBottomRight.Lon - _mapTopLeft.Lon;
        double degreesPerPixelH = mapHeightDegrees / _mapImage.Height;
        double degreesPerPixelW = mapWidthDegrees / _mapImage.Width;


        double locationLat = _mapTopLeft.Lat + (double)location.Y * degreesPerPixelH;
        double locationLon = _mapTopLeft.Lon + (double)location.X * degreesPerPixelW;
        return new LatLon(locationLat, locationLon);
    }

    /// <summary>
    /// Convert screen to map coordinates
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    private Point GeoToImage(LatLon location)
    {
        double mapHeightDegrees = _mapBottomRight.Lat - _mapTopLeft.Lat;
        double mapWidthDegrees = _mapBottomRight.Lon - _mapTopLeft.Lon;
        double pixelsPerDegreesH = _mapImage.Height / mapHeightDegrees;
        double pixelsPerDegreesW = _mapImage.Width / mapWidthDegrees;

        int locationY = (int)((location.Lat - _mapTopLeft.Lat) * pixelsPerDegreesH);
        int locationX = (int)((location.Lon - _mapTopLeft.Lon) * pixelsPerDegreesW);
        return new Point(locationX, locationY);
    }

    /// <summary>
    /// From picture box control coordinates to image coordinate
    /// </summary>
    /// <param name="controlPoint"></param>
    /// <returns></returns>
    private Point ControlToImage(Point controlPoint)
    {
        //double scale = Math.Min((double)pictureMap.ClientSize.Width / _mapImage.Width, (double)pictureMap.ClientSize.Height / _mapImage.Height);
        //int xOffset = (int)(pictureMap.Width - _mapImage.Width * scale) / 2;
        //int yOffset = (int)(pictureMap.Height - _mapImage.Height * scale) / 2;
        var transf = GetControlTransform();

        /// Translate and scale the control point to the image space
        int imageX = (int)((controlPoint.X - transf.OffsetX) * transf.Scale);
        int imageY = (int)((controlPoint.Y - transf.OffsetY) * transf.Scale);
        Point p = new(imageX, imageY);
        var p2 = ImageToControl(p);
        return p;
    }

    /// <summary>
    /// From image coordinates to picture box coordinates
    /// </summary>
    /// <param name="imagePoint"></param>
    /// <returns></returns>
    private Point ImageToControl(Point imagePoint)
    {
        //double scale = Math.Min((double)pictureMap.ClientSize.Width / _mapImage.Width, (double)pictureMap.ClientSize.Height / _mapImage.Height);
        //int xOffset = (int)(pictureMap.Width - _mapImage.Width * scale) / 2;
        //int yOffset = (int)(pictureMap.Height - _mapImage.Height * scale) / 2;
        var transf = GetControlTransform();

        // Scale and translate the image point to the control space
        int controlX = (int)(imagePoint.X / transf.Scale + transf.OffsetX);
        int controlY = (int)(imagePoint.Y / transf.Scale + transf.OffsetY);
        Point p = new(controlX, controlY);
        return p;
    }

    /// <summary>
    /// Calculate the image to control space factors 
    /// </summary>
    /// <remarks>
    /// - Scaling factor from image to control space - if image has width=1000 and the box has width=500, the scale is 2.0
    /// - OffsetX - control space pixels used for horizontal padding if the image is narrower than the control
    /// - OffsetY - control space pixels used for vertical padding if the image is shorter than the control
    ///  </remarks>
    /// <returns></returns>
    private (double Scale, int OffsetX, int OffsetY) GetControlTransform()
    {
        // Scaling factor from image to the picture box
        double scale =
            Math.Max(
                (double)_mapImage.Width / _mapControl.ClientSize.Width,
                (double)_mapImage.Height / _mapControl.ClientSize.Height
            );
        // Offset in screen units that may be left as padding by the picture box if the control is
        // does not have the same aspect ratio as the image 
        int xOffset = (int)(_mapControl.Width - _mapImage.Width / scale) / 2;
        int yOffset = (int)(_mapControl.Height - _mapImage.Height / scale) / 2;

        return (scale, xOffset, yOffset);
    }
    #endregion

    #region Utility
    /// <summary>
    /// Bounding rectangle given a center point and radius
    /// </summary>
    /// <param name="point"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    private Rectangle BoundingRect(Point point, int radius)
    {
        return BoundingRect(point, new Size(radius, radius));
    }

    /// <summary>
    /// Bounding rectangle given a center point and size
    /// </summary>
    /// <param name="point"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    private Rectangle BoundingRect(Point point, Size size)
    {
        // Adjust center to top, left
        return new Rectangle(new Point(point.X - size.Width / 2, point.Y - size.Height / 2), size);
    }

    /// <summary>
    /// Bounding rectangle of a polyline
    /// </summary>
    /// <param name="poly"></param>
    /// <param name="strokeWidth"></param>
    /// <returns></returns>
    private Rectangle BoundingRect(List<Point> poly, int strokeWidth)
    {
        var minX = poly.Select(p => p.X).Min() - strokeWidth;
        var maxX = poly.Select(p => p.X).Max() + strokeWidth;
        var minY = poly.Select(p => p.Y).Min() - strokeWidth;
        var maxY = poly.Select(p => p.Y).Max() + strokeWidth;
        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }
    #endregion

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
}
