using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StpSDK;
using StpSDK.Mapping;
using StpSDK.Speech;
using AzureRecognitionResult = Microsoft.CognitiveServices.Speech.SpeechRecognitionResult;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace StpSDKSample;
public partial class Form1 : Form
{
    #region Private Properties and constants
    private static ILogger _logger;
    private static AppParams _appParams;

    private static StpRecognizer _stpRecognizer;
    private AzureSpeechRecognizer _speechRecognizer;
    private Mapping _mapHandler;

    private Dictionary<string, StpSymbol> _currentSymbols;
    private StpTask _currentTask;
    private BindingSource _bindingSource = new BindingSource();

    private const int TimeOutSec = 120;
    #endregion

    #region Construction/Teardown
    /// <summary>
    /// Construct form IOptions provided via dependency injection
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="agent"></param>
    /// <param name="bridgeParamsOptions"></param>
    public Form1(ILoggerFactory loggerFactory, IOptions<AppParams> appSettings)
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

        InitializeComponent();
        ResizeRedraw = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer, true);

        // Initialize the symbol cache
        _currentSymbols = new();

        // Load initial/blank datagridview and associate the data property names with StpItem fields
        _bindingSource.DataSource = new List<StpItem>();
        dataGridViewAlternates.AutoGenerateColumns = false;
        dataGridViewAlternates.DataSource = _bindingSource;
        FullDescription.DataPropertyName = "FullDescription";
        Confidence.DataPropertyName = "Confidence";
    }

    /// <summary>
    /// Initialize components and connect to STP on form load
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Form1_Load(object sender, EventArgs e)
    {
        // Attempt to connect to STP
        await Connect();
    }

    /// <summary>
    /// Disconnect from STP when form is closed
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        Disconnect();
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
    internal async Task<bool> Connect()
    {
        bool success;
        try
        {
            // Create an STP connection object - using STP's native pub/sub system via TCP or WebSockets
            IStpConnector stpConnector = new StpOaaConnector(_logger, toolStripTextBoxStpUri.Text);

            // Re/initialize the STP recognizer with the connector definition
            Disconnect();
            _stpRecognizer = new StpRecognizer(stpConnector);

            // Hook up to the events _before_ connecting, so that the correct message subscriptions can be identified
            // A new symbol has been added, updated or removed
            _stpRecognizer.OnSymbolAdded += StpRecognizer_OnSymbolAdded;
            _stpRecognizer.OnSymbolModified += StpRecognizer_OnSymbolModified;
            _stpRecognizer.OnSymbolDeleted += StpRecognizer_OnSymbolDeleted;

            // Tasking
            _stpRecognizer.OnTaskAdded += StpRecognizer_OnTaskAdded;
            _stpRecognizer.OnTaskModified += StpRecognizer_OnTaskModified;
            _stpRecognizer.OnTaskDeleted += StpRecognizer_OnTaskDeleted;

            // Edit operations, including map commands
            _stpRecognizer.OnSymbolEdited += StpRecognizer_OnSymbolEdited;
            _stpRecognizer.OnMapOperation += StpRecognizer_OnMapOperation;

            // Speech recognition and ink feedback
            _stpRecognizer.OnSpeechRecognized += StpRecognizer_OnSpeechRecognized;
            _stpRecognizer.OnListeningStateChanged += StpRecognizer_OnListeningStateChanged;
            _stpRecognizer.OnSketchRecognized += StpRecognizer_OnSketchRecognized;
            _stpRecognizer.OnSketchIntegrated += StpRecognizer_OnSketchIntegrated;

            // Message from STP to be conveyed to user
            _stpRecognizer.OnStpMessage += StpRecognizer_OnStpMessage;

            // Connection error notification
            _stpRecognizer.OnConnectionError += StpRecognizer_OnConnectionError;

            // STP is being shutdown 
            _stpRecognizer.OnShutdown += StpRecognizer_OnShutdown;

            // Attempt to connect
            ShowStpMessage("---------------------------------");
            ShowStpMessage("Connecting...");
            success = _stpRecognizer.ConnectAndRegister("SpeechSample");
        }
        catch
        {
            success = false;
        }
        ShowConnectionSuccess(success);

        // Nothing else to do if connection failed
        if (!success)
        {
            return false;
        }

        // Create the embedded speech recognizer
        _speechRecognizer = new AzureSpeechRecognizer(_appParams.AzureKey,
            _appParams.AzureRegion, _appParams.AzureLang, _appParams.AzureEndpoint);
        _speechRecognizer.OnRecognized += SpeechRecognizer_OnRecognized;
        _speechRecognizer.OnRecognizing += SpeechRecognizer_OnRecognizing;
        _speechRecognizer.OnListeningStateChanged += SpeechRecognizer_OnListeningStateChanged;
        _speechRecognizer.OnError += SpeechRecognizer_OnError;

        // Hook up to the map handler
        _mapHandler = new Mapping(_logger,
            pictureMap,
            _appParams.MapImagePath,
            new LatLon(_appParams.MapTopLat, _appParams.MapLeftLon),
            new LatLon(_appParams.MapBottomLat, _appParams.MapRightLon));
        _mapHandler.OnPenDown += MapHandler_OnPenDown;
        _mapHandler.OnStrokeCompleted += MapHandler_OnStrokeCompleted;

        // Manual user edits - selection of alternate interpretations and manual deletions
        //// Handle selection of alternate interpretations
        //dataGridViewAlternates.RowStateChanged += DataGridViewAlternates_RowStateChanged;
        // Handle manual symbol deletion and update
        buttonDelete.Click += ButtonDelete_Click;
        buttonUpdate.Click += ButtonUpdate_Click;

        // Offer to join ongoing session if there is one, or start new scenario otherwise
        if (await _stpRecognizer.HasActiveScenarioAsync())
        {
            if (DialogResult.Yes == MessageBox.Show(
                $"Join current STP scenario? Yes to Join, No to reset to a new scenario", 
                "Scenario Option", 
                MessageBoxButtons.YesNo))
            {
                await DoJoinScenarioAsync();
                return true;
            }
        }

        // Start new empty scenario
        await DoNewScenarioAsync();
        return true;
    }

    /// <summary>
    /// Disconnect from stp
    /// </summary>
    private void Disconnect()
    {
        if (_stpRecognizer is not null)
        {
            _stpRecognizer.OnSymbolAdded -= StpRecognizer_OnSymbolAdded;
            _stpRecognizer.OnSymbolModified -= StpRecognizer_OnSymbolModified;
            _stpRecognizer.OnSymbolDeleted -= StpRecognizer_OnSymbolDeleted;
            _stpRecognizer.OnTaskAdded -= StpRecognizer_OnTaskAdded;
            _stpRecognizer.OnTaskModified -= StpRecognizer_OnTaskModified;
            _stpRecognizer.OnTaskDeleted -= StpRecognizer_OnTaskDeleted;
            _stpRecognizer.OnSymbolEdited -= StpRecognizer_OnSymbolEdited;
            _stpRecognizer.OnMapOperation -= StpRecognizer_OnMapOperation;
            _stpRecognizer.OnSpeechRecognized -= StpRecognizer_OnSpeechRecognized;
            _stpRecognizer.OnListeningStateChanged -= StpRecognizer_OnListeningStateChanged;
            _stpRecognizer.OnSketchRecognized -= StpRecognizer_OnSketchRecognized;
            _stpRecognizer.OnSketchIntegrated -= StpRecognizer_OnSketchIntegrated;
            _stpRecognizer.OnStpMessage -= StpRecognizer_OnStpMessage;
            _stpRecognizer.OnConnectionError -= StpRecognizer_OnConnectionError;
            _stpRecognizer.OnShutdown -= StpRecognizer_OnShutdown;

            _stpRecognizer.Disconnect();
            _stpRecognizer.Dispose();
            _stpRecognizer = null;
        }

        if (_speechRecognizer is not null)
        {
            _speechRecognizer.OnRecognized -= SpeechRecognizer_OnRecognized;
            _speechRecognizer.OnListeningStateChanged -= SpeechRecognizer_OnListeningStateChanged;
            _speechRecognizer.OnError -= SpeechRecognizer_OnError;

            _speechRecognizer.Dispose();
            _speechRecognizer = null;
        }
    }

    /// <summary>
    /// STP has recognized a new symbol
    /// </summary>
    /// <param name="poid">Unique identifier</param>
    /// <param name="stpItem">Recognized item</param>
    /// <param name="isUndo">True if this event represents a compensating action to undo a symbol delete</param>
    private void StpRecognizer_OnSymbolAdded(string poid, StpItem stpItem, bool isUndo)
    {
        ShowStpMessage("---------------------------------");
        string msg = $"SYMBOL ADDED:\t{stpItem.Poid}\t{stpItem.FullDescription}";
        ShowStpMessage(msg);

        // Get the recognized item as a military symbol - not interested in other types of objects 
        if (stpItem is StpSymbol stpSymbol)
        {
            _currentSymbols[stpSymbol.Poid] = stpSymbol;
            DisplaySymbol(stpSymbol);
        }
    }

    /// <summary>
    /// A symbol has been updated - via STP speech and sketch edits or manually by th user
    /// </summary>
    /// <param name="poid"></param>
    /// <param name="stpSymbol"></param>
    /// <param name="isUndo"></param>
    private void StpRecognizer_OnSymbolModified(string poid, StpItem stpItem, bool isUndo)
    {
        ShowStpMessage("---------------------------------");
        string msg = $"SYMBOL MODIFIED:\t{stpItem.Poid}\t{stpItem.FullDescription}";
        ShowStpMessage(msg);

        // Display the modified  item as a military symbol - not interested in other types of objects 
        if (stpItem is StpSymbol stpSymbol)
        {
            _currentSymbols[stpSymbol.Poid] = stpSymbol;
            DisplaySymbol(stpSymbol);
        }
    }

    /// <summary>
    /// A symbol has been removed
    /// </summary>
    /// <param name="poid"></param>
    /// <param name="isUndo"></param>
    private void StpRecognizer_OnSymbolDeleted(string poid, bool isUndo)
    {
        ShowStpMessage("---------------------------------");
        string msg = $"SYMBOL DELETED:\t{poid}";
        ShowStpMessage(msg);

        // Remove from cache and display
        if (_currentSymbols.ContainsKey(poid))
        {
            ClearSymbol(poid);
        }
    }

    /// <summary>
    /// Symbol edit operation
    /// </summary>
    /// <remarks>
    /// Operation other than "move" and "delete", which are turned directly by STP into updates.
    /// One example is selection, which depends on a local UI</remarks>
    /// <param name="operation"></param>
    /// <param name="location"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void StpRecognizer_OnSymbolEdited(string operation, Location location)
    {
        ShowStpMessage("---------------------------------");
        string msg = $"EDIT OPERATION:\t{operation}";
        ShowStpMessage(msg);
    }

    /// <summary>
    /// Map operations, such as zoom, pan
    /// </summary>
    /// <remarks>
    /// These operations cause a UI effect, and need therefore to be handled by the client app</remarks>
    /// <param name="operation"></param>
    /// <param name="location"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void StpRecognizer_OnMapOperation(string operation, Location location)
    {
        ShowStpMessage("---------------------------------");
        string msg = $"MAP OPERATION:\t{operation}";
        ShowStpMessage(msg);
    }

    /// <summary>
    /// Speech recognition results
    /// </summary>
    /// <param name="speechList"></param>
    private void StpRecognizer_OnSpeechRecognized(List<string> speechList)
    {
        // Display to provide users feedback on the input
        if (speechList != null && speechList.Count > 0)
        {
            // Show just top alternates to avoid best being hidden by scroll
            int max = speechList.Count > 5 ? 5 : speechList.Count;
            string concat = string.Join(" | ", speechList.GetRange(0, max));
            if (max < speechList.Count)
            {
                concat += " | ...";
            }
            ShowSpeechReco(concat);
        }
    }

    /// <summary>
    /// Audio capture has been turned on/off - signal to the user that the mike is open or not
    /// </summary>
    /// <param name="isListening"></param>
    private void StpRecognizer_OnListeningStateChanged(bool isListening)
    {
        ColorListeningBar(isListening ? Color.Green : SystemColors.Control);
    }

    /// <summary>
    /// Change the color of the speech text box border to to indicate audio activity
    /// </summary>
    /// <param name="color"></param>
    private void ColorListeningBar(Color color)
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => ColorListeningBar(color)));
            return;
        }
        // Change the color of the speech text box to green while on
        panelAudioCapture.BackColor = color;

    }
    /// <summary>
    /// Sketch has been processed by STP, so mark it a different color so user can get a sense of progress
    /// </summary>
    /// <param name="sketchList"></param>
    private void StpRecognizer_OnSketchRecognized(List<SketchRecoResult> sketchList)
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => StpRecognizer_OnSketchRecognized(sketchList)));
            return;
        }
        // Change color
        _mapHandler.MarkInkAsProcessed();
    }

    /// <summary>
    /// Symbol fusing the sketched gesture was produced, so can clear ink marks to declutter the display
    /// </summary>
    /// <remarks>
    /// An alternative is to keep the ink in an overlay and allow users to show/hide that
    /// </remarks>
    private void StpRecognizer_OnSketchIntegrated()
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => StpRecognizer_OnSketchIntegrated()));
            return;
        }
        // Remove ink
        _mapHandler.ClearInk();
    }

    private void StpRecognizer_OnTaskAdded(string poid, StpTask stpTask, List<string> taskPoids, bool isUndo)
    {
        _currentTask = stpTask;
        DisplayTask(_currentTask);
    }

    private void StpRecognizer_OnTaskModified(string poid, StpTask stpTask, List<string> tgPoids, bool isUndo)
    {
        ShowStpMessage("---------------------------------");
        string msg = $"TASK MODIFIED:\t{stpTask.Poid}\t{stpTask.FullDescription}";
        ShowStpMessage(msg);
        _currentTask = stpTask;
        DisplayTask(_currentTask);
    }

    private void StpRecognizer_OnTaskDeleted(string poid, bool isUndo)
    {
        ShowStpMessage("---------------------------------");
        string msg = $"Task DELETED:\t{poid}";
        ShowStpMessage(msg);
        _currentTask = null;
        DisplayTask(_currentTask);
    }

    /// <summary>
    /// Connection error notification
    /// </summary>
    /// <param name="sce"></param>
    private void StpRecognizer_OnConnectionError(StpCommunicationException sce)
    {
        MessageBox.Show("Connection to STP was lost. Verify that the service is running and restart this app", "Connection Lost", MessageBoxButtons.OK);
        //Application.Exit();
    }

    /// <summary>
    /// Show message receive from STP
    /// </summary>
    /// <param name="level"></param>
    /// <param name="msg"></param>
    private void StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel level, string msg)
    {
        ShowStpMessage(msg);
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

    #region Speech event handlers
    /// <summary>
    /// Audio collection started/stopped
    /// </summary>
    /// <param name="isListening"></param>
    private void SpeechRecognizer_OnListeningStateChanged(bool isListening)
    {
        StpRecognizer_OnListeningStateChanged(isListening);
    }

    /// <summary>
    /// Speech utterance recognized
    /// </summary>
    /// <param name="sr"></param>
    private void SpeechRecognizer_OnRecognized(SpeechRecoResult sr)
    {
        if (sr is null || sr.Results is null || sr.Results.Count == 0)
        {
            return;
        }
        // Send over to stp for processing
        _stpRecognizer.SendSpeechRecognition(sr.Results, sr.StartTime, sr.EndTime);

        // Extract list of recognized alternates and display locally
        List<string> speechList = sr.Results.Select(a => a.Text).ToList();
        StpRecognizer_OnSpeechRecognized(speechList);
    }

    /// <summary>
    /// Partial speech transcription results
    /// </summary>
    /// <param name="phrase"></param>
    private void SpeechRecognizer_OnRecognizing(string phrase)
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => SpeechRecognizer_OnRecognizing(phrase)));
            return;
        }
        txtSimSpeech.Text = phrase;
        ShowStpMessage(phrase);
    }

    /// <summary>
    /// Speech recognition error detected
    /// </summary>
    /// <param name="msg"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void SpeechRecognizer_OnError(string msg)
    {
        ShowStpMessage(msg);
    }
    #endregion

    #region User symbol edit commands
    /// <summary>
    /// Alternate selection from "n-best" list
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void DataGridViewAlternates_RowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
    {
        // Update the symbol with the corresponding interpretation when row is selected
        if (e.StateChanged == DataGridViewElementStates.Selected)
        {
            var item = e.Row.DataBoundItem as StpItem;
            if (item != null)
            {
                // Inform STP that the item with this rank order was chosen by the user
                // Note that the first row corresponds to the currently selected / displayed symbol 
                // No need to chose it again if the user selected the first row
                if (item is StpSymbol && e.Row.Index > 0)
                {
                    // STP will issue a symbol update notification (OnSymbolModefied) with this element first in the list
                    // of alternates
                    _stpRecognizer.ChooseAlternate(item.Poid, item.Order);
                }
                else if (item is StpTask)
                {
                    // For tasks, even selecting the first / best item is a valid option, as that will then be confirmed and
                    // in many cases generate an anticipated unit at the target location (e.g. the objective being attacked)
                    // Task confirmation will cause STP to issue a task update notification (OnTaskModified) with the chosen
                    // element as the single selected task
                    await _stpRecognizer.ConfirmTaskAsync(item.Poid, item.Order);
                }
            }
        }
    }

    /// <summary>
    /// Manual deletion
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void ButtonDelete_Click(object sender, EventArgs e)
    {
        // Bail if no symbol 
        if (propertyGridResult.SelectedObject is null)
        {
            return;
        }

        // Get id of what is loaded
        string currentPoid = (propertyGridResult.SelectedObject as RootVM).Id;

        // Delete task or symbol, depending on what is currently displayed
        if (propertyGridResult.SelectedObject is TaskVM)
        {
            // Inform STP that this task should be deleted
            // STP will issue a symbol deletion notification (OnTaskRemoved), 
            // which will cause the actual eventual removal
            _stpRecognizer.DeleteTask(currentPoid);

        }
        else
        {
            // Inform STP that this symbol should be deleted
            // STP will issue a symbol deletion notification (OnSymbolRemoved), 
            // which will cause the actual eventual removal
            _stpRecognizer.DeleteSymbol(currentPoid);
        }
    }

    /// <summary>
    /// Manula updates
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonUpdate_Click(object sender, EventArgs e)
    {
        // Bail if no symbol 
        if (propertyGridResult.SelectedObject is null)
        {
            return;
        }

        // Delete task or symbol, depending on what is currently displayed
        if (propertyGridResult.SelectedObject is TaskVM taskVM)
        {
            // Inform STP that this task should be deleted
            // STP will issue a symbol deletion notification (OnTaskModified), 
            // which will cause the actual eventual update
            _stpRecognizer.UpdateTask(taskVM.Id, (StpTask)taskVM.AsStpItem());

        }
        else if (propertyGridResult.SelectedObject is SymbolVM symbolVM)
        {
            // Inform STP that this symbol should be updated
            // STP will issue a symbol deletion notification (OnSymbolModified), 
            // which will cause the actual eventual update
            _stpRecognizer.UpdateSymbol(symbolVM.Id, symbolVM.AsStpItem());
        }
        else
        {
            System.Diagnostics.Debug.Fail("Unexpected ViewModel item type");
        }
    }
    #endregion

    #region Mapping event handlers
    /// <summary>
    /// Notify STP that the user started to sketch
    /// </summary>
    /// <remarks>
    /// STP will propagate this event, which will cause for example speech recognizers to be activated
    /// to collect user speech to fuse with this sketched gesture
    /// </remarks>
    /// <param name="sender"></param>
    /// <param name="geoPoint"></param>
    private void MapHandler_OnPenDown(object sender, LatLon geoPoint)
    {
        // Notify STP of the start of a stroke and activate speech recognition
        _stpRecognizer.SendPenDown(geoPoint, DateTime.Now);
        // Trigger speech recognition (asynchronously), in case it is not already ongoing
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
         _speechRecognizer.RecognizeOnceAsync(audioDeviceId:null);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    /// <summary>
    /// Notify STP that the user completed a stroke/sketch gesture
    /// </summary>
    /// <remarks>
    /// If speech matching the sketch is produced, STP will produce a ranked list of interpretations
    /// that will be broadcast, for example as OnSymbolAdded, or OnSymbolModified events
    /// </remarks>
    /// <param name="sender"></param>
    /// <param name="penStroke"></param>
    private void MapHandler_OnStrokeCompleted(object sender, Mapping.PenStroke penStroke)
    {
        // To support multimodal symbol editing, it is necessary for the app to identify the existing elements
        // that a stroke intersects, for example, a point or line over a unit that one wants to move, delete,
        // of change attributes.
        List<string> intersectedPoids = _mapHandler.IntesectedSymbols(_currentSymbols?.Values.ToList());

        _stpRecognizer.SendInk(penStroke.PixelBounds,
                               penStroke.TopLeftGeo,
                               penStroke.BotRightGeo,
                               penStroke.Stroke,
                               penStroke.TimeStart,
                               penStroke.TimeEnd,
                               intersectedPoids);
    }
    #endregion

    #region Display methods
    /// <summary>
    /// Display symbol properties
    /// </summary>
    /// <param name="stpSymbol"></param>
    private void DisplaySymbol(StpSymbol stpSymbol)
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => DisplaySymbol(stpSymbol)));
            return;
        }

        DisplayItemDetails(stpSymbol);
        RenderOnMap();
        ListAlternates(stpSymbol);
    }

    private void ClearSymbol(string poid)
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => ClearSymbol(poid)));
            return;
        }

        // Remove from cache
        _currentSymbols.Remove(poid);

        // Remove from map - clear map and re-render remaining symbols
        _mapHandler.ClearMap();
        RenderOnMap();

        // Clear display
        DisplayItemDetails(null);
        ListAlternates(null);
    }

    /// <summary>
    /// Display task properties
    /// </summary>
    /// <param name="stpSymbol"></param>
    private void DisplayTask(StpTask stpTask)
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => DisplayTask(stpTask)));
            return;
        }

        DisplayItemDetails(stpTask);
        ListAlternates(stpTask);
    }

    /// <summary>
    /// Show symbol properties
    /// </summary>
    /// <param name="stpItem"></param>
    private void DisplayItemDetails(StpItem stpItem)
    {
        // Set state of the delete button - enabled if there is a symbol on the display
        buttonDelete.Enabled = buttonUpdate.Enabled = stpItem != null;

        // Create a formatted object to display - may be null
        propertyGridResult.SelectedObject = ViewModel.Create(stpItem);
        propertyGridResult.Refresh();
    }

    /// <summary>
    ///  List alternates on the log window
    /// </summary>
    /// <param name="stpItem"></param>
    private void ListAlternates(StpItem stpItem)
    {
        // Clear previous alternate list
        _bindingSource.Clear();
        dataGridViewAlternates.Refresh();

        // Nothing else if the symbol is empty (was deleted)
        if (stpItem == null)
        {
            return;
        }

        // Load new list of alternates for the user to chose from
        // Suspend row change event handling while new content is loading to avoid firing of alternates
        // as the grid is populated
        dataGridViewAlternates.RowStateChanged -= DataGridViewAlternates_RowStateChanged;
        _bindingSource.DataSource = stpItem.Alternates;
        dataGridViewAlternates.Refresh();
        // Start wiht no row selected - selecting the first/best task is a valid option
        dataGridViewAlternates.ClearSelection();
        dataGridViewAlternates.RowStateChanged += DataGridViewAlternates_RowStateChanged;

        // Show each item in the n-best list in the log display 
        ShowStpMessage("---------------------------------");
        ShowStpMessage(stpItem.Type.ToUpper());
        foreach (var reco in stpItem.Alternates)
        {
            if (reco is null)
                continue;
            string msg = $"{reco.Order:00} ({reco.Confidence:0.0000}) :\t{reco.FullDescription}";
            ShowStpMessage(msg);
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

    /// <summary>
    /// Show a message in the log window
    /// </summary>
    /// <param name="msg"></param>
    private void ShowStpMessage(string msg)
    {
        if (this.InvokeRequired)
        {
            this.Invoke((MethodInvoker)(() => ShowStpMessage(msg)));  // recurse into UI thread if we need to
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
    /// Clear map and render all cached symbols so that changes and deletions are visible
    /// </summary>
    private void RenderOnMap()
    {
        _mapHandler.ClearMap();
        foreach (var symbol in _currentSymbols)
        {
            _mapHandler.RenderSymbol(symbol.Value);
        }
    }
    #endregion

    #region Form events
    private void PlaBtn_Click(object sender, EventArgs e)
    {
        ClearButtons();
        plaBtn.Checked = true;
        tsLabelTiming.Text = "Mode: Freehand Points,Lines,Areas";
        _stpRecognizer.ChangeTimeOut(TimingConstants.Timing_PLA);
    }

    private void DrawBtn_Click(object sender, EventArgs e)
    {
        ClearButtons();
        drawBtn.Checked = true;
        tsLabelTiming.Text = "Mode: Draw 2525 Symbols";
        _stpRecognizer.ChangeTimeOut(TimingConstants.Timing_Drawing);
    }

    private void ClearButtons()
    {
        plaBtn.Checked = drawBtn.Checked = false;
    }

    private void toolStripButtonConnect_Click(object sender, EventArgs e)
    {
        Application.UseWaitCursor = true;
        Application.DoEvents();
        
        // Re/connect to STP using the current connection type and Uri
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Connect();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    }

    private void ShowConnectionSuccess(bool success)
    {
        Application.UseWaitCursor = false;

        ShowStpMessage("---------------------------------");
        if (success)
        {
            ShowStpMessage($"Connected to {toolStripTextBoxStpUri.Text}");
            toolStripTextBoxStpUri.ForeColor = Color.Green;
        }
        else
        {
            ShowStpMessage($"Failed to connect to {toolStripTextBoxStpUri.Text}");
            ShowStpMessage($"Please make sure STP is running and click Connect to try again");
            toolStripTextBoxStpUri.ForeColor = Color.Red;
        }
        // Reset the button's appearance
        ResetPressed(toolStripButtonConnect);
    }

    private void ResetPressed(ToolStripButton button)
    {
        // From: https://stackoverflow.com/a/41794848/852915
        button.Visible = false;
        button.Visible = true;
    }
    #endregion

    #region Scenario button handling
    /// <summary>
    /// Handle new scenario button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void buttonScenarioNew_Click(object sender, EventArgs e)
    {
        if (await _stpRecognizer.HasActiveScenarioAsync())
        {
            if (DialogResult.OK != MessageBox.Show(
                "Start New, removing all symbols from STP? Ok to start new, cancel to abort",
                "Clear Confirmation",
                MessageBoxButtons.OKCancel))
            {
                return;
            }
        }
        // Create new scenario
        await DoNewScenarioAsync();
    }

    /// <summary>
    /// Handle scenario jon button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void buttonScenarioJoin_Click(object sender, EventArgs e)
    {
        // Clear all local caches
        ClearCaches();

        // Retrieve current STP scenario and load locally
        await DoJoinScenarioAsync();
    }

    /// <summary>
    /// Handle scenario save button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void buttonScenarioSave_Click(object sender, EventArgs e)
    {
        // Get save file path
        SaveFileDialog dlg = new()
        {
            Filter = "STP documents (*.op)|*.op|All files (*.*)|*.*",
            FilterIndex = 1,
            RestoreDirectory = true,
            OverwritePrompt = true
        };
        dlg.FileName = Path.Combine(dlg.InitialDirectory, "STP.op");

        // Show the dialog and retrieve the selection if there was one
        if (dlg.ShowDialog() != DialogResult.OK)
        {
            return;
        }
        string filePath = dlg.FileName;

        // Perform the actual saving
        await DoSaveScenarioAsync(filePath);
    }

    /// <summary>
    /// Handle scenario load button
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void buttonScenarioLoad_Click(object sender, EventArgs e)
    {
        // If scenario loaded already, confirm new/overwrite
        if (await _stpRecognizer.HasActiveScenarioAsync())
        {
            if (DialogResult.No == MessageBox.Show("Replace the currently loaded scenario? All symbols will be removed", "Confirm Scenario Load", MessageBoxButtons.YesNo))
            {
                return;
            }
        }
        // Get the file location
        OpenFileDialog dlg = new()
        {
            Title = "Select scenario file to load",
            Filter = "STP scenario file (*.op)|*.op|All files (*.*)|*.*",
        };
        if (dlg.ShowDialog() != DialogResult.OK)
        {
            return;
        }
        string filePath = dlg.FileName;

        // Perform the actual loading
        await DoLoadScenarioAsync(filePath);
    }
    #endregion

    #region Scenario methods
    /// <summary>
    /// Create a new scenario
    /// </summary>
    /// <remarks>
    /// STP content is purged and replaced by a new empty scenario 
    /// </remarks>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    private async Task DoNewScenarioAsync()
    {
        await PerformLongOp(async () =>
        {
            ShowStpMessage("---------------------------------");
            string name = $"StpSDKSample{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ")}";
            ShowStpMessage($"Creating new scenario: {name}");

            // Launch operation
            CancellationTokenSource cts = new();
            cts.CancelAfter(TimeSpan.FromSeconds(TimeOutSec));
            await _stpRecognizer.CreateNewScenarioAsync(name, cts.Token);
            ShowStpMessage("---------------------------------");
        });
    }

    /// <summary>
    /// Join: retrieve symbols currently loaded in STP and add them to the local app
    /// </summary>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    private async Task DoJoinScenarioAsync()
    {
        await PerformLongOp( async () =>
        {
            ShowStpMessage("---------------------------------");
            ShowStpMessage($"Joining scenario");

            // Launch operation
            CancellationTokenSource cts = new();
            cts.CancelAfter(TimeSpan.FromSeconds(TimeOutSec));
            await _stpRecognizer.JoinScenarioSessionAsync(cts.Token);
            ShowStpMessage("---------------------------------");
        });
    }

    /// <summary>
    /// Save scenario to file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    private async Task DoSaveScenarioAsync(string filePath)
    {
        await PerformLongOp(async () =>
        {
            ShowStpMessage("---------------------------------");
            ShowStpMessage($"Saving scenario to {filePath}");

            // Get the current contents
            string content = await _stpRecognizer.GetScenarioContentAsync();

            // Save to file
            await File.WriteAllTextAsync(filePath, content);
            ShowStpMessage("---------------------------------");
        });
    }

    /// <summary>
    /// Load scenario from file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    private async Task DoLoadScenarioAsync(string filePath)
    {
        await PerformLongOp(async () =>
        {
            ShowStpMessage("---------------------------------");
            ShowStpMessage($"Loading new scenario from {filePath}");

            // Load the file contents
            string content = File.ReadAllText(filePath).Replace("\n", string.Empty).Replace("\r", string.Empty);

            // Launch operation
            CancellationTokenSource cts = new();
            cts.CancelAfter(TimeSpan.FromSeconds(TimeOutSec));
            await _stpRecognizer.LoadNewScenarioAsync(content, cts.Token);
            ShowStpMessage("---------------------------------");
        });
    }
    #endregion

    #region Utility
    /// <summary>
    /// Clear all local caches
    /// </summary>
    private void ClearCaches()
    {
        _currentSymbols = new();
        _currentTask = null;
    }

    /// <summary>
    /// Perform a button-triggered action with progress indicator
    /// </summary>
    /// <param name="button"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    private async Task PerformLongOp(Func<Task> action)
    {
        try
        {
            // Set wait cursor and disable all buttons
            Application.UseWaitCursor = true;
            Application.DoEvents();
            groupBoxScenario.Enabled = false;

            // Perform the action in its own thread - side effects will be handled on the UI thread, as panels and map are updated
            await Task.Run(async () => 
                await action()
            );
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning($"Operation timed out after {TimeOutSec}");
            MessageBox.Show("Operation is taking too long. Please retry if needed", "Timeout", MessageBoxButtons.OK);
            ShowStpMessage("Operation timed out");
            ShowStpMessage("---------------------------------");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Operation failed: {ex}");
            MessageBox.Show($"Operation failed: {ex.Message}", "Error performing scenario operation", MessageBoxButtons.OK);
            ShowStpMessage($"Operation failed: {ex.Message}");
            ShowStpMessage("---------------------------------");
        }
        finally
        {
            // Restore cursor and buttons
            Application.UseWaitCursor = false;
            Application.DoEvents();
            groupBoxScenario.Enabled = true;
        }
    }
    #endregion


}

