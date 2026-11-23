using StpSDK;

namespace StpSDKSample;

/// <summary>
/// Task history and comments: what was done to a task, by whom and from where, and the comments
/// users add to it. The history is read with GetTaskHistoryAsync and followed live through the
/// TaskEventAdded / TaskEventModified events.
/// </summary>
public partial class Form1
{
    #region Task history state
    // The task whose history is shown, and its entries by entry poid
    private string _historyTaskPoid;
    private readonly Dictionary<string, TaskHistoryEntry> _historyEntries = new();

    private ToolPanel _historyPanel;
    private ListView _listHistory;
    private TextBox _textComment;
    #endregion

    #region Task history events
    /// <summary>
    /// Subscribe to the task history events. Called before connecting, like every other subscription.
    /// </summary>
    /// <remarks>
    /// The history names the author of each change and comment: the user this client registers with
    /// (StpRecognizer.User). A client that registers without a user can read the history but cannot
    /// add comments, so the user must be set before connecting.
    /// </remarks>
    private void SubscribeTaskHistoryEvents()
    {
        _stpRecognizer.User = Environment.UserName;
        _stpRecognizer.OnTaskEventAdded += StpRecognizer_OnTaskEventAdded;
        _stpRecognizer.OnTaskEventModified += StpRecognizer_OnTaskEventModified;
    }

