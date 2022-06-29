
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StpSDK;


namespace StpSDKSample;
public class Mapping
{
    public class PenStroke
    {
        public Size PixelBounds { get; set; }
        public LatLon TopLeftGeo { get; set; }
        public LatLon BotRightGeo { get; set; }
        public List<LatLon> Stroke { get; set; }
        public DateTime TimeStart { get; set; }
        public DateTime TimeEnd { get; set; }
        public List<string> IntersectedPoids { get; set; }
    }

    public event EventHandler<LatLon> OnPenDown;
    public event EventHandler<PenStroke> OnStrokeCompleted;

    private PictureBox _mapControl;
    private AppParams _appParams;

    private  Image _mapImage;
    private Image _symbolOverlay;
    private Image _inkOverlay;

    private List<LatLon> _stroke = null;     // The current stroke we're building to eventually pass to SendInk
    private DateTime _timeStart, _timeEnd;    // Start and End times of current stroke
    private int _lastX = -1;
    private int _lastY = -1;


    public Mapping(PictureBox mapControl, AppParams appParams)
    {
        _mapControl = mapControl;
        _appParams = appParams;

        // Load map provided as parameter and create image overlays of the same size
        _mapImage = Image.FromFile(_appParams.MapImagePath);
        _symbolOverlay = new Bitmap(_mapImage.Size.Width, _mapImage.Size.Height);
        _inkOverlay = new Bitmap(_mapImage.Size.Width, _mapImage.Size.Height);

        _mapControl.Image = _mapImage;
        _mapControl.MouseDown += PictureMap_MouseDown;
        _mapControl.MouseMove += PictureMap_MouseMove;
        _mapControl.MouseUp += PictureMap_MouseUp;

    }

