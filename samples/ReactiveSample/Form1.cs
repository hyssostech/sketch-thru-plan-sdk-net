using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog.Filters;
using StpSDK;
using StpSDK.Mapping;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace StpSDKSample;
public partial class Form1 : Form
{
    #region Private Properties and constants
    private static ILogger _logger;
    private static AppParams _appParams;

    private static StpRecognizer _stpRecognizer;
    private Mapping _mapHandler;

    BindingList<StpSymbol> _currentSymbols;
    BindingList<StpItem> _selectedSymbolAlternates;

    SymbolService _symbolService;
    TaskService _taskService;
    TaskOrgService _toService;

    private const int TimeOutSec = 120;
    #endregion

    #region Private observable properties
    private ObservableObj<Func<StpSymbol, bool>> _affiliationFilter;
    private NotifyingObj<SymbolVM> _selectedSymbol;
    ReadOnlyObservableCollection<StpNode<StpItem>> _taskNodesBinding;
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
        Designator.DataPropertyName = "DesignatorDescription";
        SymbolID.DataPropertyName = "SymbolID";
        Affiliation.DataPropertyName = "Affiliation";
        //ID.DataPropertyName = "Poid";

        // Create an observable object associated with grid selections
        _selectedSymbol = new();
        dataGridViewSymbolItems.SelectionChanged += (sender, e) => 
            _selectedSymbol.Value = ViewModel.Create(dataGridViewSymbolItems.CurrentRow?.DataBoundItem as StpSymbol) as SymbolVM;