    /// <summary>
    /// A task's history gained an entry - an edit, retask, confirm, delete or undo by any client, or a comment
    /// </summary>
    private void StpRecognizer_OnTaskEventAdded(string poid, TaskHistoryEntry entry, string batchId)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"TASK EVENT ADDED:\t{entry?.Task}\t{entry?.Kind}\t{entry?.Author}\t{entry?.Text}");
        ShowHistoryEntry(poid, entry);
    }

    /// <summary>
    /// A comment was edited or deleted by its author; a deleted comment arrives as its tombstone
    /// </summary>
    private void StpRecognizer_OnTaskEventModified(string poid, TaskHistoryEntry entry, string batchId)
    {
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"TASK EVENT MODIFIED:\t{entry?.Task}\t{entry?.Kind}\t{entry?.Text}");
        ShowHistoryEntry(poid, entry);
    }

    private void ShowHistoryEntry(string poid, TaskHistoryEntry entry)
    {
        // Only the task being shown is of interest here
        if (entry == null || entry.Task != _historyTaskPoid) return;
        lock (_historyEntries) { _historyEntries[poid] = entry; }
        RefreshHistoryList();
    }
    #endregion

    #region Task history panel
    /// <summary>
    /// Build the task history panel, and a toolbar button that shows it
    /// </summary>
    private void ShowTaskHistoryPanel()
    {
        if (_historyPanel == null)
        {
            _listHistory = ToolPanel.NewList(("Time (UTC)", 140), ("Kind", 80), ("Author", 90), ("Role", 50), ("Text", 220), ("Entry poid", 120));
            _historyPanel = new ToolPanel(this, "Task history", new Size(720, 340), _listHistory);
            _historyPanel.AddButton("Show history of current task", ButtonHistoryLoad_Click);
            _textComment = _historyPanel.AddControl(new TextBox { Width = 220, PlaceholderText = "Comment" });
            _historyPanel.AddButton("Add comment", ButtonCommentAdd_Click);
            _historyPanel.AddButton("Edit selected", ButtonCommentEdit_Click);
            _historyPanel.AddButton("Delete selected", ButtonCommentDelete_Click);
            _listHistory.SelectedIndexChanged += (s, e) =>
            {
                if (_listHistory.SelectedItems.Count > 0) _textComment.Text = _listHistory.SelectedItems[0].SubItems[4].Text;
            };

            toolStrip1.Items.Add(new ToolStripButton("Task history", null, (s, e) => _historyPanel.Show()));
        }
        _historyPanel.Show();
    }

    private void RefreshHistoryList()
    {
        _historyPanel?.OnUi(() =>
        {
            _listHistory.BeginUpdate();
            _listHistory.Items.Clear();
            List<KeyValuePair<string, TaskHistoryEntry>> entries;
            lock (_historyEntries) { entries = _historyEntries.ToList(); }
            // Newest first, as GetTaskHistory answers
            foreach (var (poid, entry) in entries.OrderByDescending(kv => kv.Value.Utc))
            {
                string text = entry.Deleted != null ? "(deleted)" : entry.Text;
                if (entry.Edited != null && entry.Deleted == null) text += " (edited)";
                var item = new ListViewItem(entry.Utc) { Tag = poid };
                item.SubItems.Add(entry.Kind);
                item.SubItems.Add(entry.Author);
                item.SubItems.Add(entry.Role);
                item.SubItems.Add(text ?? entry.Detail);
                item.SubItems.Add(poid);
                _listHistory.Items.Add(item);
            }
            _listHistory.EndUpdate();
        });
    }

    private string SelectedHistoryEntry()
    {
        string poid = _listHistory.SelectedItems.Count > 0 ? (string)_listHistory.SelectedItems[0].Tag : null;
        if (poid == null)
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, "Select a comment in the history first");
        }
        return poid;
    }

    /// <summary>
    /// Run a history call, showing refusals (for example editing someone else's comment) in the log
    /// </summary>
    private async Task HistoryOp(Func<Task> action)
    {
        _historyPanel.Enabled = false;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, $"Task history operation failed: {ex.Message}");
        }
        finally
        {
            _historyPanel.Enabled = true;
        }
    }
    #endregion

    #region Task history buttons
    /// <summary>
    /// Read the history of the current (last recognized) task. Entries outlive a deleted task
    /// </summary>
    private async void ButtonHistoryLoad_Click(object sender, EventArgs e)
    {
        string taskPoid = _currentTask?.Poid ?? _historyTaskPoid;
        if (taskPoid == null)
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, "No task yet - place a unit, an axis and an objective to create one");
            return;
        }
        await HistoryOp(async () =>
        {
            List<TaskHistoryItem> history = await _stpRecognizer.GetTaskHistoryAsync(taskPoid);
            _historyTaskPoid = taskPoid;
            lock (_historyEntries)
            {
                _historyEntries.Clear();
                foreach (var item in history) _historyEntries[item.Poid] = item.Entry;
            }
            _historyPanel.Text = $"Task history - {_currentTask?.FullDescription ?? taskPoid}";
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Task {taskPoid} has {history.Count} history entries");
            RefreshHistoryList();
        });
    }

    /// <summary>
    /// Add a comment. The engine stamps author, role and time; TaskEventAdded goes to every client
    /// </summary>
    private async void ButtonCommentAdd_Click(object sender, EventArgs e)
    {
        string text = _textComment.Text.Trim();
        if (_historyTaskPoid == null || text.Length == 0)
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, "Show a task's history and type a comment first");
            return;
        }
        await HistoryOp(async () =>
        {
            string entryPoid = await _stpRecognizer.AddTaskCommentAsync(_historyTaskPoid, text);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Comment {entryPoid} added");
            _textComment.Clear();
        });
    }

    /// <summary>
    /// Change a comment's text. Only its author may; TaskEventModified follows
    /// </summary>
    private async void ButtonCommentEdit_Click(object sender, EventArgs e)
    {
        string entryPoid = SelectedHistoryEntry();
        if (entryPoid == null) return;
        string text = _textComment.Text.Trim();
        await HistoryOp(async () =>
        {
            bool ok = await _stpRecognizer.EditTaskCommentAsync(entryPoid, text);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Comment {entryPoid} edit: {(ok ? "done" : "not done")}");
        });
    }

    /// <summary>
    /// Delete a comment. Only its author may; the entry stays as a tombstone and TaskEventModified follows
    /// </summary>
    private async void ButtonCommentDelete_Click(object sender, EventArgs e)
    {
        string entryPoid = SelectedHistoryEntry();
        if (entryPoid == null) return;
        await HistoryOp(async () =>
        {
            bool ok = await _stpRecognizer.DeleteTaskCommentAsync(entryPoid);
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Comment {entryPoid} delete: {(ok ? "done" : "not done")}");
        });
    }
    #endregion
}
