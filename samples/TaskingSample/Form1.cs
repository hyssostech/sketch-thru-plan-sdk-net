﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StpSDK;
using StpSDK.Mapping;
using System.ComponentModel;

namespace StpSDKSample;
public partial class Form1 : Form
{
    #region Private Properties and constants
    private static ILogger _logger;
    private static AppParams _appParams;

    private static StpRecognizer _stpRecognizer;
    private Mapping _mapHandler;

    private Dictionary<string, StpSymbol> _currentSymbols;
    private StpTask _currentTask;
    private BindingSource _bindingSource = new BindingSource();
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
        if (! await Connect())
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
    internal async Task<bool> Connect()
    {
        bool success;
        try
        {
            // Create an STP connection object - using STP's native pub/sub system via TCP or WebSockets
            IStpConnector stpConnector = new StpOaaConnector(_logger, toolStripTextBoxStpUri.Text);

            // Initialize the STP recognizer with the connector definition
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
            string session = await _stpRecognizer.ConnectAndRegisterAsync("TaskingSample");
            success = !string.IsNullOrWhiteSpace(session);
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
        _mapHandler = new Mapping(_logger, 
            pictureMap,
            _appParams.MapImagePath,
            new LatLon(_appParams.MapTopLat, _appParams.MapLeftLon), 
            new LatLon(_appParams.MapBottomLat, _appParams.MapRightLon));
        _mapHandler.OnPenDown += MapHandler_OnPenDown;
        _mapHandler.OnStrokeCompleted += MapHandler_OnStrokeCompleted;

        // Manual user edits - selection of alternate interpretations and manual deletions
        // Handle selection of alternate interpretations
        dataGridViewAlternates.RowStateChanged += DataGridViewAlternates_RowStateChanged;
        // Handle manual symbol deletion and updste
        buttonDelete.Click += ButtonDelete_Click;
        buttonUpdate.Click += ButtonUpdate_Click;

        // Clear any previous STP state  - all symbols are deleted and STP is returned to a clean state
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"Resetting STP";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
        await _stpRecognizer.ResetStpScenarioAsync();

        return true;
    }

    /// <summary>
    /// STP has recognized a new symbol
    /// </summary>
    /// <param name="poid">Unique identifier</param>
    /// <param name="stpItem">Recognized item</param>
    /// <param name="isUndo">True if this event represents a compensating action to undo a symbol delete</param>
    private void StpRecognizer_OnSymbolAdded(string poid, StpItem stpItem, bool isUndo)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"SYMBOL ADDED:\t{stpItem.Poid}\t{stpItem.FullDescription}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);

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
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"SYMBOL MODIFIED:\t{stpItem.Poid}\t{stpItem.FullDescription}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);

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
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"SYMBOL DELETED:\t{poid}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);

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
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"EDIT OPERATION:\t{operation}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
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
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"MAP OPERATION:\t{operation}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
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
            // Show just top 5 to avoid best being hidden by scroll
            int max = speechList.Count > 5 ? 5 : speechList.Count;
            string concat = string.Join(" | ", speechList.GetRange(0, max));
            ShowSpeechReco(concat);
        }
    }

    /// <summary>
    /// Audio capture has been turned on/off - signal to the user that the mike is open or not
    /// </summary>
    /// <param name="isListening"></param>
    private void StpRecognizer_OnListeningStateChanged(bool isListening)
    {
        if (this.InvokeRequired)
        {   // recurse on GUI thread if necessary
            this.Invoke(new MethodInvoker(() => StpRecognizer_OnListeningStateChanged(isListening)));
            return;
        }
        // Change the color of the speech text box to green while on
        panelAudioCapture.BackColor = isListening ? Color.Green : SystemColors.Control;
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
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"TASK MODIFIED:\t{stpTask.Poid}\t{stpTask.FullDescription}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
        _currentTask = stpTask;
        DisplayTask(_currentTask);
    }

    private void StpRecognizer_OnTaskDeleted(string poid, bool isUndo)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        string msg = $"Task DELETED:\t{poid}";
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg);
        _currentTask = null;
        DisplayTask(_currentTask);
    }

    /// <summary>
    /// Connection error notification
    /// </summary>
    /// <param name="sce"></param>
    private void StpRecognizer_OnConnectionError(string msg, bool isStpActive, StpCommunicationException sce)
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

    #region User symbol edit commands
    /// <summary>
    /// Alternate selection from "n-best" list
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void DataGridViewAlternates_RowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
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
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    _stpRecognizer.ConfirmTaskAsync(item.Poid, item.Order);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
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
        _stpRecognizer.SendPenDown(geoPoint, DateTime.UtcNow);
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
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, stpItem.Type.ToUpper());
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

    private void BtnClearLog_Click_1(object sender, EventArgs e)
    {
        if (MessageBox.Show("Remove all symbols from STP?", "Clear Confirmation", MessageBoxButtons.OKCancel) == DialogResult.OK)
        {
            ResetScenario();
        }
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

    #region State setting methods 
    /// <summary>
    /// Clear STP's scenario, removing all symbols, and update log and map 
    /// </summary>
    private void ResetScenario()
    {
        // Reset STP scenario - all symbols are deleted and STP is returned to a clean state
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        _stpRecognizer.ResetStpScenarioAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        // Clear log window
        textBoxLog.Clear();

        // Clear the map display
        _mapHandler.ClearMap();

        // Clear any previous STP state
        _currentSymbols = new();

    }
    #endregion
}