    #region Mapping methods
    /// <summary>
    /// The user has pressed the mouse/stylus to the map
    /// </summary>
    /// <remarks>
    ///  Capture the current position/time and call the SDK API SendPenDown() to 
    ///  inform STP system that we are in the process of drawing a stroke and to not segment until complete
    ///</remarks>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PictureMap_MouseDown(object sender, MouseEventArgs e)
    {
        _stroke = new List<LatLon>();

        var geo = GeoCoordinatesAt(e.Location);
        _stroke.Add(geo);

        // Set the first anchor point
        _lastX = e.Location.X;
        _lastY = e.Location.Y;

        // Draw a point (segment starting and ending at the same point)
        DrawInkSegment(new Point(_lastX, _lastY), new Point(e.Location.X, e.Location.Y));

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
    private void PictureMap_MouseMove(object sender, MouseEventArgs e)
    {
        // Skip moves that were not preceded by a pen down that initialized a stroke capture
        if (_stroke is null)
        {
            return;
        }    

        // Collect the coordinates as the user sketches to be able to send over to STP when done
        var geo = GeoCoordinatesAt(e.Location);
        _stroke.Add(geo);

        // Draw next segment
        DrawInkSegment(new Point(_lastX, _lastY), new Point(e.Location.X, e.Location.Y));

        // Keep track of the end of the stroke to build the next segment
        _lastX = e.Location.X;
        _lastY = e.Location.Y;
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
    private void PictureMap_MouseUp(object sender, MouseEventArgs e)
    {
        if (_stroke == null) return;

        PenStroke penStroke = new()
        {
            TimeEnd = DateTime.Now,
            PixelBounds = new Size(_mapControl.Width, _mapControl.Height),
            TopLeftGeo = GeoCoordinatesAt(new Point(0, 0)),
            BotRightGeo = GeoCoordinatesAt(new Point(_mapControl.Width, _mapControl.Height)),
            Stroke = _stroke,
            // To support multimodal symbol editing, it is necessary for the app to identify the existing elements
            // that a stroke intersects, for example, a point or line over a unit that one wants to move, delete,
            // of change attributes.
            // Here we just return null for simplicity sake. That will prevent editing of symbols placed on this app
            IntersectedPoids = null
        };
        OnStrokeCompleted?.Invoke(this, penStroke);
        _stroke = null;
    }

    /// <summary>
    /// Draw ink segment to overlay
    /// </summary>
    /// <param name="point1"></param>
    /// <param name="point2"></param>
    private void DrawInkSegment(Point point1, Point point2)
    {
        const int Thikness = 4;
        using (var g = Graphics.FromImage(_inkOverlay))
        {
            if (point1 == point2)
            {
                // Single point (or first point of a stroke) - render a dot
                using (var brush = new SolidBrush(Color.Red))
                    g.FillEllipse(brush, point1.X - Thikness/2, point1.Y - Thikness/2, Thikness, Thikness);
            }
            else
            {
                using (var penLine = new Pen(Color.Red, Thikness))
                    g.DrawLine(penLine, point1, point2);
            }
        }
        UpdateMapImage();
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
    }

    /// <summary>
    /// Remove the ink strokes from the display - a symbol fusing the ink has been added
    /// </summary>
    public void ClearInk()
    {
        // Clean ink overlay 
        _inkOverlay = new Bitmap(_mapImage.Size.Width, _mapImage.Size.Height);

        // Reload base map
        _mapControl.Image = _mapImage; 

        // Redraw other layers
        UpdateMapImage();
    }

    /// <summary>
    /// Render symbol on the map
    /// </summary>
    /// <param name="stpSymbol"></param>
    public void RenderSymbol(StpSymbol stpSymbol)
    {
        const int RenderSize = 100;
        Image symbolImage = stpSymbol.Bitmap(RenderSize, RenderSize);

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

        using (var g = Graphics.FromImage(_symbolOverlay))
        {
            if (stpSymbol.Location.Coords.Count == 1)
            {
                Point p = ScreenCoordinatesAt(stpSymbol.Location.Coords[0]);
                if (symbolImage != null)
                {
                    g.DrawImage(symbolImage, p.X - RenderSize / 2, p.Y - RenderSize / 2, RenderSize, RenderSize);
                }
                else
                {
                    // Fall back to circle fitting a rectangle centered at the point
                    using (var brush = new SolidBrush(color))
                        g.FillEllipse(brush, p.X - 5, p.Y - 5, 10, 10);
                }
            }
            else if (stpSymbol.Location.Coords.Count > 1)
            {
                var coords = stpSymbol.GetLinearSymbolCoords();
                if (coords != null)
                {
                    Point last = ScreenCoordinatesAt(coords[0]);
                    foreach (var coord in coords)
                    {
                        Point p = ScreenCoordinatesAt(coord);
                        using (var pen = new Pen(color, 4))
                            g.DrawLine(pen, last, p);
                        last = p;
                    }
                }
            }
        }
        // Update image
        UpdateMapImage();
    }

    /// <summary>
    /// Refresh the map image with potentially modified overlays
    /// </summary>
    private void UpdateMapImage()
    {
        if (_mapControl.InvokeRequired)
        {   // recurse on GUI thread if necessary
            _mapControl.Invoke(new MethodInvoker(() => UpdateMapImage()));
            return;
        }
        // Redraw the background and overlays
        using (var g = _mapControl.CreateGraphics())
        {
            g.DrawImage(_symbolOverlay, new Point(0,0));
            g.DrawImage(_inkOverlay, new Point(0, 0));
        }
    }

    /// <summary>
    /// Convert screen to map coordinates
    /// </summary>
    /// <remarks>
    /// Uses an approximation that will only be acceptable on small map regions 
    /// not affected by earth's curvature</remarks>
    /// <param name="location"></param>
    /// <returns></returns>
    private LatLon GeoCoordinatesAt(Point location)
    {
        double mapHeightDegrees = _appParams.MapBottomLat - _appParams.MapTopLat;
        double mapWidthDegrees = _appParams.MapRightLon - _appParams.MapLeftLon;
        double degreesPerPixelH = mapHeightDegrees / _mapControl.Height;
        double degreesPerPixelW = mapWidthDegrees / _mapControl.Width;


        double locationLat = _appParams.MapTopLat + (double)location.Y * degreesPerPixelH;
        double locationLon = _appParams.MapLeftLon + (double)location.X * degreesPerPixelW;

        return new LatLon(locationLat, locationLon);
    }

    /// <summary>
    /// Convert map to screen coordinates
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    private Point ScreenCoordinatesAt(LatLon location)
    {
        double mapHeightDegrees = _appParams.MapBottomLat - _appParams.MapTopLat;
        double mapWidthDegrees = _appParams.MapRightLon - _appParams.MapLeftLon;
        double pixelsPerDegreesH = _mapControl.Height / mapHeightDegrees;
        double pixelsPerDegreesW = _mapControl.Width / mapWidthDegrees;

        int locationY = (int)((location.Lat - _appParams.MapTopLat) * pixelsPerDegreesH);
        int locationX = (int)((location.Lon - _appParams.MapLeftLon) * pixelsPerDegreesW);


        return new Point(locationX, locationY);
    }
    #endregion

}