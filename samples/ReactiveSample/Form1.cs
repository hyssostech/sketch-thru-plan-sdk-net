using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StpSDK;
using StpSDK.Mapping;
using System.ComponentModel;
using System.Reactive.Linq;

namespace StpSDKSample;
public partial class Form1 : Form
{
    #region Private Properties and constants
    private static ILogger _logger;
    private static AppParams _appParams;

    private static StpRecognizer _stpRecognizer;
    private Mapping _mapHandler;

    BindingList<StpSymbol> _currentSymbols;
    SymbolService _symbolService;
    TaskService _taskService;

    private const int TimeOutSec = 120;
    #endregion

    #region Private observable properties
    private class ObservableObject<T> : AbstractNotifyPropertyChanged
    {
        private T _value;
        public T Value
        { 
            get => _value; 
            set => SetAndRaise(ref _value, value); 
        }
    }
    private ObservableObject<SymbolVM> _selectedSymbol;

    private class ObservableFilter<T> : AbstractNotifyPropertyChanged
    {
        private Func<T, bool> _filter;
        private IObservable<Func<T, bool>> _dynamicFilter;
        /// <summary>
        /// Predicate to use as filter
        /// </summary>
        public Func<T, bool> Filter
        {
            private get => _filter;
            set => SetAndRaise(ref _filter, value);
        }
        /// <summary>
        /// The observable predicate to use in a Filter() expression
        /// </summary>
        /// <remarks>
        /// Whenever the value of the Filter expression changes, this will cause
        /// the Filter() expression to be evaluated and list elements to be filtered accordingly
        /// </remarks>
        public IObservable<Func<T, bool>> DynamicFilter => _dynamicFilter;

        /// <summary>
        /// Constructor
        /// </summary>
        public ObservableFilter()
        {
            // WhenValueChanged will wrap the object into an IObservable
            _dynamicFilter = this.WhenValueChanged(@this => @this.Filter)
                .Select(f => f ?? (t => true)); // No filtering if expression is null/clear
        }
    }
    private ObservableFilter<StpSymbol> _affiliationFilter;
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

        // Load initial/blank Symbols datagridview and associate the data property names with StpSymbol fields
        _currentSymbols = new BindingList<StpSymbol>();
        dataGridViewSymbolItems.AutoGenerateColumns = false;
        dataGridViewSymbolItems.DataSource = _currentSymbols;
        Type.DataPropertyName = "Type";
        Description.DataPropertyName = "Description";
        SymbolID.DataPropertyName = "SymbolID";
        Affiliation.DataPropertyName = "Affiliation";
        ID.DataPropertyName = "Poid";

        // Create an observable object associated with grid selections
        _selectedSymbol = new();
        dataGridViewSymbolItems.SelectionChanged += (sender, e) =>
        {
            if (dataGridViewSymbolItems.CurrentRow != null)
            {
                _selectedSymbol.Value = ViewModel.Create(dataGridViewSymbolItems.CurrentRow.DataBoundItem as StpSymbol) as SymbolVM;
            }
            //else
            //{
            //    // Clear the controls that depend on the selected symbol
            //}
        };

        // Associate selected item with the propertygrid control
        propertyGridResult.DataBindings.Add("SelectedObject", _selectedSymbol, "Value");

        // Load initial/blank Alternates datagridview and associate the data property names with StpItem fields
        dataGridViewAlternates.AutoGenerateColumns = false;
        dataGridViewAlternates.DataSource = _selectedSymbol;
        dataGridViewAlternates.DataMember = "Value.Alternates";
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
        if (!await Connect())
        {
            MessageBox.Show("Failed to connect to STP. Please make sure it is running and try again", "Could not connect to  agents");
            //Application.Exit();
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
        // Create an STP connection object - using STP's native pub/sub system
        var stpConnector = new StpOaaConnector(_logger, _appParams.StpHost, _appParams.StpPort);

        // Initialize the STP recognizer with the connector definition
        _stpRecognizer = new StpRecognizer(stpConnector);

        _affiliationFilter = new();
        SetAffiliationFilter();

        // Subscribe to services  _before_ connecting to STP, so that the correct message subscriptions can be identified
        _symbolService = _stpRecognizer.CreateSymbolService();
        _symbolService.Items.Connect()
            // Log the updates
            .ForEachChange(change => StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info,
                $"SymbolService.Items {change.Reason}: {((StpSymbol)change.Current).FullDescription}"))
            // Convert to StpSymbol and bind to list that feeds the UI controls
            .Cast<StpItem, string, StpSymbol>(t => (StpSymbol)t)
            .Filter(_affiliationFilter.DynamicFilter)
            .ObserveOn(SynchronizationContext.Current)
            .Bind<StpSymbol, string>(_currentSymbols)
            // Dispose items that are removed as symbols are deleted and subscribe to the feed
            .DisposeMany()
            .Subscribe();

