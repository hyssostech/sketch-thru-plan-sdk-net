using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StpSDK;


namespace StpSDKSample;
public partial class Form1 : Form
{
    #region Private Properties and constants
    private static ILogger _logger;
    private static AppParams _appParams;

    private static StpRecognizer _stpRecognizer;

    private Image _mapImage;
    private List<LatLon> _stroke = null;     // The current stroke we're building to eventually pass to SendInk
    private DateTime _timeStart, _timeEnd;    // Start and End times of current stroke
    private int _lastX = -1;
    private int _lastY = -1;

    private const int InkThickness = 4;
    private const int SymbolSize = 100;

    #endregion

    #region Construction/Teardown
    /// <summary>
    /// Construct form IOptions provided via dependency injection
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="agent"></param>
    /// <param name="bridgeParamsOptions"></param>
    public Form1(ILoggerFactory loggerFactory, IOptions<AppParams>appSettings)
        : this(loggerFactory, appSettings.Value)
    {

    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="appParams"></param>
    public Form1(ILoggerFactory loggerFactory, AppParams appParams)
    {
        _logger = loggerFactory.CreateLogger(this.GetType());
        _appParams = appParams;

        // Load map provided as parameter
        _mapImage = Image.FromFile(_appParams.MapImagePath);

        InitializeComponent();
        
    }
    /// <summary>
    /// Initialize components and connect to STP on form load
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Form1_Load(object sender, EventArgs e)
    {
        // Attempt to connect to STP
        if (!Connect())
        {
            MessageBox.Show("Failed to connect to STP. Please make sure it is running and try again", "Could not connect to  agents");
            Application.Exit();
        }
    }

    /// <summary>
    /// Disconnect from STP when form is closed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        _stpRecognizer?.Disconnect();
    }
    #endregion

    #region STP SDK methods and event handlers
    /// <summary>
    /// Initialize event handlers and connect to STP
    /// </summary>
    /// <remarks>
    /// THe STP Engine must be running before the app is started
    /// </remarks>
    /// <returns>True if connection was successful</returns>
    internal bool Connect()
    {
        // Create an STP connection object - using STP's native pub/sub system
        var stpConnector = new StpOaaConnector(_logger, _appParams.StpHost, _appParams.StpPort);

        // Initialize the STP recognizer with the connector definition
        _stpRecognizer = new StpRecognizer(stpConnector);

        // Hook up to the events _before_ connecting, so that the correct message subscriptions can be identified
        // A new symbol has been added, updated or removed
        _stpRecognizer.OnSymbolAdded += StpRecognizer_OnSymbolAdded;
        _stpRecognizer.OnSymbolModified += StpRecognizer_OnSymbolModified;
        _stpRecognizer.OnSymbolDeleted += StpRecognizer_OnSymbolDeleted;

        // Speech recognition feedback
        _stpRecognizer.OnSpeechRecognized += StpRecognizer_OnSpeechRecognized;
        // Indication that the ink can be removed to declutter the display
        _stpRecognizer.OnInkProcessed += StpRecognizer_OnInkProcessed;

        // Message from STP to be conveyed to user
        _stpRecognizer.OnStpMessage += StpRecognizer_OnStpMessage;

        // Connection error notification
        _stpRecognizer.OnConnectionError += StpRecognizer_OnConnectionError;
        
        // STP is being shutdown 
        _stpRecognizer.OnShutdown += StpRecognizer_OnShutdown;

        // Attempt to connect
        bool success;
        try
        {
            success = _stpRecognizer.ConnectAndRegister("StpSDKSample");
        }
        catch
        {
            success = false;
        }

        // Nothing else to do if connection failed
        if (!success)
        {
            return false;
        }

        // Hook up to the map events to collect the sketched gestures
        pictureMap.Image = _mapImage;
        pictureMap.MouseDown += PictureMap_MouseDown;
        pictureMap.MouseMove += PictureMap_MouseMove;
        pictureMap.MouseUp += PictureMap_MouseUp;

        return true;
    }
    #endregion

    #region Symbol event handlers
    /// <summary>
    /// STP has recognized a new symbol
    /// </summary>
    /// <param name="poid">Unique identifier</param>
    /// <param name="stpItem">Recognized item</param>
    /// <param name="isUndo">True if this event represents a compensating action to undo a symbol delete</param>
    private void StpRecognizer_OnSymbolAdded(string poid, StpItem stpItem, bool isUndo)
    {
        if (stpItem is null)
            return;

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

    /// <summary>
    /// A symbol has been updated - via STP speech and sketch edits or manually by th user
    /// </summary>
    /// <param name="poid"></param>
    /// <param name="stpSymbol"></param>
    /// <param name="isUndo"></param>
    private void StpRecognizer_OnSymbolModified(string poid, StpSymbol stpSymbol, bool isUndo)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"SYMBOL MODIFIED:\t{stpSymbol.Poid}\t{stpSymbol.FullDescription}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
    }

