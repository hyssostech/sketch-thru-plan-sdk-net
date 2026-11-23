using StpSDK;
using StpSDK.Search;

namespace StpSDKSample;

/// <summary>
/// Symbol search: find the symbols that match a few words ("recon platoon", "phase line"), with the
/// speech that produces each and the gesture to sketch. The SDK answers from the symbol index installed
/// with STP; the engine is not asked, so this works with or without a connection.
/// </summary>
public partial class Form1
{
    #region Symbol search state
    private SymbolSearch _symbolSearch;
    private ToolPanel _searchPanel;
    private ListView _listSearchResults;
    private TextBox _textSearch;
    private TextBox _textIndexPath;
    #endregion

    /// <summary>
    /// Build the symbol search panel, and a toolbar button that shows it
    /// </summary>
    private void ShowSymbolSearchPanel()
    {
        if (_searchPanel == null)
        {
            _listSearchResults = ToolPanel.NewList(("Name", 200), ("Say", 160), ("Sketch", 70), ("SIDC", 170), ("Score", 50));
            _searchPanel = new ToolPanel(this, "Symbol search", new Size(680, 340), _listSearchResults);
            _textSearch = _searchPanel.AddControl(new TextBox { Width = 200, PlaceholderText = "e.g. recon platoon" });
            var buttonSearch = _searchPanel.AddButton("Search", (s, e) => DoSymbolSearch());
            _searchPanel.AcceptButton = buttonSearch;
            _searchPanel.AddLabel("Index file");
            _textIndexPath = _searchPanel.AddControl(new TextBox { Width = 220, PlaceholderText = "(STP_SYMBOL_INDEX or the STP install)" });

            toolStrip1.Items.Add(new ToolStripButton("Symbol search", null, (s, e) => _searchPanel.Show()));
        }
        _searchPanel.Show();
    }

    /// <summary>
    /// Search the symbol index. The index is read on first use: the path given, else the file the
    /// STP_SYMBOL_INDEX environment variable names, else the one installed with STP
    /// </summary>
    private void DoSymbolSearch()
    {
        string text = _textSearch.Text.Trim();
        if (text.Length == 0) return;
        try
        {
            if (_symbolSearch == null)
            {
                string path = string.IsNullOrWhiteSpace(_textIndexPath.Text) ? null : _textIndexPath.Text.Trim();
                SymbolIndex index = SymbolIndex.LoadDefault(path, msg => StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, msg));
                _symbolSearch = new SymbolSearch(index);
            }
        }
        catch (SymbolIndexUnavailableException ex)
        {
            StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Warning, $"Symbol search unavailable: {ex.Message}");
            return;
        }

        IReadOnlyList<SymbolSearchResult> results = _symbolSearch.Search(text);
        _listSearchResults.BeginUpdate();
        _listSearchResults.Items.Clear();
        foreach (var r in results)
        {
            var item = new ListViewItem(r.Name);
            item.SubItems.Add(r.Speech);
            item.SubItems.Add(r.Gesture);
            item.SubItems.Add(r.DeltaSidc ?? r.CharlieSidc);
            item.SubItems.Add(r.Score.ToString("0.00"));
            _listSearchResults.Items.Add(item);
        }
        _listSearchResults.EndUpdate();
        StpRecognizer_OnStpMessage(StpRecognizer.StpMessageLevel.Info, $"Symbol search '{text}': {results.Count} result(s)");
    }
}
