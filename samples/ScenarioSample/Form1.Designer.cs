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
            this.toolStripSplitButton1 = new System.Windows.Forms.ToolStripSplitButton();
            this.txtSimSpeech = new System.Windows.Forms.TextBox();
            this.pictureMap = new System.Windows.Forms.PictureBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripSplitButton2 = new System.Windows.Forms.ToolStripSplitButton();
            this.plaBtn = new System.Windows.Forms.ToolStripMenuItem();
            this.drawBtn = new System.Windows.Forms.ToolStripMenuItem();
            this.tsLabelTiming = new System.Windows.Forms.ToolStripLabel();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.groupBoxScenario = new System.Windows.Forms.GroupBox();
            this.buttonSaveData = new System.Windows.Forms.Button();
            this.buttonScenarioLoad = new System.Windows.Forms.Button();
            this.buttonScenarioNew = new System.Windows.Forms.Button();
            this.buttonScenarioJoin = new System.Windows.Forms.Button();
            this.panelAudioCapture = new System.Windows.Forms.Panel();
            this.panelAlternates = new System.Windows.Forms.Panel();
            this.buttonUpdate = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.propertyGridResult = new System.Windows.Forms.PropertyGrid();
            ((System.ComponentModel.ISupportInitialize)(this.pictureMap)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.groupBoxScenario.SuspendLayout();
            this.panelAlternates.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripSplitButton1
            // 
            this.toolStripSplitButton1.Name = "toolStripSplitButton1";
            this.toolStripSplitButton1.Size = new System.Drawing.Size(23, 23);
            // 
            // txtSimSpeech
            // 
            this.txtSimSpeech.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSimSpeech.Location = new System.Drawing.Point(322, 39);
            this.txtSimSpeech.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtSimSpeech.Multiline = true;
            this.txtSimSpeech.Name = "txtSimSpeech";
            this.txtSimSpeech.Size = new System.Drawing.Size(684, 24);
            this.txtSimSpeech.TabIndex = 13;
            // 
            // pictureMap
            // 
            this.pictureMap.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureMap.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureMap.Image = ((System.Drawing.Image)(resources.GetObject("pictureMap.Image")));
            this.pictureMap.InitialImage = ((System.Drawing.Image)(resources.GetObject("pictureMap.InitialImage")));
            this.pictureMap.Location = new System.Drawing.Point(317, 68);
            this.pictureMap.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pictureMap.Name = "pictureMap";
            this.pictureMap.Size = new System.Drawing.Size(695, 391);
            this.pictureMap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureMap.TabIndex = 11;
            this.pictureMap.TabStop = false;
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSplitButton2,
            this.tsLabelTiming});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1014, 25);
            this.toolStrip1.TabIndex = 9;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripSplitButton2
            // 
            this.toolStripSplitButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripSplitButton2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.plaBtn,
            this.drawBtn});
            this.toolStripSplitButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripSplitButton2.Image")));
            this.toolStripSplitButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripSplitButton2.Name = "toolStripSplitButton2";
            this.toolStripSplitButton2.Size = new System.Drawing.Size(32, 22);
            this.toolStripSplitButton2.Text = "toolStripSplitButton2";
            this.toolStripSplitButton2.ToolTipText = "toolStripBtn";
            // 
            // plaBtn
            // 
            this.plaBtn.Checked = true;
            this.plaBtn.CheckState = System.Windows.Forms.CheckState.Checked;
            this.plaBtn.Name = "plaBtn";
            this.plaBtn.Size = new System.Drawing.Size(258, 22);
            this.plaBtn.Text = "Mode: Freehand Points,Lines,Areas";
            this.plaBtn.Click += new System.EventHandler(this.PlaBtn_Click);
            // 
            // drawBtn
            // 
            this.drawBtn.Name = "drawBtn";
            this.drawBtn.Size = new System.Drawing.Size(258, 22);
            this.drawBtn.Text = "Mode: Draw 2525 Symbol";
            this.drawBtn.Click += new System.EventHandler(this.DrawBtn_Click);
            // 
            // tsLabelTiming
            // 
            this.tsLabelTiming.Name = "tsLabelTiming";
            this.tsLabelTiming.Size = new System.Drawing.Size(191, 22);
            this.tsLabelTiming.Text = "Mode: Freehand Points,Lines,Areas";
            // 
            // textBoxLog
            // 
            this.textBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLog.Location = new System.Drawing.Point(0, 515);
            this.textBoxLog.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ReadOnly = true;
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLog.Size = new System.Drawing.Size(1015, 227);
            this.textBoxLog.TabIndex = 8;
            // 
            // groupBoxScenario
            // 
            this.groupBoxScenario.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxScenario.Controls.Add(this.buttonSaveData);
            this.groupBoxScenario.Controls.Add(this.buttonScenarioLoad);
            this.groupBoxScenario.Controls.Add(this.buttonScenarioNew);
            this.groupBoxScenario.Controls.Add(this.buttonScenarioJoin);
            this.groupBoxScenario.Location = new System.Drawing.Point(266, 462);
            this.groupBoxScenario.Name = "groupBoxScenario";
            this.groupBoxScenario.Size = new System.Drawing.Size(482, 52);
            this.groupBoxScenario.TabIndex = 14;
            this.groupBoxScenario.TabStop = false;
            this.groupBoxScenario.Text = "Scenario";
            // 
            // buttonSaveData
            // 
            this.buttonSaveData.Location = new System.Drawing.Point(262, 22);
            this.buttonSaveData.Name = "buttonSaveData";
            this.buttonSaveData.Size = new System.Drawing.Size(84, 23);
            this.buttonSaveData.TabIndex = 27;
            this.buttonSaveData.Text = "Save";
            this.buttonSaveData.Click += new System.EventHandler(this.buttonScenarioSave_Click);
            // 
            // buttonScenarioLoad
            // 
            this.buttonScenarioLoad.Location = new System.Drawing.Point(390, 22);
            this.buttonScenarioLoad.Name = "buttonScenarioLoad";
            this.buttonScenarioLoad.Size = new System.Drawing.Size(84, 23);
            this.buttonScenarioLoad.TabIndex = 2;
            this.buttonScenarioLoad.Text = "Load";
            this.buttonScenarioLoad.Click += new System.EventHandler(this.buttonScenarioLoad_Click);
            // 
            // buttonScenarioNew
            // 
            this.buttonScenarioNew.Location = new System.Drawing.Point(6, 22);
            this.buttonScenarioNew.Name = "buttonScenarioNew";
            this.buttonScenarioNew.Size = new System.Drawing.Size(84, 23);
            this.buttonScenarioNew.TabIndex = 3;
            this.buttonScenarioNew.Text = "New";
            this.buttonScenarioNew.Click += new System.EventHandler(this.buttonScenarioNew_Click);
            // 
            // buttonScenarioJoin
            // 
            this.buttonScenarioJoin.Location = new System.Drawing.Point(134, 22);
            this.buttonScenarioJoin.Name = "buttonScenarioJoin";
            this.buttonScenarioJoin.Size = new System.Drawing.Size(84, 23);
            this.buttonScenarioJoin.TabIndex = 4;
            this.buttonScenarioJoin.Text = "Join";
            this.buttonScenarioJoin.Click += new System.EventHandler(this.buttonScenarioJoin_Click);
            // 
            // panelAudioCapture
            // 
            this.panelAudioCapture.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelAudioCapture.Location = new System.Drawing.Point(316, 35);
            this.panelAudioCapture.Name = "panelAudioCapture";
            this.panelAudioCapture.Size = new System.Drawing.Size(696, 32);
            this.panelAudioCapture.TabIndex = 21;
            // 
            // panelAlternates
            // 
            this.panelAlternates.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.panelAlternates.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelAlternates.Controls.Add(this.buttonUpdate);
            this.panelAlternates.Controls.Add(this.buttonDelete);
            this.panelAlternates.Controls.Add(this.propertyGridResult);
            this.panelAlternates.Location = new System.Drawing.Point(1, 35);
            this.panelAlternates.Name = "panelAlternates";
            this.panelAlternates.Size = new System.Drawing.Size(309, 424);
            this.panelAlternates.TabIndex = 17;
            // 
            // buttonUpdate
            // 
            this.buttonUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonUpdate.Enabled = false;
            this.buttonUpdate.Location = new System.Drawing.Point(138, 396);
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Size = new System.Drawing.Size(75, 23);
            this.buttonUpdate.TabIndex = 23;
            this.buttonUpdate.Text = "Update";
            this.buttonUpdate.UseVisualStyleBackColor = true;
            // 
            // buttonDelete
            // 
            this.buttonDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonDelete.Enabled = false;
            this.buttonDelete.Location = new System.Drawing.Point(231, 396);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(75, 23);
            this.buttonDelete.TabIndex = 22;
            this.buttonDelete.Text = "Delete";
            this.buttonDelete.UseVisualStyleBackColor = true;
            // 
            // propertyGridResult
            // 
            this.propertyGridResult.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.propertyGridResult.HelpVisible = false;
            this.propertyGridResult.Location = new System.Drawing.Point(-1, -1);
            this.propertyGridResult.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.propertyGridResult.Name = "propertyGridResult";
            this.propertyGridResult.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
            this.propertyGridResult.Size = new System.Drawing.Size(310, 391);
            this.propertyGridResult.TabIndex = 21;
            this.propertyGridResult.ToolbarVisible = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1014, 743);
            this.Controls.Add(this.panelAlternates);
            this.Controls.Add(this.groupBoxScenario);
            this.Controls.Add(this.txtSimSpeech);
            this.Controls.Add(this.pictureMap);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.panelAudioCapture);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "Form1";
            this.Text = "Sketch-Thru-Plan SDK Scenario  Sample";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureMap)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.groupBoxScenario.ResumeLayout(false);
            this.panelAlternates.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion
    private System.Windows.Forms.ToolStripSplitButton toolStripSplitButton1;
    private TextBox txtSimSpeech;
    private PictureBox pictureMap;
    private ToolStrip toolStrip1;
    private ToolStripSplitButton toolStripSplitButton2;
    private ToolStripMenuItem plaBtn;
    private ToolStripMenuItem drawBtn;
    private ToolStripLabel tsLabelTiming;
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
}