        //_symbolService.Units.Connect()
        //    .ForEachChange(change => StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info,
        //        $"SymbolService.Units {change.Reason}: {((StpSymbol)change.Current).FullDescription}"))
        //    //.ObserveOn(SynchronizationContext.Current)
        //    //.Bind(_allUnitsBinding)
        //    .DisposeMany()
        //    .Subscribe();

        _taskService = _stpRecognizer.CreateTaskService(_symbolService);
        // https://web.archive.org/web/20210306225409/http://blog.clauskonrad.net/2011/04/how-to-make-hierarchical-treeview.html
        _taskService.TaskTree.Connect()
            .ForEachChange(change => StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info,
                $"TaskService.TaskTree {change.Reason}: {((StpTask)change.Current).Description} has {((StpTask)change.Current).Alternates?.Count ?? 0} Task(s)"))
            //.ObserveOn(SynchronizationContext.Current)
            //.Bind(out _taskTreeBinding)
            .DisposeMany()
            .Subscribe();

        //_taskService.Items.Connect()
        //    .ForEachChange(change => StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info,
        //        $"TaskService.Items {change.Reason}: {((StpTask)change.Current).FullDescription}"))
        //    //.ObserveOn(SynchronizationContext.Current)
        //    //.Bind(out _allTasksBinding)
        //    .DisposeMany()
        //    .Subscribe();

        // Hook up to the events _before_ connecting, so that the correct message subscriptions can be identified
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
        bool success;
        try
        {
            success = _stpRecognizer.ConnectAndRegister("ReactiveSample");
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
        _mapHandler.DataSource = _currentSymbols;
        _mapHandler.OnPenDown += MapHandler_OnPenDown;
        _mapHandler.OnStrokeCompleted += MapHandler_OnStrokeCompleted;

        // Manual user edits - selection of alternate interpretations and manual deletions
        //// Handle selection of alternate interpretations
        dataGridViewAlternates.RowStateChanged += DataGridViewAlternates_RowStateChanged;
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
    /// Set symbol filtering based on selected affiliations
    /// </summary>
    private void SetAffiliationFilter()
    {
        if (checkBoxAffiliationAny.Checked)
        {
            _affiliationFilter.Filter = s => true;
        }
        else if (checkBoxFriendly.Checked && checkBoxHostile.Checked)
        {
            _affiliationFilter.Filter = s => s.Affiliation == StpSDK.Affiliation.friend ||
                s.Affiliation == StpSDK.Affiliation.hostile;
        }
        else if (checkBoxFriendly.Checked)
        {
            _affiliationFilter.Filter = s => s.Affiliation == StpSDK.Affiliation.friend;
        }
        else if (checkBoxHostile.Checked)
        {
            _affiliationFilter.Filter = s => s.Affiliation == StpSDK.Affiliation.hostile;
        }
        else
        {
            _affiliationFilter.Filter = s => false;
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
                    _stpRecognizer.ConfirmTask(item.Poid, item.Order);
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
    /// Manual updates
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
        List<string> intersectedPoids = _mapHandler.IntesectedSymbols(_currentSymbols.ToList());

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

    private void CheckBoxAffiliationFilter_CheckedChanged(object sender, EventArgs e)
    {
        SetAffiliationFilter();
    }


    private void ClearButtons()
    {
        plaBtn.Checked = drawBtn.Checked = false;
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
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
            string name = $"StpSDKSample{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")}";
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Creating new scenario: {name}");

            // Launch operation
            CancellationTokenSource cts = new();
            cts.CancelAfter(TimeSpan.FromSeconds(TimeOutSec));
            await _stpRecognizer.CreateNewScenarioAsync(name, cts.Token);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
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
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Joining scenario");

            // Launch operation
            CancellationTokenSource cts = new();
            cts.CancelAfter(TimeSpan.FromSeconds(TimeOutSec));
            await _stpRecognizer.JoinScenarioSessionAsync(cts.Token);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
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
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Saving scenario to {filePath}");

            // Get the current contents
            string content = await _stpRecognizer.GetScenarioContentAsync();

            // Save to file
            await File.WriteAllTextAsync(filePath, content);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
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
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Loading new scenario from {filePath}");

            // Load the file contents
            string content = File.ReadAllText(filePath).Replace("\n", string.Empty).Replace("\r", string.Empty);

            // Launch operation
            CancellationTokenSource cts = new();
            cts.CancelAfter(TimeSpan.FromSeconds(TimeOutSec));
            await _stpRecognizer.LoadNewScenarioAsync(content, cts.Token);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        });
    }
    #endregion

    #region Utility
    /// <summary>
    /// Clear all local caches
    /// </summary>
    private void ClearCaches()
    {
        _currentSymbols.Clear();
        _selectedSymbol.Value = null;
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
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "Operation timed out");
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Operation failed: {ex}");
            MessageBox.Show($"Operation failed: {ex.Message}", "Error performing scenario operation", MessageBoxButtons.OK);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Operation failed: {ex.Message}");
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
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

