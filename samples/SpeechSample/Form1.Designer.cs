namespace StpSDKSample;

partial class Form1
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        toolStripSplitButton1 = new ToolStripSplitButton();
        txtSimSpeech = new TextBox();
        pictureMap = new PictureBox();
        toolStrip1 = new ToolStrip();
        toolStripSplitButtonTiming = new ToolStripSplitButton();
        plaBtn = new ToolStripMenuItem();
        drawBtn = new ToolStripMenuItem();
        tsLabelTiming = new ToolStripLabel();
        toolStripSeparator1 = new ToolStripSeparator();
        toolStripLabel2 = new ToolStripLabel();
        toolStripTextBoxStpUri = new ToolStripTextBox();
        toolStripButtonConnect = new ToolStripButton();
        textBoxLog = new TextBox();
        groupBoxScenario = new GroupBox();
        buttonSaveData = new Button();
        buttonScenarioLoad = new Button();
        buttonScenarioNew = new Button();
        buttonScenarioJoin = new Button();
        panelAudioCapture = new Panel();
        panelAlternates = new Panel();
        propertyGridResult = new PropertyGrid();
        buttonUpdate = new Button();
        buttonDelete = new Button();
        panel1 = new Panel();
        dataGridViewAlternates = new DataGridView();
        FullDescription = new DataGridViewTextBoxColumn();
        Confidence = new DataGridViewTextBoxColumn();
        ((System.ComponentModel.ISupportInitialize)pictureMap).BeginInit();
        toolStrip1.SuspendLayout();
        groupBoxScenario.SuspendLayout();
        panelAlternates.SuspendLayout();
        panel1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridViewAlternates).BeginInit();
        SuspendLayout();
        // 
        // toolStripSplitButton1
        // 
        toolStripSplitButton1.Name = "toolStripSplitButton1";
        toolStripSplitButton1.Size = new Size(23, 23);
        // 
        // txtSimSpeech
        // 
        txtSimSpeech.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtSimSpeech.Location = new Point(460, 65);
        txtSimSpeech.Margin = new Padding(6, 5, 6, 5);
        txtSimSpeech.Multiline = true;
        txtSimSpeech.Name = "txtSimSpeech";
        txtSimSpeech.Size = new Size(975, 37);
        txtSimSpeech.TabIndex = 13;
        // 
        // pictureMap
        // 
        pictureMap.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pictureMap.BorderStyle = BorderStyle.FixedSingle;
        pictureMap.Image = (Image)resources.GetObject("pictureMap.Image");
        pictureMap.InitialImage = (Image)resources.GetObject("pictureMap.InitialImage");
        pictureMap.Location = new Point(453, 113);
        pictureMap.Margin = new Padding(6, 5, 6, 5);
        pictureMap.Name = "pictureMap";
        pictureMap.Size = new Size(992, 650);
        pictureMap.SizeMode = PictureBoxSizeMode.Zoom;
        pictureMap.TabIndex = 11;
        pictureMap.TabStop = false;
        // 
        // toolStrip1
        // 
        toolStrip1.BackgroundImageLayout = ImageLayout.None;
        toolStrip1.ImageScalingSize = new Size(24, 24);
        toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripSplitButtonTiming, tsLabelTiming, toolStripSeparator1, toolStripLabel2, toolStripTextBoxStpUri, toolStripButtonConnect });
        toolStrip1.Location = new Point(0, 0);
        toolStrip1.Name = "toolStrip1";
        toolStrip1.Padding = new Padding(0, 0, 3, 0);
        toolStrip1.Size = new Size(1449, 33);
        toolStrip1.TabIndex = 9;
        toolStrip1.Text = "toolStrip1";
        // 
        // toolStripSplitButtonTiming
        // 
        toolStripSplitButtonTiming.DisplayStyle = ToolStripItemDisplayStyle.Image;
        toolStripSplitButtonTiming.DropDownItems.AddRange(new ToolStripItem[] { plaBtn, drawBtn });
        toolStripSplitButtonTiming.Image = (Image)resources.GetObject("toolStripSplitButtonTiming.Image");
        toolStripSplitButtonTiming.ImageTransparentColor = Color.Magenta;
        toolStripSplitButtonTiming.Name = "toolStripSplitButtonTiming";
        toolStripSplitButtonTiming.Size = new Size(45, 28);
        toolStripSplitButtonTiming.Text = "toolStripSplitButton2";
        toolStripSplitButtonTiming.ToolTipText = "toolStripBtn";
        // 
        // plaBtn
        // 
        plaBtn.Checked = true;
        plaBtn.CheckState = CheckState.Checked;
        plaBtn.Name = "plaBtn";
        plaBtn.Size = new Size(387, 34);
        plaBtn.Text = "Mode: Freehand Points,Lines,Areas";
        plaBtn.Click += PlaBtn_Click;
        // 
        // drawBtn
        // 
        drawBtn.Name = "drawBtn";
        drawBtn.Size = new Size(387, 34);
        drawBtn.Text = "Mode: Draw 2525 Symbol";
        drawBtn.Click += DrawBtn_Click;
        // 
        // tsLabelTiming
        // 
        tsLabelTiming.Name = "tsLabelTiming";
        tsLabelTiming.Size = new Size(285, 28);
        tsLabelTiming.Text = "Mode: Freehand Points,Lines,Areas";
        // 
        // toolStripSeparator1
        // 
        toolStripSeparator1.Name = "toolStripSeparator1";
        toolStripSeparator1.Size = new Size(6, 33);
        // 
        // toolStripLabel2
        // 
        toolStripLabel2.Name = "toolStripLabel2";
        toolStripLabel2.Size = new Size(156, 28);
        toolStripLabel2.Text = "Connection string:";
        // 
        // toolStripTextBoxStpUri
        // 
        toolStripTextBoxStpUri.Margin = new Padding(1, 1, 5, 0);
        toolStripTextBoxStpUri.Name = "toolStripTextBoxStpUri";
        toolStripTextBoxStpUri.Size = new Size(284, 32);
        toolStripTextBoxStpUri.Text = "localhost:9555";
        // 
        // toolStripButtonConnect
        // 
        toolStripButtonConnect.BackColor = SystemColors.ButtonShadow;
        toolStripButtonConnect.DisplayStyle = ToolStripItemDisplayStyle.Text;
        toolStripButtonConnect.Image = (Image)resources.GetObject("toolStripButtonConnect.Image");
        toolStripButtonConnect.ImageTransparentColor = Color.Magenta;
        toolStripButtonConnect.Margin = new Padding(0, 2, 0, 1);
        toolStripButtonConnect.Name = "toolStripButtonConnect";
        toolStripButtonConnect.Size = new Size(81, 30);
        toolStripButtonConnect.Text = "Connect";
        toolStripButtonConnect.Click += toolStripButtonConnect_Click;
        // 
        // textBoxLog
        // 
        textBoxLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        textBoxLog.Location = new Point(453, 858);
        textBoxLog.Margin = new Padding(6, 5, 6, 5);
        textBoxLog.Multiline = true;
        textBoxLog.Name = "textBoxLog";
        textBoxLog.ReadOnly = true;
        textBoxLog.ScrollBars = ScrollBars.Both;
        textBoxLog.Size = new Size(995, 376);
        textBoxLog.TabIndex = 8;
        // 
        // groupBoxScenario
        // 
        groupBoxScenario.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        groupBoxScenario.Controls.Add(buttonSaveData);
        groupBoxScenario.Controls.Add(buttonScenarioLoad);
        groupBoxScenario.Controls.Add(buttonScenarioNew);
        groupBoxScenario.Controls.Add(buttonScenarioJoin);
        groupBoxScenario.Location = new Point(604, 767);
        groupBoxScenario.Margin = new Padding(4, 5, 4, 5);
        groupBoxScenario.Name = "groupBoxScenario";
        groupBoxScenario.Padding = new Padding(4, 5, 4, 5);
        groupBoxScenario.Size = new Size(689, 87);
        groupBoxScenario.TabIndex = 14;
        groupBoxScenario.TabStop = false;
        groupBoxScenario.Text = "Scenario";
        // 
        // buttonSaveData
        // 
        buttonSaveData.Location = new Point(374, 37);
        buttonSaveData.Margin = new Padding(4, 5, 4, 5);
        buttonSaveData.Name = "buttonSaveData";
        buttonSaveData.Size = new Size(120, 38);
        buttonSaveData.TabIndex = 27;
        buttonSaveData.Text = "Save";
        buttonSaveData.Click += buttonScenarioSave_Click;
        // 
        // buttonScenarioLoad
        // 
        buttonScenarioLoad.Location = new Point(557, 37);
        buttonScenarioLoad.Margin = new Padding(4, 5, 4, 5);
        buttonScenarioLoad.Name = "buttonScenarioLoad";
        buttonScenarioLoad.Size = new Size(120, 38);
        buttonScenarioLoad.TabIndex = 2;
        buttonScenarioLoad.Text = "Load";
        buttonScenarioLoad.Click += buttonScenarioLoad_Click;
        // 
        // buttonScenarioNew
        // 
        buttonScenarioNew.Location = new Point(9, 37);
        buttonScenarioNew.Margin = new Padding(4, 5, 4, 5);
        buttonScenarioNew.Name = "buttonScenarioNew";
        buttonScenarioNew.Size = new Size(120, 38);
        buttonScenarioNew.TabIndex = 3;
        buttonScenarioNew.Text = "New";
        buttonScenarioNew.Click += buttonScenarioNew_Click;
        // 
        // buttonScenarioJoin
        // 
        buttonScenarioJoin.Location = new Point(191, 37);
        buttonScenarioJoin.Margin = new Padding(4, 5, 4, 5);
        buttonScenarioJoin.Name = "buttonScenarioJoin";
        buttonScenarioJoin.Size = new Size(120, 38);
        buttonScenarioJoin.TabIndex = 4;
        buttonScenarioJoin.Text = "Join";
        buttonScenarioJoin.Click += buttonScenarioJoin_Click;
        // 
        // panelAudioCapture
        // 
        panelAudioCapture.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        panelAudioCapture.Location = new Point(451, 58);
        panelAudioCapture.Margin = new Padding(4, 5, 4, 5);
        panelAudioCapture.Name = "panelAudioCapture";
        panelAudioCapture.Size = new Size(994, 53);
        panelAudioCapture.TabIndex = 21;
        // 
        // panelAlternates
        // 
        panelAlternates.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        panelAlternates.BorderStyle = BorderStyle.FixedSingle;
        panelAlternates.Controls.Add(propertyGridResult);
        panelAlternates.Controls.Add(buttonUpdate);
        panelAlternates.Controls.Add(buttonDelete);
        panelAlternates.Location = new Point(1, 775);
        panelAlternates.Margin = new Padding(4, 5, 4, 5);
        panelAlternates.Name = "panelAlternates";
        panelAlternates.Size = new Size(441, 460);
        panelAlternates.TabIndex = 17;
        // 
        // propertyGridResult
        // 
        propertyGridResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        propertyGridResult.HelpVisible = false;
        propertyGridResult.Location = new Point(-1, -2);
        propertyGridResult.Margin = new Padding(6, 5, 6, 5);
        propertyGridResult.Name = "propertyGridResult";
        propertyGridResult.PropertySort = PropertySort.Alphabetical;
        propertyGridResult.Size = new Size(443, 407);
        propertyGridResult.TabIndex = 21;
        propertyGridResult.ToolbarVisible = false;
        // 
        // buttonUpdate
        // 
        buttonUpdate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        buttonUpdate.Enabled = false;
        buttonUpdate.Location = new Point(197, 415);
        buttonUpdate.Margin = new Padding(4, 5, 4, 5);
        buttonUpdate.Name = "buttonUpdate";
        buttonUpdate.Size = new Size(107, 38);
        buttonUpdate.TabIndex = 23;
        buttonUpdate.Text = "Update";
        buttonUpdate.UseVisualStyleBackColor = true;
        // 
        // buttonDelete
        // 
        buttonDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        buttonDelete.Enabled = false;
        buttonDelete.Location = new Point(330, 415);
        buttonDelete.Margin = new Padding(4, 5, 4, 5);
        buttonDelete.Name = "buttonDelete";
        buttonDelete.Size = new Size(107, 38);
        buttonDelete.TabIndex = 22;
        buttonDelete.Text = "Delete";
        buttonDelete.UseVisualStyleBackColor = true;
        // 
        // panel1
        // 
        panel1.Controls.Add(dataGridViewAlternates);
        panel1.Location = new Point(3, 58);
        panel1.Margin = new Padding(4, 5, 4, 5);
        panel1.Name = "panel1";
        panel1.Size = new Size(441, 707);
        panel1.TabIndex = 22;
        // 
        // dataGridViewAlternates
        // 
        dataGridViewAlternates.AllowUserToAddRows = false;
        dataGridViewAlternates.AllowUserToDeleteRows = false;
        dataGridViewAlternates.BackgroundColor = SystemColors.ControlLightLight;
        dataGridViewAlternates.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridViewAlternates.Columns.AddRange(new DataGridViewColumn[] { FullDescription, Confidence });
        dataGridViewAlternates.Dock = DockStyle.Fill;
        dataGridViewAlternates.Location = new Point(0, 0);
        dataGridViewAlternates.Margin = new Padding(4, 5, 4, 5);
        dataGridViewAlternates.MultiSelect = false;
        dataGridViewAlternates.Name = "dataGridViewAlternates";
        dataGridViewAlternates.ReadOnly = true;
        dataGridViewAlternates.RowHeadersWidth = 24;
        dataGridViewAlternates.RowTemplate.Height = 25;
        dataGridViewAlternates.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridViewAlternates.ShowEditingIcon = false;
        dataGridViewAlternates.Size = new Size(441, 707);
        dataGridViewAlternates.TabIndex = 14;
        // 
        // FullDescription
        // 
        FullDescription.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        FullDescription.HeaderText = "Alternate";
        FullDescription.MinimumWidth = 8;
        FullDescription.Name = "FullDescription";
        FullDescription.ReadOnly = true;
        // 
        // Confidence
        // 
        Confidence.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
        dataGridViewCellStyle1.Format = "N2";
        dataGridViewCellStyle1.NullValue = null;
        Confidence.DefaultCellStyle = dataGridViewCellStyle1;
        Confidence.HeaderText = "Conf";
        Confidence.MinimumWidth = 8;
        Confidence.Name = "Confidence";
        Confidence.ReadOnly = true;
        Confidence.Width = 86;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1449, 1238);
        Controls.Add(panel1);
        Controls.Add(panelAlternates);
        Controls.Add(groupBoxScenario);
        Controls.Add(txtSimSpeech);
        Controls.Add(pictureMap);
        Controls.Add(toolStrip1);
        Controls.Add(textBoxLog);
        Controls.Add(panelAudioCapture);
        Margin = new Padding(6, 5, 6, 5);
        Name = "Form1";
        Text = "Sketch-Thru-Plan SDK Speech  Sample";
        FormClosing += Form1_FormClosing;
        Load += Form1_Load;
        ((System.ComponentModel.ISupportInitialize)pictureMap).EndInit();
        toolStrip1.ResumeLayout(false);
        toolStrip1.PerformLayout();
        groupBoxScenario.ResumeLayout(false);
        panelAlternates.ResumeLayout(false);
        panel1.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)dataGridViewAlternates).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
    private ToolStripSplitButton toolStripSplitButton1;
    private TextBox txtSimSpeech;
    private PictureBox pictureMap;
    private ToolStrip toolStrip1;
    private ToolStripSplitButton toolStripSplitButtonTiming;
    private ToolStripMenuItem plaBtn;
    private ToolStripMenuItem drawBtn;
    private ToolStripLabel tsLabelTiming;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripLabel toolStripLabel2;
    private ToolStripTextBox toolStripTextBoxStpUri;
    private ToolStripButton toolStripButtonConnect;
    private TextBox textBoxLog;
    private GroupBox groupBoxScenario;
    private Button buttonScenarioNew;
    private Button buttonScenarioJoin;
    private Button buttonScenarioLoad;
    private Panel panelAudioCapture;
    private Panel panelAlternates;
    private Button buttonUpdate;
    private Button buttonDelete;
    private PropertyGrid propertyGridResult;
    private Button buttonSaveData;
    private Panel panel1;
    private DataGridView dataGridViewAlternates;
    private DataGridViewTextBoxColumn FullDescription;
    private DataGridViewTextBoxColumn Confidence;
}

