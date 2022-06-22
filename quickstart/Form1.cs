using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StpSDK;


namespace StpSDKSample;
public partial class Form1 : Form
{
    #region Private Properties
    private static ILogger _logger;
    private static AppParams _appParams;

    private static StpRecognizer _stpRecognizer;

    private Image _mapImage;
    private List<LatLon> _stroke = null;     // The current stroke we're building to eventually pass to SendInk
    private DateTime _timeStart, _timeEnd;    // Start and End times of current stroke
    private int _lastX = -1;
    private int _lastY = -1;

    #endregion

    #region Construction/Teardown
    /// <summary>
    /// Construct form IOptions
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="agent"></param>
    /// <param name="bridgeParamsOptions"></param>
    public Form1(ILoggerFactory loggerFactory, IOptions<AppParams>appSettings)
        : this(loggerFactory, appSettings.Value)
    {

    }

    public Form1(ILoggerFactory loggerFactory, AppParams appParams)
    {
        _logger = loggerFactory.CreateLogger(this.GetType());
        _appParams = appParams;

        // Load map provided as parameter
        _mapImage = Image.FromFile(_appParams.MapImagePath);

        InitializeComponent();
        
        // Hook up to the map
        pictureMap.Image = _mapImage;
        pictureMap.MouseDown += PictureMap_MouseDown;
        pictureMap.MouseMove += PictureMap_MouseMove;
        pictureMap.MouseUp += PictureMap_MouseUp;
    }
    private void Form1_Load(object sender, EventArgs e)
    {
        // Attempt to connect to STP
        if (!Connect())
        {
            MessageBox.Show("Failed to connect to STP. Please make sure it is running and try again", "Could not connect to  agents");
            Application.Exit();
        }
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        _stpRecognizer?.Disconnect();
    }
    #endregion

    #region STP SDK methods and event handlers
    // Connect to the S2C2 system. At present the agents should be running before this call is made.
    internal bool Connect()
    {
        // Create an STP connection object - using STP's native pub/sub system
        var stpConnector = new StpOaaConnector(_logger, _appParams.StpHost, _appParams.StpPort);

        // Initialize the STP recognizer with the connector definition
        _stpRecognizer = new StpRecognizer(stpConnector);

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

        try
        {
            bool success = _stpRecognizer.ConnectAndRegister("StpSDKSample");
            if (success)
            {
                // Start with faster POint, Line, Area mode
                ChangeTimeOut(TimingConstants.Timing_PLA);
            }
            return success;
        }
        catch
        {
            return false;
        }
    }


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

    private void StpRecognizer_OnSymbolModified(string poid, StpSymbol stpSymbol, bool isUndo, bool selectSymbol)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"SYMBOL MODIFIED:\t{stpSymbol.Poid}\t{stpSymbol.FullDescription}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
    }

    private void StpRecognizer_OnSymbolDeleted(string poid, bool isUndo)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"SYMBOL DELETED:\t{poid}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
    }

    private void StpRecognizer_OnTaskAdded(string poid, StpTask stpTask, List<string> taskPoids, bool isUndo)
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => StpRecognizer_OnTaskAdded(poid, stpTask, taskPoids, isUndo)));
            return;
        }

        // Get the recognized item as a military symbol - not interested in other types of objects 
        DisplayTask(stpTask);
    }
    private void DisplayTask(StpTask stpTask)
    {
        var objectToDisplay = new
        {
            Id = stpTask.Poid,
            Description = stpTask.Description,
            Who = stpTask.Who,
            How = stpTask.How.ToString(),
            What = stpTask.What,
            Why = stpTask.Why,
            StartTime = stpTask.StartTime,
            EndTime = stpTask.EndTime,
        };
        propertyGridResult.SelectedObject = objectToDisplay;
        propertyGridResult.Refresh();

        ListAlternates(stpTask);
    }

    private void StpRecognizer_OnTaskModified(string poid, StpTask stpTask, List<string> tgPoids, bool isUndo)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"TASK MODIFIED:\t{stpTask.Poid}\t{stpTask.FullDescription}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
    }

    private void StpRecognizer_OnTaskDeleted(string poid, bool isUndo)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"Task DELETED:\t{poid}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
    }

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

    private void StpRecognizer_OnConnectionError(StpCommunicationException sce)
    {
        MessageBox.Show("Connection to STP was lost. Verify that the service is running and restart this app", "Connection Lost", MessageBoxButtons.OK);
        Application.Exit();
    }

    private void StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel level, string msg)
    {
        if (this.InvokeRequired)
        {
            this.Invoke((MethodInvoker)(() => StpRecognizer_OnStpMessage(level, msg)));  // recurse into UI thread if we need to
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

        // Notify STP of the start of a stroke and activate speech recognition
        _stpRecognizer.SendPenDown(geo, DateTime.Now);
        _timeStart = DateTime.Now;
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
            using (var brush = new SolidBrush(color))
            using (var g = pictureMap.CreateGraphics())
            {
                Point p = ScreenCoordinatesAt(stpSymbol.Location.Coords[0]);
                // Draw circle fitting a rectangle centered at the point
                g.FillEllipse(brush, p.X - 5, p.Y - 5, 10, 10);
            }
        }
        else if (stpSymbol.Location.Coords.Count > 1)
        {
            using (var pen = new Pen(color, 4))
            using (var g = pictureMap.CreateGraphics())
            {
                var coords = stpSymbol.GetLinearSymbolCoords();
                if (coords != null)
                {
                    Point last = ScreenCoordinatesAt(coords[0]);
                    foreach (var coord in coords)
                    {
                        Point p = ScreenCoordinatesAt(coord);
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

