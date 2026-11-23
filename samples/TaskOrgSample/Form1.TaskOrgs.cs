using StpSDK;

namespace StpSDKSample;

/// <summary>
/// The scenario's task orgs: list them, make one the default for its affiliation or clear the default,
/// and export / import a task org as an object set with a load timeout.
/// </summary>
public partial class Form1
{
    #region Task org panel state
    private ToolPanel _taskOrgPanel;
    private ListView _listTaskOrgs;
    private NumericUpDown _numericLoadTimeout;
    // The last task org exported as an object set
    private ObjectSet _lastTaskOrgObjectSet;
    #endregion

    #region Task org panel
    /// <summary>
    /// Build the task org panel, and a toolbar button that shows it
    /// </summary>
    private void ShowTaskOrgPanel()
    {
        if (_taskOrgPanel == null)
        {
            _listTaskOrgs = ToolPanel.NewList(("Name", 160), ("Affiliation", 80), ("Poid", 200));
            _taskOrgPanel = new ToolPanel(this, "Task orgs", new Size(520, 300), _listTaskOrgs);
            _taskOrgPanel.AddButton("Refresh", async (s, e) => await PerformLongOp(RefreshTaskOrgsAsync));
            _taskOrgPanel.AddButton("Make default", ButtonTaskOrgDefault_Click);
            _taskOrgPanel.AddButton("Clear friend default", async (s, e) => await ResetDefaultAsync("friend"));
            _taskOrgPanel.AddButton("Clear hostile default", async (s, e) => await ResetDefaultAsync("hostile"));
            _taskOrgPanel.AddButton("Export object set", ButtonTaskOrgExport_Click);
            _taskOrgPanel.AddButton("Import object set", ButtonTaskOrgImport_Click);
            _taskOrgPanel.AddLabel("Load timeout (s)");
            _numericLoadTimeout = _taskOrgPanel.AddControl(new NumericUpDown { Width = 60, Minimum = 0, Maximum = 600, Value = 30 });

            toolStrip1.Items.Add(new ToolStripButton("Task orgs", null, (s, e) => _taskOrgPanel.Show()));
        }
        _taskOrgPanel.Show();
    }

    /// <summary>
    /// List the scenario's task orgs (GetScenarioTaskOrgList)
    /// </summary>
    private async Task RefreshTaskOrgsAsync()
    {
        List<StpTaskOrg> taskOrgs = await _stpRecognizer.GetScenarioTaskOrgsAsync();
        _taskOrgPanel.OnUi(() =>
        {
            _listTaskOrgs.Items.Clear();
            foreach (var to in taskOrgs)
            {
                var item = new ListViewItem(to.Name) { Tag = to.Poid };
                item.SubItems.Add(to.Affiliation);
                item.SubItems.Add(to.Poid);
                _listTaskOrgs.Items.Add(item);
            }
        });
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Scenario has {taskOrgs.Count} task org(s)");
    }

    private string SelectedTaskOrgPoid()
    {
        string poid = _listTaskOrgs.SelectedItems.Count > 0 ? (string)_listTaskOrgs.SelectedItems[0].Tag : null;
        if (poid == null)
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, "Select a task org in the list first (Refresh lists them)");
        }
        return poid;
    }
    #endregion

    #region Task org panel buttons
    private async void ButtonTaskOrgDefault_Click(object sender, EventArgs e)
    {
        string poid = SelectedTaskOrgPoid();
        if (poid == null) return;
        await PerformLongOp(async () =>
        {
            await _stpRecognizer.SetDefaultTaskOrgAsync(poid);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Task org {poid} is the default for its affiliation");
        });
    }

    /// <summary>
    /// Unset the default task org of an affiliation. The task org itself is kept; TaskOrgSwitched follows
    /// </summary>
    private async Task ResetDefaultAsync(string affiliation)
    {
        await PerformLongOp(async () =>
        {
            await _stpRecognizer.ResetDefaultTaskOrgAsync(affiliation, CancellationToken.None);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Default {affiliation} task org cleared");
        });
    }

    /// <summary>
    /// Export a task org - the task org with its units and relationships - as an object set
    /// </summary>
    private async void ButtonTaskOrgExport_Click(object sender, EventArgs e)
    {
        string poid = SelectedTaskOrgPoid();
        if (poid == null) return;
        await PerformLongOp(async () =>
        {
            _lastTaskOrgObjectSet = await _stpRecognizer.GetTaskOrgObjectSetAsync(poid);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Task org {poid} exported as an object set ({_lastTaskOrgObjectSet.Count} objects)");
        });
    }

    /// <summary>
    /// Import the last exported task org object set with a load timeout. A load the engine stops, or cuts
    /// off at the timeout, throws StpObjectSetLoadException, whose Report is the engine's account of
    /// which objects were applied, which was in flight and which were not sent
    /// </summary>
    private async void ButtonTaskOrgImport_Click(object sender, EventArgs e)
    {
        if (_lastTaskOrgObjectSet == null)
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, "Export a task org as an object set first");
            return;
        }
        var timeout = TimeSpan.FromSeconds((double)_numericLoadTimeout.Value);
        await PerformLongOp(async () =>
        {
            try
            {
                string poid = await _stpRecognizer.ImportTaskOrgAsync(_lastTaskOrgObjectSet, timeout);
                StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Task org {poid} imported from the object set");
            }
            catch (StpObjectSetLoadException ex)
            {
                StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, $"{ex.Method} did not complete. Engine report:");
                StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, ex.Report);
            }
        });
    }
    #endregion
}
