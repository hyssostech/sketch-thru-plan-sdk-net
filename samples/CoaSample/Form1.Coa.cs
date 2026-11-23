using Newtonsoft.Json;
using StpSDK;

namespace StpSDKSample;

/// <summary>
/// COA (Course of Action) handling: create, rename, delete and switch COAs, follow the COA events,
/// tie a task org to a COA, and export/import a COA as text or as an object set.
/// </summary>
public partial class Form1
{
    #region COA state
    // The scenario's COAs, by poid, as the COA events report them
    private readonly Dictionary<string, StpCoa> _coas = new();
    // The COA the current role works on, if known
    private string _currentCoaPoid;
    // The last exported COA, as text and as an object set, ready to be imported back
    private string _lastCoaContent;
    private ObjectSet _lastCoaObjectSet;

    private ToolPanel _coaPanel;
    private ListView _listCoas;
    private TextBox _textCoaName;
    private ComboBox _comboCoaAffiliation;
    private NumericUpDown _numericLoadTimeout;
    #endregion

    #region COA event handlers
    /// <summary>
    /// Subscribe to the COA events. Called before connecting, like every other subscription.
    /// </summary>
    /// <remarks>
    /// The Ext variants carry the trailing batchId: the change set (BatchStarted ... BatchEnded) the
    /// COA arrived in, or null outside a change set and from engines that predate change sets.
    /// </remarks>
    private void SubscribeCoaEvents()
    {
        _stpRecognizer.OnCoaAddedExt += StpRecognizer_OnCoaAdded;
        _stpRecognizer.OnCoaModifiedExt += StpRecognizer_OnCoaModified;
        _stpRecognizer.OnCoaDeletedExt += StpRecognizer_OnCoaDeleted;
        _stpRecognizer.OnCoaSwitched += StpRecognizer_OnCoaSwitched;
    }

