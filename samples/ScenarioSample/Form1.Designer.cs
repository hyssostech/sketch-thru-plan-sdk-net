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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.toolStripSplitButton1 = new System.Windows.Forms.ToolStripSplitButton();
            this.txtSimSpeech = new System.Windows.Forms.TextBox();
            this.propertyGridResult = new System.Windows.Forms.PropertyGrid();
            this.pictureMap = new System.Windows.Forms.PictureBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripSplitButton2 = new System.Windows.Forms.ToolStripSplitButton();
            this.plaBtn = new System.Windows.Forms.ToolStripMenuItem();
            this.drawBtn = new System.Windows.Forms.ToolStripMenuItem();
            this.tsLabelTiming = new System.Windows.Forms.ToolStripLabel();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttonDataImport = new System.Windows.Forms.Button();
            this.buttonScenarioSave = new System.Windows.Forms.Button();
            this.buttonScenarioLoad = new System.Windows.Forms.Button();
            this.buttonScenarioNew = new System.Windows.Forms.Button();
            this.buttonScenarioJoin = new System.Windows.Forms.Button();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.panel1 = new System.Windows.Forms.Panel();
            this.dataGridViewAlternates = new System.Windows.Forms.DataGridView();
            this.FullDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Confidence = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.buttonUpdate = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureMap)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAlternates)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStripSplitButton1
            // 
            this.toolStripSplitButton1.Name = "toolStripSplitButton1";
            this.toolStripSplitButton1.Size = new System.Drawing.Size(23, 23);
            // 
            // txtSimSpeech
            // 
            this.txtSimSpeech.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSimSpeech.Location = new System.Drawing.Point(317, 28);
            this.txtSimSpeech.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtSimSpeech.Multiline = true;
            this.txtSimSpeech.Name = "txtSimSpeech";
            this.txtSimSpeech.Size = new System.Drawing.Size(685, 24);
            this.txtSimSpeech.TabIndex = 13;
            // 
            // propertyGridResult
            // 
            this.propertyGridResult.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.propertyGridResult.HelpVisible = false;
            this.propertyGridResult.Location = new System.Drawing.Point(0, 452);
            this.propertyGridResult.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.propertyGridResult.Name = "propertyGridResult";
            this.propertyGridResult.PropertySort = System.Windows.Forms.PropertySort.Alphabetical;
            this.propertyGridResult.Size = new System.Drawing.Size(310, 252);
            this.propertyGridResult.TabIndex = 12;
            this.propertyGridResult.ToolbarVisible = false;
            // 
            // pictureMap
            // 
            this.pictureMap.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureMap.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureMap.Image = ((System.Drawing.Image)(resources.GetObject("pictureMap.Image")));
            this.pictureMap.InitialImage = ((System.Drawing.Image)(resources.GetObject("pictureMap.InitialImage")));
            this.pictureMap.Location = new System.Drawing.Point(317, 55);
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
            this.textBoxLog.Location = new System.Drawing.Point(317, 510);
            this.textBoxLog.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ReadOnly = true;
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLog.Size = new System.Drawing.Size(698, 232);
            this.textBoxLog.TabIndex = 8;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttonDataImport);
            this.groupBox1.Controls.Add(this.buttonScenarioSave);
            this.groupBox1.Controls.Add(this.buttonScenarioLoad);
            this.groupBox1.Controls.Add(this.buttonScenarioNew);
            this.groupBox1.Controls.Add(this.buttonScenarioJoin);
            this.groupBox1.Location = new System.Drawing.Point(317, 452);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(695, 52);
            this.groupBox1.TabIndex = 14;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Scenario";
            // 
            // buttonDataImport
            // 
            this.buttonDataImport.Location = new System.Drawing.Point(595, 20);
            this.buttonDataImport.Name = "buttonDataImport";
            this.buttonDataImport.Size = new System.Drawing.Size(84, 23);
            this.buttonDataImport.TabIndex = 0;
            this.buttonDataImport.Text = "Add/Merge";
            // 
            // buttonScenarioSave
            // 
            this.buttonScenarioSave.Location = new System.Drawing.Point(157, 20);
            this.buttonScenarioSave.Name = "buttonScenarioSave";
            this.buttonScenarioSave.Size = new System.Drawing.Size(84, 23);
            this.buttonScenarioSave.TabIndex = 1;
            this.buttonScenarioSave.Text = "Save";
            // 
            // buttonScenarioLoad
            // 
            this.buttonScenarioLoad.Location = new System.Drawing.Point(11, 20);
            this.buttonScenarioLoad.Name = "buttonScenarioLoad";
            this.buttonScenarioLoad.Size = new System.Drawing.Size(84, 23);
            this.buttonScenarioLoad.TabIndex = 2;
            this.buttonScenarioLoad.Text = "Load";
            // 
            // buttonScenarioNew
            // 
            this.buttonScenarioNew.Location = new System.Drawing.Point(449, 20);
            this.buttonScenarioNew.Name = "buttonScenarioNew";
            this.buttonScenarioNew.Size = new System.Drawing.Size(84, 23);
            this.buttonScenarioNew.TabIndex = 3;
            this.buttonScenarioNew.Text = "New";
            // 
            // buttonScenarioJoin
            // 
            this.buttonScenarioJoin.Location = new System.Drawing.Point(303, 20);
            this.buttonScenarioJoin.Name = "buttonScenarioJoin";
            this.buttonScenarioJoin.Size = new System.Drawing.Size(84, 23);
            this.buttonScenarioJoin.TabIndex = 4;
            this.buttonScenarioJoin.Text = "Join";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(430, 239);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(469, 23);
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 15;
            this.progressBar1.UseWaitCursor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.dataGridViewAlternates);
            this.panel1.Location = new System.Drawing.Point(1, 28);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(309, 418);
            this.panel1.TabIndex = 17;
            // 
            // dataGridViewAlternates
            // 
            this.dataGridViewAlternates.AllowUserToAddRows = false;
            this.dataGridViewAlternates.AllowUserToDeleteRows = false;
            this.dataGridViewAlternates.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridViewAlternates.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewAlternates.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.FullDescription,
            this.Confidence});
            this.dataGridViewAlternates.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewAlternates.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewAlternates.MultiSelect = false;
            this.dataGridViewAlternates.Name = "dataGridViewAlternates";
            this.dataGridViewAlternates.ReadOnly = true;
            this.dataGridViewAlternates.RowHeadersWidth = 24;
            this.dataGridViewAlternates.RowTemplate.Height = 25;
            this.dataGridViewAlternates.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewAlternates.ShowEditingIcon = false;
            this.dataGridViewAlternates.Size = new System.Drawing.Size(309, 418);
            this.dataGridViewAlternates.TabIndex = 15;
            // 
            // FullDescription
            // 
            this.FullDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.FullDescription.HeaderText = "Alternate";
            this.FullDescription.Name = "FullDescription";
            this.FullDescription.ReadOnly = true;
            // 
            // Confidence
            // 
            this.Confidence.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            dataGridViewCellStyle2.Format = "N2";
            dataGridViewCellStyle2.NullValue = null;
            this.Confidence.DefaultCellStyle = dataGridViewCellStyle2;
            this.Confidence.HeaderText = "Conf";
            this.Confidence.Name = "Confidence";
            this.Confidence.ReadOnly = true;
            this.Confidence.Width = 58;
            // 
            // buttonUpdate
            // 
            this.buttonUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonUpdate.Enabled = false;
            this.buttonUpdate.Location = new System.Drawing.Point(142, 710);
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Size = new System.Drawing.Size(75, 23);
            this.buttonUpdate.TabIndex = 20;
            this.buttonUpdate.Text = "Update";
            this.buttonUpdate.UseVisualStyleBackColor = true;
            // 
            // buttonDelete
            // 
            this.buttonDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonDelete.Enabled = false;
            this.buttonDelete.Location = new System.Drawing.Point(235, 710);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(75, 23);
            this.buttonDelete.TabIndex = 19;
            this.buttonDelete.Text = "Delete";
            this.buttonDelete.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1014, 743);
            this.Controls.Add(this.buttonUpdate);
            this.Controls.Add(this.buttonDelete);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.txtSimSpeech);
            this.Controls.Add(this.propertyGridResult);
            this.Controls.Add(this.pictureMap);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.textBoxLog);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "Form1";
            this.Text = "Sketch-Thru-Plan SDK Tasking  Sample";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureMap)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAlternates)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion
    private System.Windows.Forms.ToolStripSplitButton toolStripSplitButton1;
    private TextBox txtSimSpeech;
    private PropertyGrid propertyGridResult;
    private PictureBox pictureMap;
    private ToolStrip toolStrip1;
    private ToolStripSplitButton toolStripSplitButton2;
    private ToolStripMenuItem plaBtn;
    private ToolStripMenuItem drawBtn;
    private ToolStripLabel tsLabelTiming;
    private TextBox textBoxLog;
    private GroupBox groupBox1;
    private Button buttonScenarioNew;
    private Button buttonScenarioJoin;
    private Button buttonDataImport;
    private Button buttonScenarioSave;
    private Button buttonScenarioLoad;
    private ProgressBar progressBar1;
    private Panel panel1;
    private DataGridView dataGridViewAlternates;
    private DataGridViewTextBoxColumn FullDescription;
    private DataGridViewTextBoxColumn Confidence;
    private Button buttonUpdate;
    private Button buttonDelete;
}

