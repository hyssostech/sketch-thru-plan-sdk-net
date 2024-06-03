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
        components = new System.ComponentModel.Container();
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        toolStripSplitButton1 = new ToolStripSplitButton();
        txtSimSpeech = new TextBox();
        propertyGridResult = new PropertyGrid();
        pictureMap = new PictureBox();
        btnClearLog = new Button();
        toolStrip1 = new ToolStrip();
        toolStripSplitButton2 = new ToolStripSplitButton();
        plaBtn = new ToolStripMenuItem();
        drawBtn = new ToolStripMenuItem();
        tsLabelTiming = new ToolStripLabel();
        toolStripSeparator1 = new ToolStripSeparator();
        toolStripLabel2 = new ToolStripLabel();
        toolStripTextBoxStpUri = new ToolStripTextBox();
        toolStripButtonConnect = new ToolStripButton();
        textBoxLog = new TextBox();
        dataGridViewAlternates = new DataGridView();
        FullDescription = new DataGridViewTextBoxColumn();
        Confidence = new DataGridViewTextBoxColumn();
        buttonDelete = new Button();
        stpItemBindingSource = new BindingSource(components);
        alternatesBindingSource = new BindingSource(components);
        alternatesBindingSource1 = new BindingSource(components);
        panel1 = new Panel();
        panelAudioCapture = new Panel();
        buttonUpdate = new Button();
        toolStripLabel1 = new ToolStripLabel();
        toolStripAutoTaskingState = new ToolStripLabel();
        ((System.ComponentModel.ISupportInitialize)pictureMap).BeginInit();
        toolStrip1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridViewAlternates).BeginInit();
        ((System.ComponentModel.ISupportInitialize)stpItemBindingSource).BeginInit();
        ((System.ComponentModel.ISupportInitialize)alternatesBindingSource).BeginInit();
        ((System.ComponentModel.ISupportInitialize)alternatesBindingSource1).BeginInit();
        panel1.SuspendLayout();
        SuspendLayout();
        // 
        // toolStripSplitButton1
        // 
        toolStripSplitButton1.Name = "toolStripSplitButton1";
        toolStripSplitButton1.Size = new Size(23, 23);
        // 
        // txtSimSpeech
        // 
        txtSimSpeech.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtSimSpeech.Location = new Point(454, 728);
        txtSimSpeech.Margin = new Padding(6, 5, 6, 5);
        txtSimSpeech.Multiline = true;
        txtSimSpeech.Name = "txtSimSpeech";
        txtSimSpeech.Size = new Size(887, 36);
        txtSimSpeech.TabIndex = 13;
        // 
        // propertyGridResult
        // 
        propertyGridResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
        propertyGridResult.HelpVisible = false;
        propertyGridResult.Location = new Point(4, 725);
        propertyGridResult.Margin = new Padding(6, 5, 6, 5);
        propertyGridResult.Name = "propertyGridResult";
        propertyGridResult.PropertySort = PropertySort.NoSort;
        propertyGridResult.Size = new Size(441, 378);
        propertyGridResult.TabIndex = 12;
        propertyGridResult.ToolbarVisible = false;
        // 
        // pictureMap
        // 
        pictureMap.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pictureMap.BorderStyle = BorderStyle.FixedSingle;
        pictureMap.Image = (Image)resources.GetObject("pictureMap.Image");
        pictureMap.InitialImage = (Image)resources.GetObject("pictureMap.InitialImage");
        pictureMap.Location = new Point(453, 47);
        pictureMap.Margin = new Padding(6, 5, 6, 5);
        pictureMap.Name = "pictureMap";
        pictureMap.Size = new Size(1025, 670);
        pictureMap.SizeMode = PictureBoxSizeMode.Zoom;
        pictureMap.TabIndex = 11;
        pictureMap.TabStop = false;
        // 
        // btnClearLog
        // 
        btnClearLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        btnClearLog.Location = new Point(1353, 728);
        btnClearLog.Margin = new Padding(6, 5, 6, 5);
        btnClearLog.Name = "btnClearLog";
        btnClearLog.Size = new Size(126, 37);
        btnClearLog.TabIndex = 10;
        btnClearLog.Text = "Reset STP";
        btnClearLog.UseVisualStyleBackColor = true;
        btnClearLog.Click += BtnClearLog_Click_1;
        // 
        // toolStrip1
        // 
        toolStrip1.BackgroundImageLayout = ImageLayout.None;
        toolStrip1.ImageScalingSize = new Size(24, 24);
        toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripSplitButton2, tsLabelTiming, toolStripLabel1, toolStripAutoTaskingState });
        toolStrip1.Location = new Point(0, 0);
        toolStrip1.Name = "toolStrip1";
        toolStrip1.Padding = new Padding(0, 0, 3, 0);
        toolStrip1.Size = new Size(1496, 33);
        toolStrip1.TabIndex = 9;
        toolStrip1.Text = "toolStrip1";
        // 
        // toolStripSplitButton2
        // 
        toolStripSplitButton2.DisplayStyle = ToolStripItemDisplayStyle.Image;
        toolStripSplitButton2.DropDownItems.AddRange(new ToolStripItem[] { plaBtn, drawBtn });
        toolStripSplitButton2.Image = (Image)resources.GetObject("toolStripSplitButton2.Image");
        toolStripSplitButton2.ImageTransparentColor = Color.Magenta;
        toolStripSplitButton2.Name = "toolStripSplitButton2";
        toolStripSplitButton2.Size = new Size(45, 28);
        toolStripSplitButton2.Text = "toolStripSplitButton2";
        toolStripSplitButton2.ToolTipText = "toolStripBtn";
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
        toolStripSeparator1.Size = new Size(6, 25);
        // 
        // toolStripLabel2
        // 
        toolStripLabel2.Name = "toolStripLabel2";
        toolStripLabel2.Size = new Size(105, 22);
        toolStripLabel2.Text = "Connection string:";
        // 
        // toolStripTextBoxStpUri
        // 
        toolStripTextBoxStpUri.Margin = new Padding(1, 1, 5, 0);
        toolStripTextBoxStpUri.Name = "toolStripTextBoxStpUri";
        toolStripTextBoxStpUri.Size = new Size(200, 24);
        toolStripTextBoxStpUri.Text = "localhost:9555";
        // 
        // toolStripButtonConnect
        // 
        toolStripButtonConnect.BackColor = SystemColors.ButtonShadow;
        toolStripButtonConnect.DisplayStyle = ToolStripItemDisplayStyle.Text;
        toolStripButtonConnect.ImageTransparentColor = Color.Magenta;
        toolStripButtonConnect.Margin = new Padding(0, 2, 0, 1);
        toolStripButtonConnect.Name = "toolStripButtonConnect";
        toolStripButtonConnect.Size = new Size(56, 22);
        toolStripButtonConnect.Text = "Connect";
        toolStripButtonConnect.Click += toolStripButtonConnect_Click;
        // 
        // textBoxLog
        // 
        textBoxLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        textBoxLog.Location = new Point(453, 775);
        textBoxLog.Margin = new Padding(6, 5, 6, 5);
        textBoxLog.Multiline = true;
        textBoxLog.Name = "textBoxLog";
        textBoxLog.ReadOnly = true;
        textBoxLog.ScrollBars = ScrollBars.Both;
        textBoxLog.Size = new Size(1043, 381);
        textBoxLog.TabIndex = 8;
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
        dataGridViewAlternates.Size = new Size(441, 672);
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
        // buttonDelete
        // 
        buttonDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        buttonDelete.Enabled = false;
        buttonDelete.Location = new Point(333, 1113);
        buttonDelete.Margin = new Padding(4, 5, 4, 5);
        buttonDelete.Name = "buttonDelete";
        buttonDelete.Size = new Size(107, 38);
        buttonDelete.TabIndex = 15;
        buttonDelete.Text = "Delete";
        buttonDelete.UseVisualStyleBackColor = true;
        // 
        // panel1
        // 
        panel1.Controls.Add(dataGridViewAlternates);
        panel1.Location = new Point(4, 47);
        panel1.Margin = new Padding(4, 5, 4, 5);
        panel1.Name = "panel1";
        panel1.Size = new Size(441, 672);
        panel1.TabIndex = 16;
        // 
        // panelAudioCapture
        // 
        panelAudioCapture.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        panelAudioCapture.Location = new Point(449, 720);
        panelAudioCapture.Margin = new Padding(4, 5, 4, 5);
        panelAudioCapture.Name = "panelAudioCapture";
        panelAudioCapture.Size = new Size(901, 53);
        panelAudioCapture.TabIndex = 17;
        // 
        // buttonUpdate
        // 
        buttonUpdate.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        buttonUpdate.Enabled = false;
        buttonUpdate.Location = new Point(200, 1113);
        buttonUpdate.Margin = new Padding(4, 5, 4, 5);
        buttonUpdate.Name = "buttonUpdate";
        buttonUpdate.Size = new Size(107, 38);
        buttonUpdate.TabIndex = 18;
        buttonUpdate.Text = "Update";
        buttonUpdate.UseVisualStyleBackColor = true;
        // 
        // toolStripLabel1
        // 
        toolStripLabel1.Name = "toolStripLabel1";
        toolStripLabel1.Size = new Size(118, 28);
        toolStripLabel1.Text = "Auto Tasking:";
        // 
        // toolStripAutoTaskingState
        // 
        toolStripAutoTaskingState.BackColor = SystemColors.ActiveCaption;
        toolStripAutoTaskingState.Name = "toolStripAutoTaskingState";
        toolStripAutoTaskingState.Size = new Size(39, 28);
        toolStripAutoTaskingState.Text = "ON";
        toolStripAutoTaskingState.Click += toolStripAutoTaskingState_Click;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1496, 1160);
        Controls.Add(buttonUpdate);
        Controls.Add(panel1);
        Controls.Add(buttonDelete);
        Controls.Add(txtSimSpeech);
        Controls.Add(propertyGridResult);
        Controls.Add(pictureMap);
        Controls.Add(btnClearLog);
        Controls.Add(toolStrip1);
        Controls.Add(textBoxLog);
        Controls.Add(panelAudioCapture);
        Margin = new Padding(6, 5, 6, 5);
        Name = "Form1";
        Text = "Sketch-Thru-Plan SDK Tasking Sample";
        FormClosing += Form1_FormClosing;
        Load += Form1_Load;
        ((System.ComponentModel.ISupportInitialize)pictureMap).EndInit();
        toolStrip1.ResumeLayout(false);
        toolStrip1.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)dataGridViewAlternates).EndInit();
        ((System.ComponentModel.ISupportInitialize)stpItemBindingSource).EndInit();
        ((System.ComponentModel.ISupportInitialize)alternatesBindingSource).EndInit();
        ((System.ComponentModel.ISupportInitialize)alternatesBindingSource1).EndInit();
        panel1.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion
    private System.Windows.Forms.ToolStripSplitButton toolStripSplitButton1;
    private TextBox txtSimSpeech;
    private PropertyGrid propertyGridResult;
    private PictureBox pictureMap;
    private Button btnClearLog;
    private ToolStrip toolStrip1;
    private ToolStripSplitButton toolStripSplitButton2;
    private ToolStripMenuItem plaBtn;
    private ToolStripMenuItem drawBtn;
    private ToolStripLabel tsLabelTiming;
    private ToolStripSeparator toolStripSeparator1;
    private ToolStripLabel toolStripLabel2;
    private ToolStripTextBox toolStripTextBoxStpUri;
    private ToolStripButton toolStripButtonConnect;
    private TextBox textBoxLog;
    private DataGridView dataGridViewAlternates;
    private Button buttonDelete;
    private BindingSource stpItemBindingSource;
    private BindingSource alternatesBindingSource;
    private BindingSource alternatesBindingSource1;
    private Panel panel1;
    private Panel panelAudioCapture;
    private DataGridViewTextBoxColumn FullDescription;
    private DataGridViewTextBoxColumn Confidence;
    private Button buttonUpdate;
    private ToolStripLabel toolStripLabel1;
    private ToolStripLabel toolStripAutoTaskingState;
}

