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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.toolStripSplitButton1 = new System.Windows.Forms.ToolStripSplitButton();
            this.pictureMap = new System.Windows.Forms.PictureBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripSplitButton2 = new System.Windows.Forms.ToolStripSplitButton();
            this.plaBtn = new System.Windows.Forms.ToolStripMenuItem();
            this.drawBtn = new System.Windows.Forms.ToolStripMenuItem();
            this.tsLabelTiming = new System.Windows.Forms.ToolStripLabel();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripTextBoxStpUri = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripButtonConnect = new System.Windows.Forms.ToolStripButton();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.groupBoxScenario = new System.Windows.Forms.GroupBox();
            this.buttonSaveData = new System.Windows.Forms.Button();
            this.buttonScenarioLoad = new System.Windows.Forms.Button();
            this.buttonScenarioNew = new System.Windows.Forms.Button();
            this.buttonScenarioJoin = new System.Windows.Forms.Button();
            this.panelAudioCapture = new System.Windows.Forms.Panel();
            this.txtSimSpeech = new System.Windows.Forms.TextBox();
            this.panelAlternates = new System.Windows.Forms.Panel();
            this.propertyGridResult = new System.Windows.Forms.PropertyGrid();
            this.buttonUpdate = new System.Windows.Forms.Button();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.dataGridViewSymbolItems = new System.Windows.Forms.DataGridView();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBoxHostile = new System.Windows.Forms.CheckBox();
            this.checkBoxFriendly = new System.Windows.Forms.CheckBox();
            this.panelSymbolItems = new System.Windows.Forms.Panel();
            this.dataGridViewAlternates = new System.Windows.Forms.DataGridView();
            this.FullDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Confidence = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.panel1 = new System.Windows.Forms.Panel();
            this.Type = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Designator = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SymbolID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Affiliation = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.pictureMap)).BeginInit();
            this.toolStrip1.SuspendLayout();
            this.groupBoxScenario.SuspendLayout();
            this.panelAudioCapture.SuspendLayout();
            this.panelAlternates.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSymbolItems)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.panelSymbolItems.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAlternates)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStripSplitButton1
            // 
            this.toolStripSplitButton1.Name = "toolStripSplitButton1";
            this.toolStripSplitButton1.Size = new System.Drawing.Size(23, 23);
            // 
            // pictureMap
            // 
            this.pictureMap.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureMap.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureMap.Image = ((System.Drawing.Image)(resources.GetObject("pictureMap.Image")));
            this.pictureMap.InitialImage = ((System.Drawing.Image)(resources.GetObject("pictureMap.InitialImage")));
            this.pictureMap.Location = new System.Drawing.Point(316, 68);
            this.pictureMap.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pictureMap.Name = "pictureMap";
            this.pictureMap.Size = new System.Drawing.Size(696, 389);
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
            this.toolStrip1.Size = new System.Drawing.Size(1019, 25);
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
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(105, 22);
            this.toolStripLabel2.Text = "Connection string:";
            // 
            // toolStripTextBoxStpUri
            // 
            this.toolStripTextBoxStpUri.Margin = new System.Windows.Forms.Padding(1, 1, 5, 0);
            this.toolStripTextBoxStpUri.Name = "toolStripTextBoxStpUri";
            this.toolStripTextBoxStpUri.Size = new System.Drawing.Size(200, 24);
            this.toolStripTextBoxStpUri.Text = "localhost:9555";
            // 
            // toolStripButtonConnect
            // 
            this.toolStripButtonConnect.BackColor = System.Drawing.SystemColors.ButtonShadow;
            this.toolStripButtonConnect.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButtonConnect.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonConnect.Image")));
            this.toolStripButtonConnect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonConnect.Margin = new System.Windows.Forms.Padding(0, 2, 0, 1);
            this.toolStripButtonConnect.Name = "toolStripButtonConnect";
            this.toolStripButtonConnect.Size = new System.Drawing.Size(56, 22);
            this.toolStripButtonConnect.Text = "Connect";
            this.toolStripButtonConnect.Click += new System.EventHandler(this.toolStripButtonConnect_Click);
            // 
            // textBoxLog
            // 
            this.textBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLog.Location = new System.Drawing.Point(2, 785);
            this.textBoxLog.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ReadOnly = true;
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLog.Size = new System.Drawing.Size(1010, 146);
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
            this.groupBoxScenario.Location = new System.Drawing.Point(315, 463);
            this.groupBoxScenario.Name = "groupBoxScenario";
            this.groupBoxScenario.Size = new System.Drawing.Size(373, 46);
            this.groupBoxScenario.TabIndex = 14;
            this.groupBoxScenario.TabStop = false;
            this.groupBoxScenario.Text = "Scenario";
            // 
            // buttonSaveData
            // 
            this.buttonSaveData.Location = new System.Drawing.Point(186, 17);
            this.buttonSaveData.Name = "buttonSaveData";
            this.buttonSaveData.Size = new System.Drawing.Size(84, 23);
            this.buttonSaveData.TabIndex = 27;
            this.buttonSaveData.Text = "Save";
            this.buttonSaveData.Click += new System.EventHandler(this.buttonScenarioSave_Click);
            // 
            // buttonScenarioLoad
            // 
            this.buttonScenarioLoad.Location = new System.Drawing.Point(276, 17);
            this.buttonScenarioLoad.Name = "buttonScenarioLoad";
            this.buttonScenarioLoad.Size = new System.Drawing.Size(84, 23);
            this.buttonScenarioLoad.TabIndex = 2;
            this.buttonScenarioLoad.Text = "Load";
            this.buttonScenarioLoad.Click += new System.EventHandler(this.buttonScenarioLoad_Click);
            // 
            // buttonScenarioNew
            // 
            this.buttonScenarioNew.Location = new System.Drawing.Point(6, 16);
            this.buttonScenarioNew.Name = "buttonScenarioNew";
            this.buttonScenarioNew.Size = new System.Drawing.Size(84, 23);
            this.buttonScenarioNew.TabIndex = 3;
            this.buttonScenarioNew.Text = "New";
            this.buttonScenarioNew.Click += new System.EventHandler(this.buttonScenarioNew_Click);
            // 
            // buttonScenarioJoin
            // 
            this.buttonScenarioJoin.Location = new System.Drawing.Point(96, 16);
            this.buttonScenarioJoin.Name = "buttonScenarioJoin";
            this.buttonScenarioJoin.Size = new System.Drawing.Size(84, 23);
            this.buttonScenarioJoin.TabIndex = 4;
            this.buttonScenarioJoin.Text = "Join";
            this.buttonScenarioJoin.Click += new System.EventHandler(this.buttonScenarioJoin_Click);
            // 
            // panelAudioCapture
            // 
            this.panelAudioCapture.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelAudioCapture.Controls.Add(this.txtSimSpeech);
            this.panelAudioCapture.Location = new System.Drawing.Point(313, 33);
            this.panelAudioCapture.Name = "panelAudioCapture";
            this.panelAudioCapture.Size = new System.Drawing.Size(701, 32);
            this.panelAudioCapture.TabIndex = 21;
            // 
            // txtSimSpeech
            // 
            this.txtSimSpeech.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSimSpeech.Location = new System.Drawing.Point(6, 4);
            this.txtSimSpeech.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtSimSpeech.Multiline = true;
            this.txtSimSpeech.Name = "txtSimSpeech";
            this.txtSimSpeech.Size = new System.Drawing.Size(689, 24);
            this.txtSimSpeech.TabIndex = 16;
            // 
            // panelAlternates
            // 
            this.panelAlternates.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.panelAlternates.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelAlternates.Controls.Add(this.propertyGridResult);
            this.panelAlternates.Controls.Add(this.buttonUpdate);
            this.panelAlternates.Controls.Add(this.buttonDelete);
            this.panelAlternates.Location = new System.Drawing.Point(1, 465);
            this.panelAlternates.Name = "panelAlternates";
            this.panelAlternates.Size = new System.Drawing.Size(309, 318);
            this.panelAlternates.TabIndex = 17;
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
            this.propertyGridResult.Size = new System.Drawing.Size(310, 286);
            this.propertyGridResult.TabIndex = 21;
            this.propertyGridResult.ToolbarVisible = false;
            // 
            // buttonUpdate
            // 
            this.buttonUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonUpdate.Location = new System.Drawing.Point(136, 291);
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Size = new System.Drawing.Size(75, 23);
            this.buttonUpdate.TabIndex = 23;
            this.buttonUpdate.Text = "Update";
            this.buttonUpdate.UseVisualStyleBackColor = true;
            // 
            // buttonDelete
            // 
            this.buttonDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonDelete.Location = new System.Drawing.Point(229, 291);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(75, 23);
            this.buttonDelete.TabIndex = 22;
            this.buttonDelete.Text = "Delete";
            this.buttonDelete.UseVisualStyleBackColor = true;
            // 
            // dataGridViewSymbolItems
            // 
            this.dataGridViewSymbolItems.AllowUserToAddRows = false;
            this.dataGridViewSymbolItems.AllowUserToDeleteRows = false;
            this.dataGridViewSymbolItems.AllowUserToOrderColumns = true;
            this.dataGridViewSymbolItems.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewSymbolItems.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
            this.dataGridViewSymbolItems.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewSymbolItems.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Type,
            this.Description,
            this.Designator,
            this.SymbolID,
            this.Affiliation});
            this.dataGridViewSymbolItems.Location = new System.Drawing.Point(0, 0);
            this.dataGridViewSymbolItems.Name = "dataGridViewSymbolItems";
            this.dataGridViewSymbolItems.ReadOnly = true;
            this.dataGridViewSymbolItems.RowHeadersWidth = 24;
            this.dataGridViewSymbolItems.RowTemplate.Height = 25;
            this.dataGridViewSymbolItems.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridViewSymbolItems.ShowEditingIcon = false;
            this.dataGridViewSymbolItems.Size = new System.Drawing.Size(695, 268);
            this.dataGridViewSymbolItems.TabIndex = 23;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.checkBoxHostile);
            this.groupBox1.Controls.Add(this.checkBoxFriendly);
            this.groupBox1.Location = new System.Drawing.Point(697, 465);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(315, 46);
            this.groupBox1.TabIndex = 24;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Filters";
            // 
            // checkBoxHostile
            // 
            this.checkBoxHostile.AutoSize = true;
            this.checkBoxHostile.Checked = true;
            this.checkBoxHostile.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxHostile.Location = new System.Drawing.Point(196, 19);
            this.checkBoxHostile.Name = "checkBoxHostile";
            this.checkBoxHostile.Size = new System.Drawing.Size(63, 19);
            this.checkBoxHostile.TabIndex = 1;
            this.checkBoxHostile.Text = "Hostile";
            this.checkBoxHostile.UseVisualStyleBackColor = true;
            this.checkBoxHostile.CheckedChanged += new System.EventHandler(this.CheckBoxAffiliationFilter_CheckedChanged);
            // 
            // checkBoxFriendly
            // 
            this.checkBoxFriendly.AutoSize = true;
            this.checkBoxFriendly.Checked = true;
            this.checkBoxFriendly.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxFriendly.Location = new System.Drawing.Point(57, 19);
            this.checkBoxFriendly.Name = "checkBoxFriendly";
            this.checkBoxFriendly.Size = new System.Drawing.Size(68, 19);
            this.checkBoxFriendly.TabIndex = 0;
            this.checkBoxFriendly.Text = "Friendly";
            this.checkBoxFriendly.UseVisualStyleBackColor = true;
            this.checkBoxFriendly.CheckedChanged += new System.EventHandler(this.CheckBoxAffiliationFilter_CheckedChanged);
            // 
            // panelSymbolItems
            // 
            this.panelSymbolItems.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelSymbolItems.Controls.Add(this.dataGridViewSymbolItems);
            this.panelSymbolItems.Location = new System.Drawing.Point(315, 515);
            this.panelSymbolItems.Name = "panelSymbolItems";
            this.panelSymbolItems.Size = new System.Drawing.Size(697, 270);
            this.panelSymbolItems.TabIndex = 25;
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
            this.dataGridViewAlternates.Size = new System.Drawing.Size(309, 424);
            this.dataGridViewAlternates.TabIndex = 14;
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
            dataGridViewCellStyle1.Format = "N2";
            dataGridViewCellStyle1.NullValue = null;
            this.Confidence.DefaultCellStyle = dataGridViewCellStyle1;
            this.Confidence.HeaderText = "Conf";
            this.Confidence.Name = "Confidence";
            this.Confidence.ReadOnly = true;
            this.Confidence.Width = 58;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.dataGridViewAlternates);
            this.panel1.Location = new System.Drawing.Point(2, 35);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(309, 424);
            this.panel1.TabIndex = 22;
            // 
            // Type
            // 
            this.Type.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.Type.HeaderText = "Type";
            this.Type.Name = "Type";
            this.Type.ReadOnly = true;
            this.Type.Width = 57;
            // 
            // Description
            // 
            this.Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Description.HeaderText = "Description";
            this.Description.Name = "Description";
            this.Description.ReadOnly = true;
            // 
            // Designator
            // 
            this.Designator.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.Designator.HeaderText = "Desig";
            this.Designator.Name = "Designator";
            this.Designator.ReadOnly = true;
            this.Designator.Width = 61;
            // 
            // SymbolID
            // 
            this.SymbolID.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.SymbolID.HeaderText = "SIDC";
            this.SymbolID.Name = "SymbolID";
            this.SymbolID.ReadOnly = true;
            this.SymbolID.Width = 57;
            // 
            // Affiliation
            // 
            this.Affiliation.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.Affiliation.HeaderText = "Affiliation";
            this.Affiliation.Name = "Affiliation";
            this.Affiliation.ReadOnly = true;
            this.Affiliation.Width = 84;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1019, 932);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panelSymbolItems);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.panelAlternates);
            this.Controls.Add(this.groupBoxScenario);
            this.Controls.Add(this.pictureMap);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.panelAudioCapture);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "Form1";
            this.Text = "Sketch-Thru-Plan SDK Reactive  Sample";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureMap)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.groupBoxScenario.ResumeLayout(false);
            this.panelAudioCapture.ResumeLayout(false);
            this.panelAudioCapture.PerformLayout();
            this.panelAlternates.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSymbolItems)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.panelSymbolItems.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAlternates)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion
    private System.Windows.Forms.ToolStripSplitButton toolStripSplitButton1;
    private PictureBox pictureMap;
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
    private DataGridView dataGridViewSymbolItems;
    private GroupBox groupBox1;
    private CheckBox checkBoxHostile;
    private CheckBox checkBoxFriendly;
    private Panel panelSymbolItems;
    private DataGridView dataGridViewAlternates;
    private DataGridViewTextBoxColumn FullDescription;
    private DataGridViewTextBoxColumn Confidence;
    private Panel panel1;
    private TextBox txtSimSpeech;
    private DataGridViewTextBoxColumn Type;
    private DataGridViewTextBoxColumn Description;
    private DataGridViewTextBoxColumn Designator;
    private DataGridViewTextBoxColumn SymbolID;
    private DataGridViewTextBoxColumn Affiliation;
}