    /// <summary>
    /// A symbol has been removed
    /// </summary>
    /// <param name="poid"></param>
    /// <param name="isUndo"></param>
    private void StpRecognizer_OnSymbolDeleted(string poid, bool isUndo)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"SYMBOL DELETED:\t{poid}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
    }

    /// <summary>
    /// Speech recognition results
    /// </summary>
    /// <param name="speechList"></param>
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

    /// <summary>
    /// Symbol fusing the sketched gesture was produced, so can clear ink marks to declutter the display
    /// </summary>
    /// <remarks>
    /// An alternative is to keep the ink in an overlay and allow users to show/hide that
    /// </remarks>
    private void StpRecognizer_OnInkProcessed()
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => StpRecognizer_OnInkProcessed()));
        }
        else
        {
            pictureMap.Image = _mapImage; // remove drawn ink from the map
            pictureMap.Refresh();
        }
    }

    /// <summary>
    /// Connection error notification
    /// </summary>
    /// <param name="sce"></param>
    private void StpRecognizer_OnConnectionError(StpCommunicationException sce)
    {
        MessageBox.Show("Connection to STP was lost. Verify that the service is running and restart this app", "Connection Lost", MessageBoxButtons.OK);
        Application.Exit();
    }

    /// <summary>
    /// Show message receive from STP
    /// </summary>
    /// <param name="level"></param>
    /// <param name="msg"></param>
    private void StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel level, string msg)
    {
        ShowMessage(msg);
    }

    /// <summary>
    /// STP is shutting down - terminate this app
    /// </summary>
    /// <remarks>
    /// Applications that can only be run when STP is available should shutdown,
    /// or at least advise users that the service is not available and provide means to reconnect
    /// </remarks>
    private void StpRecognizer_OnShutdown()
    {
        Application.Exit();
    }
    #endregion

    #region Display methods
    /// <summary>
    /// Show a message in the log window
    /// </summary>
    /// <param name="msg"></param>
    private void ShowMessage(string msg)
    {
        if (this.InvokeRequired)
        {
            this.Invoke((MethodInvoker)(() => ShowMessage(msg)));  // recurse into UI thread if we need to
        }
        else
        {
            if (msg == null)
            {
                this.textBoxLog.Clear();
            }
            else
            {
                this.textBoxLog.Text += msg + "\r\n";
                this.textBoxLog.SelectionStart = this.textBoxLog.Text.Length;
                this.textBoxLog.ScrollToCaret();
            }
        }
    }

    /// <summary>
    /// Display symbol properties
    /// </summary>
    /// <param name="stpSymbol"></param>
    private void DisplaySymbol(StpSymbol stpSymbol)
    {
        // Format attributes of interest to display in the PropertyGrid
        if (stpSymbol.Type == "unit")
        {
            var objectToDisplay = new
            {
                Id = stpSymbol.Poid,
                Description = stpSymbol.Description,
                SIDC = stpSymbol.SymbolId,
                Designator = stpSymbol.Designator1 + (!string.IsNullOrWhiteSpace(stpSymbol.Designator2) ? "/" + stpSymbol.Designator2 : string.Empty),
                Affiliation = stpSymbol.Affiliation,
                Echelon = stpSymbol.Echelon,
                Strength = stpSymbol.Strength,
                HQType = stpSymbol.Modifier,
                Status = stpSymbol.Status,
            };
            propertyGridResult.SelectedObject = objectToDisplay;
        }
        else if (stpSymbol.Type == "mootw")
        {
            var objectToDisplay = new
            {
                Description = stpSymbol.Description,
                SIDC = stpSymbol.SymbolId,
                Designator = stpSymbol.Designator1 + (!string.IsNullOrWhiteSpace(stpSymbol.Designator2) ? "/" + stpSymbol.Designator2 : string.Empty),
                Affiliation = stpSymbol.Affiliation,
            };
            propertyGridResult.SelectedObject = objectToDisplay;
        }
        else if (stpSymbol.Type == "tg")
        {
            var objectToDisplay = new
            {
                Description = stpSymbol.Description,
                SIDC = stpSymbol.SymbolId,
                Designator = stpSymbol.Designator1 + (!string.IsNullOrWhiteSpace(stpSymbol.Designator2) ? "/" + stpSymbol.Designator2 : string.Empty),
                Affiliation = stpSymbol.Affiliation,
                Echelon = stpSymbol.Echelon,
            };
            propertyGridResult.SelectedObject = objectToDisplay;
        }
        propertyGridResult.Refresh();

        // Render 
        RenderSymbol(stpSymbol);

        // List alternates in log window
        ListAlternates(stpSymbol);
    }

    /// <summary>
    ///  List alternates on the log window
    /// </summary>
    /// <param name="stpItem"></param>
    private void ListAlternates(StpItem stpItem)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, stpItem.Type.ToUpper());
        // Show each item in the n-best list in the log display
        foreach (var reco in stpItem.Alternates)
        {
            if (reco is null)
                continue;
            string msg = $"{reco.Order:00} ({reco.Confidence:0.0000}) :\t{reco.FullDescription}";
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
        }
    }

    /// <summary>
    /// Show what the speech recognizer transcribed, to provide user feedback
    /// </summary>
    /// <param name="speechReco"></param>
    private void ShowSpeechReco(string speechReco)
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => ShowSpeechReco(speechReco)));
        }
        else
        {
            txtSimSpeech.Text = speechReco;
        }
    }
    #endregion

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

        // Add single/initial coordinate
        var geo = GeoCoordinatesAt(e.Location);
        _stroke.Add(geo);

        // Show initial dot in case the user just clicks (adding a point)
        using (var g = pictureMap.CreateGraphics())
        {
            using (var brush = new SolidBrush(Color.Red))
                g.FillEllipse(brush, e.Location.X - InkThickness / 2, e.Location.Y - InkThickness / 2, InkThickness, InkThickness);
        }

        // Notify STP of the start of a stroke and activate speech recognition
        _stpRecognizer.SendPenDown(geo, DateTime.Now);
        _timeStart = DateTime.Now;

        // Mark the starting point of the first segment, if the user starts to drag 
        _lastX = e.Location.X;
        _lastY = e.Location.Y;
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
        // Skip mouse movements that were not started by a mouse down - user may be hovering over the map
        if (_stroke == null) 
            return;

        // Collect the coordinates as the user sketches to be able to send over to STP when done
        var geo = GeoCoordinatesAt(e.Location);
        _stroke.Add(geo);

        using (var g = pictureMap.CreateGraphics())
        {
            using (var penLine = new Pen(Color.Red, InkThickness))
                g.DrawLine(penLine, new Point(_lastX, _lastY), new Point(e.Location.X, e.Location.Y));
        }

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

    /// <summary>
    /// Mark symbol position - rendering placeholder
    /// </summary>
    /// <param name="stpSymbol"></param>
    private void RenderSymbol(StpSymbol stpSymbol)
    {
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

        if (stpSymbol.Location.Coords.Count == 1 )
        {
            using (var g = pictureMap.CreateGraphics())
            {
                Point p = ScreenCoordinatesAt(stpSymbol.Location.Coords[0]);
                // Draw circle fitting a rectangle centered at the point
                using (var brush = new SolidBrush(color))
                    g.FillEllipse(brush, p.X - 5, p.Y - 5, 10, 10);
            }
        }
        else if (stpSymbol.Location.Coords.Count > 1)
        {
            using (var g = pictureMap.CreateGraphics())
            {
                var coords = stpSymbol.GetLinearSymbolCoords();
                if (coords != null)
                {
                    Point last = ScreenCoordinatesAt(coords[0]);
                    foreach (var coord in coords)
                    {
                        Point p = ScreenCoordinatesAt(coord);
                        using (var pen = new Pen(color, InkThickness))
                            g.DrawLine(pen, last, p);
                        last = p;
                    }
                }
            }

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
        double degreesPerPixelH = mapHeightDegrees / pictureMap.Height;
        double degreesPerPixelW = mapWidthDegrees / pictureMap.Width;


        double locationLat = _appParams.MapTopLat + (double)location.Y * degreesPerPixelH;
        double locationLon = _appParams.MapLeftLon + (double)location.X * degreesPerPixelW;

        return new LatLon(locationLat, locationLon);
    }

    /// <summary>
    /// Convert screen to map coordinates
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    private Point ScreenCoordinatesAt(LatLon location)
    {
        double mapHeightDegrees = _appParams.MapBottomLat - _appParams.MapTopLat;
        double mapWidthDegrees = _appParams.MapRightLon - _appParams.MapLeftLon;
        double pixelsPerDegreesH = pictureMap.Height / mapHeightDegrees;
        double pixelsPerDegreesW = pictureMap.Width / mapWidthDegrees;
        
        int locationY = (int)((location.Lat - _appParams.MapTopLat) * pixelsPerDegreesH);
        int locationX = (int)((location.Lon - _appParams.MapLeftLon) * pixelsPerDegreesW);


        return new Point(locationX, locationY);
    }

    #endregion

    #region Form events
    private void PlaBtn_Click(object sender, EventArgs e)
    {
        ClearButtons();
        plaBtn.Checked = true;
        tsLabelTiming.Text = "Mode: Freehand Points,Lines,Areas";
        ChangeTimeOut(TimingConstants.Timing_PLA);
    }

    private void DrawBtn_Click(object sender, EventArgs e)
    {
        ClearButtons();
        drawBtn.Checked = true;
        tsLabelTiming.Text = "Mode: Draw 2525 Symbols";
        ChangeTimeOut(TimingConstants.Timing_Sketch);
    }

    private void ClearButtons()
    {
        plaBtn.Checked = drawBtn.Checked = false;
    }

    private void ChangeTimeOut(double _timeout)
    {
        _stpRecognizer.ResetWaitTimeout();
        _stpRecognizer.ResetSegmentationTimeout();

        _stpRecognizer.SetWaitTimeout(_timeout);
        _stpRecognizer.SetSegmentationTimeout(_timeout);
    }

    private void BtnClearLog_Click_1(object sender, EventArgs e)
    {
        textBoxLog.Clear();
    }
    #endregion
}

