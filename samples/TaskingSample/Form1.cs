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

    private Mapping _mapHandler;
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

        InitializeComponent();
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
        _stpRecognizer.OnSymbolAdded += (poid, stpItem, isUndo) =>
        {
            // Get the recognized item as a military symbol - not interested in other types of objects 
            if (stpItem is StpSymbol stpSymbol)
            {
                DisplaySymbol(stpSymbol);
            }
        };
        _stpRecognizer.OnSymbolModified += (poid, stpSymbol, isUndo) =>
        {
            ShowStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
            string msg = $"SYMBOL MODIFIED:\t{stpSymbol.Poid}\t{stpSymbol.FullDescription}";
            ShowStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
        };
        _stpRecognizer.OnSymbolDeleted += (poid, isUndo) =>
        {
            ShowStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
            string msg = $"SYMBOL DELETED:\t{poid}";
            ShowStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
        };

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

        _stpRecognizer.OnSpeechRecognized += (speechList) =>
        {
            if (speechList != null && speechList.Count > 0)
            {
                // Show just top 5 to avoid best being hidden by scroll
                int max = speechList.Count > 5 ? 5 : speechList.Count;
                string concat = string.Join(" | ", speechList.GetRange(0, max));
                ShowSpeechReco(concat);
            }
        };

        _stpRecognizer.OnInkProcessed += () =>
        {
            _mapHandler.ClearInk();
        };

        _stpRecognizer.OnStpMessage += (msg, level) =>
        {
            /// Applications that can only be run when STP is available should shutdown,
            /// or at least advise users that the service is not available and provide means to reconnect
            ShowStpMessage(msg, level);
        };
        _stpRecognizer.OnConnectionError += (sce) =>
        {
            MessageBox.Show("Connection to STP was lost. Verify that the service is running and restart this app", "Connection Lost", MessageBoxButtons.OK);
            Application.Exit();
        };
        _stpRecognizer.OnShutdown += () =>
        {
            /// Applications that can only be run when STP is available should shutdown,
            /// or at least advise users that the service is not available and provide means to reconnect
            MessageBox.Show("Connection to STP was lost. Verify that the service is running and restart this app", "Connection Lost", MessageBoxButtons.OK);
            Application.Exit();
        };

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
            return false;
        }

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

        // Clear any previous STP state to start with a clean slate
        _stpRecognizer.ResetStpScenario();

        return true;
    }
    #endregion

    #region Display methods
    private void DisplaySymbol(StpSymbol stpSymbol)
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => DisplaySymbol(stpSymbol)));
            return;
        }

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
        _mapHandler.RenderSymbol(stpSymbol);

        // List alternates in log window
        ListAlternates(stpSymbol);
    }


    /// <summary>
    ///  List alternates on the log window
    /// </summary>
    /// <param name="stpItem"></param>
    private void ListAlternates(StpItem stpItem)
    {
        ShowStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        ShowStpMessage(StpRecognizer.StpMessageLevel.Info, stpItem.Type.ToUpper());
        // Show each item in the n-best list in the log display
        foreach (var reco in stpItem.Alternates)
        {
            if (reco is null)
                continue;
            string msg = $"{reco.Order:00} ({reco.Confidence:0.0000}) :\t{reco.FullDescription}";
            ShowStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
        }
    }

    private void DisplayTask(StpTask stpTask)
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => DisplayTask(stpTask)));
            return;
        }
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

    private void ShowStpMessage(StpRecognizer.StpMessageLevel level, string msg)
    {
        if (this.InvokeRequired)
        {
            this.Invoke((MethodInvoker)(() => ShowStpMessage(level, msg)));  // recurse into UI thread if we need to
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
    #endregion

    #region Form events
    private void PlaBtn_Click(object sender, EventArgs e)
    {
        ClearTimingButtons();
        plaBtn.Checked = true;
        tsLabelTiming.Text = "Mode: Freehand Points,Lines,Areas";
        ChangeTimeOut(TimingConstants.Timing_PLA);
    }

    private void DrawBtn_Click(object sender, EventArgs e)
    {
        ClearTimingButtons();
        drawBtn.Checked = true;
        tsLabelTiming.Text = "Mode: Draw 2525 Symbols";
        ChangeTimeOut(TimingConstants.Timing_Sketch);
    }

    private void ClearTimingButtons()
    {
        plaBtn.Checked = drawBtn.Checked = false;
    }

    private void BtnClearLog_Click_1(object sender, EventArgs e)
    {
        if (MessageBox.Show("Remove all symbols from STP?", "Clear Confirmation", MessageBoxButtons.OKCancel) == DialogResult.OK)
        {
            ResetScenario();
        }
    }
    #endregion

    #region State setting methods 
    /// <summary>
    /// Set STP's sketching timeout to support PLA, drawing, or other mode
    /// </summary>
    /// <param name="_timeout"></param>
    private void ChangeTimeOut(double _timeout)
    {
        _stpRecognizer.ResetWaitTimeout();
        _stpRecognizer.ResetSegmentationTimeout();

        _stpRecognizer.SetWaitTimeout(_timeout);
        _stpRecognizer.SetSegmentationTimeout(_timeout);
    }

    /// <summary>
    /// Clear STP's scenario, removing all symbols, and update log and map 
    /// </summary>
    private void ResetScenario()
    {
        // Reset STP scenario - all symbols are deleted and STP is returned to a clean state
        _stpRecognizer.ResetStpScenario();

        // Clear log window
        textBoxLog.Clear();

        // Clear the map display
        _mapHandler.ClearMap();
    }
    #endregion
}
