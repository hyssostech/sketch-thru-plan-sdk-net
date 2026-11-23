using StpSDK;

namespace StpSDKSample;

/// <summary>
/// Object sets and batches.
/// <list type="bullet">
/// <item>Object sets: the scenario as typed objects (GetScenarioObjectSetContentAsync), loaded back as a new
/// scenario, imported into the current one, or synchronized with it - each with a load timeout, and the
/// engine's report when a load is stopped or times out (StpObjectSetLoadException).</item>
/// <item>Batches: operations that touch many objects (a load, an import) are bracketed by BatchStarted and
/// BatchEnded, and every object event in between carries the batch's batchId.</item>
/// </list>
/// </summary>
public partial class Form1
{
    #region Object set and batch state
    // The last snapshot of the scenario, as an object set
    private ObjectSet _snapshot;

    // Batches seen, by batchId
    private class Batch
    {
        public string Kind;
        public int Expected;
        public bool IsOwn;
        public int Received;
        public string Status = "open";
        public int Informs;
    }
    private readonly Dictionary<string, Batch> _batches = new();

    private ToolPanel _objectSetPanel;
    private ListView _listBatches;
    private NumericUpDown _numericLoadTimeout;
    #endregion

    #region Batch events
    /// <summary>
    /// Subscribe to the batch events. Called before connecting, like every other subscription.
    /// </summary>
    /// <remarks>
    /// The Ext variants of the object events carry the trailing batchId. They are attached next to the plain
    /// events the sample already handles; the SDK subscribes to each engine event once, and the plain event
    /// fires first. Objects are handled as they arrive: an engine that predates batches sends no
    /// BatchStarted / BatchEnded, and a null batchId.
    /// </remarks>
    private void SubscribeBatchEvents()
    {
        _stpRecognizer.OnBatchStarted += StpRecognizer_OnBatchStarted;
        _stpRecognizer.OnBatchEnded += StpRecognizer_OnBatchEnded;

        _stpRecognizer.OnSymbolAddedExt += (poid, item, isUndo, batchId) => CountInBatch(batchId);
        _stpRecognizer.OnSymbolModifiedExt += (poid, item, isUndo, batchId) => CountInBatch(batchId);
        _stpRecognizer.OnSymbolDeletedExt += (poid, isUndo, batchId) => CountInBatch(batchId);
        _stpRecognizer.OnTaskAddedExt += (poid, task, tgPoids, isUndo, batchId) => CountInBatch(batchId);
        _stpRecognizer.OnTaskModifiedExt += (poid, task, tgPoids, isUndo, batchId) => CountInBatch(batchId);
        _stpRecognizer.OnTaskDeletedExt += (poid, isUndo, batchId) => CountInBatch(batchId);
        _stpRecognizer.OnTaskOrgAddedExt += (poid, to, isUndo, batchId) => CountInBatch(batchId);
        _stpRecognizer.OnTaskOrgUnitAddedExt += (poid, unit, isUndo, batchId) => CountInBatch(batchId);
        _stpRecognizer.OnTaskOrgRelationshipAddedExt += (poid, rel, isUndo, batchId) => CountInBatch(batchId);
        _stpRecognizer.OnCoaAddedExt += (poid, coa, isUndo, batchId) => CountInBatch(batchId);
        _stpRecognizer.OnCoaDeletedExt += (poid, isUndo, batchId) => CountInBatch(batchId);
    }