    private void StpRecognizer_OnCoaAdded(string poid, StpCoa coa, bool isUndo, string batchId)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"COA ADDED:\t{poid}\t{coa?.Name}\t{coa?.Affiliation}{BatchNote(batchId)}");
        lock (_coas) { _coas[poid] = coa; }
        RefreshCoaList();
    }

    private void StpRecognizer_OnCoaModified(string poid, StpCoa coa, bool isUndo, string batchId)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"COA MODIFIED:\t{poid}\t{coa?.Name}{BatchNote(batchId)}");
        lock (_coas) { _coas[poid] = coa; }
        RefreshCoaList();
    }

    private void StpRecognizer_OnCoaDeleted(string poid, bool isUndo, string batchId)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"COA DELETED:\t{poid}{BatchNote(batchId)}");
        lock (_coas) { _coas.Remove(poid); }
        if (_currentCoaPoid == poid) _currentCoaPoid = null;
        RefreshCoaList();
    }

    /// <summary>
    /// The current COA changed - by this client (SetCoaAsync) or because the role changed.
    /// A COA with its own task org brings that task org in: TaskOrgSwitched follows.
    /// </summary>
    private void StpRecognizer_OnCoaSwitched(StpCoa coa)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"COA SWITCHED:\t{coa?.Poid ?? "(none)"}\t{coa?.Name}");
        _currentCoaPoid = coa?.Poid;
        RefreshCoaList();
    }

    private static string BatchNote(string batchId) => batchId == null ? "" : $"\t(change set {batchId})";
    #endregion

    #region COA panel
    /// <summary>
    /// Build the COA panel and a toolbar button that shows it
    /// </summary>
    private void ShowCoaPanel()
    {
        if (_coaPanel == null)
        {
            _listCoas = ToolPanel.NewList(("", 20), ("Name", 150), ("Affiliation", 70), ("Creator role", 80), ("Poid", 200));
            _coaPanel = new ToolPanel(this, "COAs", new Size(640, 360), _listCoas);

            _coaPanel.AddLabel("Name");
            _textCoaName = _coaPanel.AddControl(new TextBox { Width = 120 });
            _comboCoaAffiliation = _coaPanel.AddControl(new ComboBox { Width = 70, DropDownStyle = ComboBoxStyle.DropDownList });
            _comboCoaAffiliation.Items.AddRange(new object[] { "friend", "hostile" });
            _comboCoaAffiliation.SelectedIndex = 0;
            _coaPanel.AddButton("Add", ButtonCoaAdd_Click);
            _coaPanel.AddButton("Rename", ButtonCoaRename_Click);
            _coaPanel.AddButton("Delete", ButtonCoaDelete_Click);
            _coaPanel.AddButton("Make current", ButtonCoaSwitch_Click);
            _coaPanel.AddButton("Refresh", async (s, e) => await PerformLongOp(RefreshCoasAsync));
            _coaPanel.AddButton("Tie loaded TO", ButtonCoaTieTaskOrg_Click);
            _coaPanel.AddButton("Clear COA TO", ButtonCoaClearTaskOrg_Click);
            _coaPanel.AddButton("Export", ButtonCoaExport_Click);
            _coaPanel.AddButton("Import", ButtonCoaImport_Click);
            _coaPanel.AddButton("Export object set", ButtonCoaExportObjectSet_Click);
            _coaPanel.AddButton("Import object set", ButtonCoaImportObjectSet_Click);
            _coaPanel.AddLabel("Load timeout (s)");
            _numericLoadTimeout = _coaPanel.AddControl(new NumericUpDown { Width = 60, Minimum = 0, Maximum = 600, Value = 30 });

            toolStrip1.Items.Add(new ToolStripButton("COAs", null, (s, e) => _coaPanel.Show()));
        }
        _coaPanel.Show();
    }

    /// <summary>
    /// Show the COAs in the list, marking the current one
    /// </summary>
    private void RefreshCoaList()
    {
        _coaPanel?.OnUi(() =>
        {
            _listCoas.BeginUpdate();
            _listCoas.Items.Clear();
            List<StpCoa> coas;
            lock (_coas) { coas = _coas.Values.ToList(); }
            foreach (var coa in coas.OrderBy(c => c.Name))
            {
                var item = new ListViewItem(coa.Poid == _currentCoaPoid ? "*" : "") { Tag = coa.Poid };
                item.SubItems.Add(coa.Name);
                item.SubItems.Add(coa.Affiliation);
                item.SubItems.Add(coa.CreatorRole);
                item.SubItems.Add(coa.Poid);
                _listCoas.Items.Add(item);
            }
            _listCoas.EndUpdate();
        });
    }

    /// <summary>
    /// The poid of the COA selected in the list, or null (with a message) when none is
    /// </summary>
    private string SelectedCoaPoid()
    {
        string poid = null;
        _coaPanel.Invoke(() => poid = _listCoas.SelectedItems.Count > 0 ? (string)_listCoas.SelectedItems[0].Tag : null);
        if (poid == null)
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, "Select a COA in the list first");
        }
        return poid;
    }

    /// <summary>
    /// Re-read the scenario's COAs, for example after joining a session that already had some,
    /// and find which one the current role works on
    /// </summary>
    private async Task RefreshCoasAsync()
    {
        var coas = await _stpRecognizer.GetScenarioCoasAsync();
        lock (_coas)
        {
            _coas.Clear();
            foreach (var coa in coas) _coas[coa.Poid] = coa;
        }
        string role = null;
        Invoke(() => role = comboBoxRole.Text);
        if (!string.IsNullOrEmpty(role))
        {
            _currentCoaPoid = await _stpRecognizer.GetCurrentCoaPoidAsync(role);
        }
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Scenario has {coas.Count} COA(s)");
        RefreshCoaList();
    }
    #endregion

    #region COA buttons
    /// <summary>
    /// Add a COA. AddCoa takes the new COA's definition and is fire-and-forget: STP assigns the poid,
    /// and CoaAdded confirms it
    /// </summary>
    private void ButtonCoaAdd_Click(object sender, EventArgs e)
    {
        string name = _textCoaName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, "Enter a name for the new COA");
            return;
        }
        _stpRecognizer.AddCoa(new StpCoa
        {
            Name = name,
            Affiliation = _comboCoaAffiliation.Text,
            CreatorRole = string.IsNullOrEmpty(comboBoxRole.Text) ? StpRole.S3 : comboBoxRole.Text,
        });
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Requested new COA '{name}' - waiting for CoaAdded");
    }

    /// <summary>
    /// Rename a COA. UpdateCoa sends the whole definition; CoaModified confirms it
    /// </summary>
    private void ButtonCoaRename_Click(object sender, EventArgs e)
    {
        string poid = SelectedCoaPoid();
        string name = _textCoaName.Text.Trim();
        if (poid == null) return;
        if (string.IsNullOrEmpty(name))
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, "Enter the new name in the Name box");
            return;
        }
        StpCoa coa;
        lock (_coas) { coa = _coas[poid]; }
        // Copy the definition so the cached one only changes when CoaModified arrives.
        // NOTE: StpCoa carries the fields it models only; anything else on the engine's COA
        // definition (for example a task org tied with SetCoaTaskOrgAsync) is not sent back
        var updated = JsonConvert.DeserializeObject<StpCoa>(JsonConvert.SerializeObject(coa));
        updated.Name = name;
        _stpRecognizer.UpdateCoa(poid, updated);
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Requested rename of {poid} - waiting for CoaModified");
    }

    /// <summary>
    /// Delete a COA, with its symbols and tasks. CoaDeleted follows
    /// </summary>
    private async void ButtonCoaDelete_Click(object sender, EventArgs e)
    {
        string poid = SelectedCoaPoid();
        if (poid == null) return;
        if (DialogResult.Yes != MessageBox.Show($"Delete COA {poid}?", "Delete COA", MessageBoxButtons.YesNo)) return;
        await PerformLongOp(async () =>
        {
            await _stpRecognizer.DeleteCoaAsync(poid);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"COA {poid} deleted");
        });
    }

    /// <summary>
    /// Make a COA the current one: new symbols and tasks go into it. CoaSwitched follows, and
    /// TaskOrgSwitched when the COA brings its own task org
    /// </summary>
    private async void ButtonCoaSwitch_Click(object sender, EventArgs e)
    {
        string poid = SelectedCoaPoid();
        if (poid == null) return;
        await PerformLongOp(async () =>
        {
            await _stpRecognizer.SetCoaAsync(poid);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"COA {poid} is current");
        });
    }

    /// <summary>
    /// Tie the task org loaded with the Task Org buttons to the selected COA. While the COA is current
    /// it uses that task org instead of the default one for the task org's affiliation
    /// </summary>
    private async void ButtonCoaTieTaskOrg_Click(object sender, EventArgs e)
    {
        string poid = SelectedCoaPoid();
        if (poid == null) return;
        StpCoa coa;
        lock (_coas) { coa = _coas[poid]; }
        string toPoid = coa.Affiliation == "hostile" ? _toHostilePoid : _toFriendPoid;
        if (toPoid == null)
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning,
                $"Load a {coa.Affiliation} task org first, with the Task Org buttons");
            return;
        }
        await PerformLongOp(async () =>
        {
            // Engines before STP-1077 refuse a per-COA task org; the refusal is shown by PerformLongOp
            await _stpRecognizer.SetCoaTaskOrgAsync(toPoid, poid);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Task org {toPoid} tied to COA {poid} - make the COA current to switch to it");
        });
    }

    /// <summary>
    /// Clear the task org tied to the selected COA, for every affiliation. The COA falls back to the
    /// default task org; the task org itself is not deleted
    /// </summary>
    private async void ButtonCoaClearTaskOrg_Click(object sender, EventArgs e)
    {
        string poid = SelectedCoaPoid();
        if (poid == null) return;
        await PerformLongOp(async () =>
        {
            await _stpRecognizer.ResetCoaTaskOrgAsync(null, poid);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Task org selection of COA {poid} cleared");
        });
    }

    /// <summary>
    /// Export a COA - its definition with its units, graphics and tasks - as text. Task org units and
    /// relationships are not part of it
    /// </summary>
    private async void ButtonCoaExport_Click(object sender, EventArgs e)
    {
        string poid = SelectedCoaPoid();
        if (poid == null) return;
        await PerformLongOp(async () =>
        {
            _lastCoaContent = await _stpRecognizer.GetCoaContentAsync(poid);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info,
                $"COA {poid} exported ({_lastCoaContent?.Length ?? 0} characters) - delete it and Import to bring it back");
        });
    }

    /// <summary>
    /// Import the last exported COA. Poids are kept as they are, so this brings back the very COA that
    /// was exported, not a copy. CoaAdded follows
    /// </summary>
    private async void ButtonCoaImport_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_lastCoaContent))
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, "Export a COA first");
            return;
        }
        await PerformLongOp(async () =>
        {
            string poid = await _stpRecognizer.ImportCoaContentAsync(_lastCoaContent);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"COA {poid} imported");
        });
    }

    /// <summary>
    /// Export a COA as an object set: the same content as Export, as typed objects
    /// </summary>
    private async void ButtonCoaExportObjectSet_Click(object sender, EventArgs e)
    {
        string poid = SelectedCoaPoid();
        if (poid == null) return;
        await PerformLongOp(async () =>
        {
            _lastCoaObjectSet = await _stpRecognizer.GetCoaObjectSetAsync(poid);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info,
                $"COA {poid} exported as an object set ({_lastCoaObjectSet.Count} objects)");
        });
    }

    /// <summary>
    /// Import the last exported object set, giving the engine a load timeout. A load that the engine
    /// stops, or cuts off at the timeout, throws StpObjectSetLoadException: its Report says which
    /// objects were applied, which was in flight and which were not sent
    /// </summary>
    private async void ButtonCoaImportObjectSet_Click(object sender, EventArgs e)
    {
        if (_lastCoaObjectSet == null)
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, "Export a COA as an object set first");
            return;
        }
        var timeout = TimeSpan.FromSeconds((double)_numericLoadTimeout.Value);
        await PerformLongOp(async () =>
        {
            try
            {
                string poid = await _stpRecognizer.ImportCoaAsync(_lastCoaObjectSet, timeout);
                StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"COA {poid} imported from the object set");
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
