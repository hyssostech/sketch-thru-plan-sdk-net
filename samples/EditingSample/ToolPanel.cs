namespace StpSDKSample;

/// <summary>
/// A small tool window, owned by the main form, that holds the controls of one sample feature.
/// </summary>
/// <remarks>
/// The samples share the same main form layout. Rather than squeezing more controls into it, the
/// features a sample adds live in a panel like this one: a body (a list, typically) and a row of
/// buttons and inputs underneath. Closing the panel only hides it; a toolbar button shows it again.
/// </remarks>
internal class ToolPanel : Form
{
    private readonly FlowLayoutPanel _controls;

    public ToolPanel(Form owner, string title, Size size, Control body)
    {
        Text = title;
        Owner = owner;
        ShowInTaskbar = false;
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        StartPosition = FormStartPosition.Manual;
        Size = size;
        Location = new Point(Math.Max(0, owner.Right - size.Width - 24), owner.Top + 90);

        _controls = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Padding = new Padding(4),
        };
        Controls.Add(_controls);
        if (body != null)
        {
            body.Dock = DockStyle.Fill;
            Controls.Add(body);
            body.BringToFront();
        }

        // Hide rather than close, so the panel keeps its state and can be shown again
        FormClosing += (s, e) =>
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    /// <summary>Add a button to the panel's control row</summary>
    public Button AddButton(string text, EventHandler onClick)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += onClick;
        _controls.Controls.Add(button);
        return button;
    }

    /// <summary>Add any other control (a text box, a combo) to the panel's control row</summary>
    public T AddControl<T>(T control) where T : Control
    {
        _controls.Controls.Add(control);
        return control;
    }

    /// <summary>Add a caption to the panel's control row</summary>
    public Label AddLabel(string text)
    {
        return AddControl(new Label { Text = text, AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
    }

    /// <summary>Run an action on the UI thread</summary>
    public void OnUi(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    /// <summary>A details-view list with the given column titles and widths</summary>
    public static ListView NewList(params (string Title, int Width)[] columns)
    {
        var list = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            MultiSelect = false,
            HideSelection = false,
        };
        foreach (var (title, width) in columns)
        {
            list.Columns.Add(title, width);
        }
        return list;
    }
}