    /// <summary>
    /// A batch began. expected is -1 when the requester did not say; isOwn tells whether this
    /// client's request started it
    /// </summary>
    private void StpRecognizer_OnBatchStarted(string batchId, string kind, int expected, bool isOwn, string origin)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info,
            $"BATCH STARTED:\t{batchId}\t{kind}\texpected {(expected < 0 ? "?" : expected.ToString())}\t{(isOwn ? "own" : "from " + origin)}");
        lock (_batches)
        {
            _batches[batchId] = new Batch { Kind = kind, Expected = expected, IsOwn = isOwn };
        }
        RefreshBatchList();
    }

    /// <summary>
    /// A batch ended, after its last object event. status is completed, cancelled, failed, abandoned
    /// or expired. A client could defer redrawing until here; this sample just reports the totals
    /// </summary>
    private void StpRecognizer_OnBatchEnded(string batchId, string kind, string status, int informs, bool isOwn, string origin)
    {
        int received;
        lock (_batches)
        {
            if (!_batches.TryGetValue(batchId, out var cs))
            {
                cs = _batches[batchId] = new Batch { Kind = kind, Expected = -1, IsOwn = isOwn };
            }
            cs.Status = status;
            cs.Informs = informs;
            received = cs.Received;
        }
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info,
            $"BATCH ENDED:\t{batchId}\t{kind}\t{status}\t{received} object events received, {informs} sent");
        RefreshBatchList();
    }

    /// <summary>
    /// Count an object event against its batch; events outside a batch have a null batchId
    /// </summary>
    private void CountInBatch(string batchId)
    {
        if (batchId == null) return;
        lock (_batches)
        {
            if (!_batches.TryGetValue(batchId, out var cs))
            {
                // The object arrived before (or without) its BatchStarted - handle it all the same
                cs = _batches[batchId] = new Batch { Kind = "?", Expected = -1 };
            }
            cs.Received++;
        }
        RefreshBatchList();
    }
    #endregion

    #region Object set panel
    /// <summary>
    /// Build the object set / batch panel, and a toolbar button that shows it
    /// </summary>
    private void ShowObjectSetPanel()
    {
        if (_objectSetPanel == null)
        {
            _listBatches = ToolPanel.NewList(("Batch", 160), ("Kind", 110), ("Expected", 65), ("Received", 65), ("Status", 80), ("Own", 40));
            _objectSetPanel = new ToolPanel(this, "Object sets and batches", new Size(600, 320), _listBatches);
            _objectSetPanel.AddButton("Snapshot", ButtonSnapshot_Click);
            _objectSetPanel.AddButton("Load as new scenario", ButtonSnapshotLoad_Click);
            _objectSetPanel.AddButton("Import into scenario", ButtonSnapshotImport_Click);
            _objectSetPanel.AddButton("Sync session", ButtonSnapshotSync_Click);
            _objectSetPanel.AddLabel("Load timeout (s)");
            _numericLoadTimeout = _objectSetPanel.AddControl(new NumericUpDown { Width = 60, Minimum = 0, Maximum = 600, Value = 30 });

            toolStrip1.Items.Add(new ToolStripButton("Object sets", null, (s, e) => _objectSetPanel.Show()));
        }
        _objectSetPanel.Show();
    }

    private void RefreshBatchList()
    {
        _objectSetPanel?.OnUi(() =>
        {
            _listBatches.BeginUpdate();
            _listBatches.Items.Clear();
            List<KeyValuePair<string, Batch>> sets;
            lock (_batches) { sets = _batches.ToList(); }
            foreach (var (batchId, cs) in sets)
            {
                var item = new ListViewItem(batchId);
                item.SubItems.Add(cs.Kind);
                item.SubItems.Add(cs.Expected < 0 ? "?" : cs.Expected.ToString());
                item.SubItems.Add(cs.Received.ToString());
                item.SubItems.Add(cs.Status);
                item.SubItems.Add(cs.IsOwn ? "yes" : "");
                _listBatches.Items.Add(item);
            }
            _listBatches.EndUpdate();
        });
    }

    /// <summary>
    /// Run an object set load. A load that the engine stops, or cuts off at the timeout, throws
    /// StpObjectSetLoadException: its Report is the engine's account of which objects were applied,
    /// which one was in flight and which were not sent
    /// </summary>
    private async Task LoadObjectSet(string what, Func<ObjectSet, TimeSpan, Task> load)
    {
        if (_snapshot == null)
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, "Take a snapshot first");
            return;
        }
        var timeout = TimeSpan.FromSeconds((double)_numericLoadTimeout.Value);
        await PerformLongOp(async () =>
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, "---------------------------------");
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"{what}: {_snapshot.Count} objects, timeout {timeout.TotalSeconds}s");
            try
            {
                await load(_snapshot, timeout);
                StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"{what}: done");
            }
            catch (StpObjectSetLoadException ex)
            {
                StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, $"{what}: {ex.Method} did not complete. Engine report:");
                StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, ex.Report);
            }
        });
    }
    #endregion

    #region Object set buttons
    /// <summary>
    /// Keep the current scenario as an object set
    /// </summary>
    private async void ButtonSnapshot_Click(object sender, EventArgs e)
    {
        await PerformLongOp(async () =>
        {
            _snapshot = await _stpRecognizer.GetScenarioObjectSetContentAsync();
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Snapshot taken: {_snapshot.Count} objects");
        });
    }

    /// <summary>
    /// Replace the scenario with the snapshot (LoadNewScenarioFromObjectSet)
    /// </summary>
    private async void ButtonSnapshotLoad_Click(object sender, EventArgs e)
    {
        await LoadObjectSet("Load as new scenario", async (os, timeout) =>
        {
            ClearCaches();
            await _stpRecognizer.LoadNewScenarioAsync(os, timeout);
        });
    }

    /// <summary>
    /// Add the snapshot's objects to the current scenario (ImportPlanDataFromObjectSet)
    /// </summary>
    private async void ButtonSnapshotImport_Click(object sender, EventArgs e)
    {
        await LoadObjectSet("Import into scenario", async (os, timeout) =>
        {
            bool ok = await _stpRecognizer.ImportSTPDataAsync(os, timeout);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Import answered {ok}");
        });
    }

    /// <summary>
    /// Bring the session in line with the snapshot, as a client that kept its own copy would after a
    /// reconnect (SyncScenarioSessionFromObjectSet)
    /// </summary>
    private async void ButtonSnapshotSync_Click(object sender, EventArgs e)
    {
        await LoadObjectSet("Sync session", (os, timeout) => _stpRecognizer.SyncScenarioSessionAsync(os, timeout));
    }
    #endregion
}