        // Load initial/blank Alternates datagridview and associate the data property names with StpItem fields
        _selectedSymbolAlternates = new BindingList<StpItem>();
        dataGridViewAlternates.AutoGenerateColumns = false;
        dataGridViewAlternates.DataSource = _selectedSymbolAlternates;
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
        bool success;
        try
        {
            // Create an STP connection object - using STP's native pub/sub system via TCP or WebSockets
            IStpConnector stpConnector = new StpOaaConnector(_logger, toolStripTextBoxStpUri.Text);

            // Initialize the STP recognizer with the connector definition
            _stpRecognizer = new StpRecognizer(stpConnector);

            _affiliationFilter = new();
            SetAffiliationFilter();

            // Subscribe to services  _before_ connecting to STP, so that the correct message subscriptions can be identified
            _symbolService = _stpRecognizer.CreateSymbolService();
            _symbolService.All.Connect()
                // Log the updates
                //.ForEachChange(change => ShowStpMessage(
                //    $"SymbolService.Items {change.Reason}: {((StpSymbol)change.Current).FullDescription}"))
                // Convert to StpSymbol and bind to list that feeds the UI controls
                .Filter(_affiliationFilter.Observable)
                .ObserveOn(SynchronizationContext.Current)
                .Bind<StpSymbol, string>(_currentSymbols)
                // Dispose items that are removed as symbols are deleted and subscribe to the feed
                .DisposeMany()
                .Subscribe();

            // Associate selected symbol grid item with the propertygrid control
            propertyGridResult.DataBindings.Add("SelectedObject", _selectedSymbol, "Value");

            // Associate selected symbol grid item's Alternate with the alternate item grid
            _selectedSymbol.WhenValueChanged(s => s.Value)
                .Select(s => s?.Alternates)
                .Subscribe(al =>
                {
                    _selectedSymbolAlternates.Clear();
                    if (al != null)
                    {
                        _selectedSymbolAlternates.Add(al);
                    }
                });

            //_symbolService.Units.Connect()
            //    .ForEachChange(change => ShowStpMessage(
            //        $"SymbolService.Units {change.Reason}: {((StpSymbol)change.Current).FullDescription}"))
            //    //.ObserveOn(SynchronizationContext.Current)
            //    //.Bind(_allUnitsBinding)
            //    .DisposeMany()
            //    .Subscribe();

            _taskService = _stpRecognizer.CreateTaskService(_symbolService);
            _taskService.Nodes.Connect()
                .ForEachChange(change => ShowStpMessage(
                    $"TaskService.Nodes {change.Reason}: {change.Current.Item.FullDescription} {change.Current.Key} Parent={change.Current.ParentKey} Children={change.Current.ChildrenCount}"))
                .ObserveOn(SynchronizationContext.Current)
                .Bind(out _taskNodesBinding)
                .DisposeMany()
                .Subscribe();

            //// https://web.archive.org/web/20210306225409/http://blog.clauskonrad.net/2011/04/how-to-make-hierarchical-treeview.html
            //_taskService.Tree.Connect()
            //    .ForEachChange(change => ShowStpMessage(
            //        $"TaskService.Tree {change.Reason}: {((StpItem)change.Current.Item.Item).FullDescription} {change.Current.Item.Item.Poid}  has {change.Current.Children?.Count ?? 0} Task(s)"))
            //    //.ObserveOn(SynchronizationContext.Current)
            //    //.Bind(out _taskTreeBinding)
            //    .DisposeMany()
            //    .Subscribe(
            //        ok => Console.WriteLine("ok"),
            //        ex => Console.WriteLine(ex.ToString())
            //     );

            _toService = _stpRecognizer.CreateTaskOrgService();
            _toService.Nodes.Connect()
                .ForEachChange(change => ShowStpMessage(
                $"TOService.Nodes {change.Reason}: {change.Current.Description} {change.Current.DesignatorDescription} [{change.Current.Poid}] parent {change.Current.ParentUnit}"))
                //$"TOService.Nodes {change.Reason}: {change.Current.Item.Description} {change.Current.Item.DesignatorDescription} [{change.Current.Key}] has {change.Current.Children.Count} sub-unit(s)"))
                //.ObserveOn(SynchronizationContext.Current)
                //.Bind(out _toTreeBinding)
                .DisposeMany()
                .Subscribe();

            _toService.Tree.Connect()
                .ForEachChange(change => ShowStpMessage(
                $"TOService.Tree {change.Reason}: {((StpItem)change.Current.Item).Description} [{change.Current.Key}] has {change.Current.Children.Count} sub-unit(s)"))
                //.ObserveOn(SynchronizationContext.Current)
                //.Bind(out _toTreeBinding)
                .DisposeMany()
                .Subscribe();
            //_toService.Relationships.Connect()
            //    .ForEachChange(change => ShowStpMessage(
            //        $"TOService.Relationships {change.Reason}: {change.Current.Parent}->{change.Current.Child}"))
            //    //.ObserveOn(SynchronizationContext.Current)
            //    //.Bind(out _toTreeBinding)
            //    .DisposeMany()
            //    .Subscribe();

            // Subscribe to the observables _before_ connecting, so that the correct message subscriptions can be identified
            // Edit operations, including map commands
            _stpRecognizer.WhenSymbolEdit
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(args =>
                {
                    ShowStpMessage("---------------------------------\n" +
                        $"EDIT OPERATION:\t{args.Operation}\n");
                });
            _stpRecognizer.WhenMapOperation
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(args =>
                {
                    ShowStpMessage("---------------------------------\n" +
                        $"MAP OPERATION:\t{args.Operation}\n");
                });


            // Speech recognition and ink feedback
            _stpRecognizer.WhenSpeechRecognized
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(args =>
                {
                    var topReco = args.SpeechList
                        .Take(Math.Min(args.SpeechList.Count, 5))
                        .ToList();
                    string concat = string.Join(" | ", topReco);
                    if (args.SpeechList.Count > 5)
                        concat += "...";
                    ShowSpeechReco(concat);
                });
            _stpRecognizer.WhenListeningStateChanged
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(args =>
                {
                    // Set the color of the speech text box to green while on
                    panelAudioCapture.BackColor = args.isListening ? Color.Green : SystemColors.Control;
                });
            _stpRecognizer.WhenSketchRecognized
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(args =>
                {
                    // Set color
                    _mapHandler.MarkInkAsProcessed();
                });
            _stpRecognizer.WhenSketchIntegrated
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(args =>
                {
                    // Remove ink
                    _mapHandler.ClearInk();
                });

            // Message from STP to be conveyed to user
            _stpRecognizer.WhenStpMessage
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(args =>
                {
                    ShowStpMessage(args.Msg);
                });

            // Connection error notification
            _stpRecognizer.WhenConnectionError
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(args =>
                {
                    MessageBox.Show("Connection to STP was lost. Verify that the service is running and restart this app", "Connection Lost", MessageBoxButtons.OK);
                    //Application.Exit();
                });

            // STP is being shutdown 
            _stpRecognizer.WhenShutingdown
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(args =>
                {
                    MessageBox.Show("STP is shutting down. Terminating application", "Shutting down", MessageBoxButtons.OK);
                    Application.Exit();
                });

            // Attempt to connect
            ShowStpMessage("---------------------------------");
            ShowStpMessage("Connecting...");
            success = _stpRecognizer.ConnectAndRegister("ReactiveSample");
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

        // Hook up to the map handler
        _mapHandler = new Mapping(_logger,
            pictureMap,
            _appParams.MapImagePath,
            new LatLon(_appParams.MapTopLat, _appParams.MapLeftLon),
            new LatLon(_appParams.MapBottomLat, _appParams.MapRightLon));
        // Map symbols will be fed from the current symbols source
        _mapHandler.DataSource = _currentSymbols;
        // Rx-style event subscriptions of map events
        _mapHandler.WhenPenDown
            .Subscribe((args) => MapHandler_OnPenDown(null, args.EventArgs));
        _mapHandler.WhenStrokeCompleted
            .Subscribe((args) => MapHandler_OnStrokeCompleted(null, args.EventArgs));

        // Manual user edits - selection of alternate interpretations and manual deletions
        // Handle selection of alternate interpretations
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
        // Could just Set() to the same comprehensive predicate representing a conjunction of the 
        // ones below. That would be sufficient to cause a new Observable to be emitted
        if (checkBoxFriendly.Checked && checkBoxHostile.Checked)
        {
            _affiliationFilter.Value = s => s.Affiliation == StpSDK.Affiliation.friend ||
                s.Affiliation == StpSDK.Affiliation.hostile;
        }
        else if (checkBoxFriendly.Checked)
        {
            _affiliationFilter.Value = s => s.Affiliation == StpSDK.Affiliation.friend;
        }
        else if (checkBoxHostile.Checked)
        {
            _affiliationFilter.Value = s => s.Affiliation == StpSDK.Affiliation.hostile;
        }
        else
        {
            _affiliationFilter.Value = s => s.Affiliation != StpSDK.Affiliation.friend &&
                s.Affiliation != StpSDK.Affiliation.hostile;
        }
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
        List<string> intersectedPoids = _mapHandler.IntesectedSymbols();

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
    /// Show a message in the log window - invoker needs to be calling/observing on the UI thread
    /// </summary>
    /// <param name="msg"></param>
    private void ShowStpMessage(string msg)
    {
        // Invoke is kept in here because not all invocations are done via ObserveOn(SynchronizationContext.Current),
        // and may therefore originate in a non-UI thread
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
    /// Handle scenario join button
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
            string name = $"StpSDKSample{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")}";
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
    /// <summary>
    /// Wraps object into INotifyPropertyChange context that trigger when the object value changes 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class NotifyingObj<T> : AbstractNotifyPropertyChanged
    {
        private T _object;

        /// <summary>
        /// Current value - when set, it will cause  WhenValueChanged to trigger and a new observable to be emitted
        /// </summary>
        public T Value
        {
            get => _object;
            set => SetAndRaise(ref _object, value);
        }
    }

    /// <summary>
    /// Makes object into IObservable that emits when the object value changes 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    private class ObservableObj<T> : AbstractNotifyPropertyChanged
    {
        private T _object;
        private IObservable<T> _observable;

        /// <summary>
        /// Current value - when set, it will cause  WhenValueChanged to trigger and a new observable to be emitted
        /// </summary>
        public T Value
        {
            get => _object;
            set => SetAndRaise(ref _object, value);
        }

        /// <summary>
        /// Observable wrapping the Value - use this to cause reevaluation
        /// </summary>
        public IObservable<T> Observable => _observable;

        /// <summary>
        /// Constructor
        /// </summary>
        public ObservableObj(T defaultValue=default(T))
        {
            // WhenValueChanged will wrap the object into an IObservable
            _observable = this.WhenValueChanged(@this => @this.Value)
                .Select(o => o ?? defaultValue);
        }
    }
    #endregion
}

