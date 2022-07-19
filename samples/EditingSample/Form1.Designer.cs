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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.toolStripSplitButton1 = new System.Windows.Forms.ToolStripSplitButton();
            this.txtSimSpeech = new System.Windows.Forms.TextBox();
            this.propertyGridResult = new System.Windows.Forms.PropertyGrid();
            this.pictureMap = new System.Windows.Forms.PictureBox();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripSplitButton2 = new System.Windows.Forms.ToolStripSplitButton();
            this.plaBtn = new System.Windows.Forms.ToolStripMenuItem();
            this.drawBtn = new System.Windows.Forms.ToolStripMenuItem();
            this.tsLabelTiming = new System.Windows.Forms.ToolStripLabel();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.dataGridViewAlternates = new System.Windows.Forms.DataGridView();
            this.FullDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Confidence = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.buttonDelete = new System.Windows.Forms.Button();
            this.stpItemBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.alternatesBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.alternatesBindingSource1 = new System.Windows.Forms.BindingSource(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.panelAudioCapture = new System.Windows.Forms.Panel();
            this.buttonUpdate = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureMap)).BeginInit();
            this.toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAlternates)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.stpItemBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.alternatesBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.alternatesBindingSource1)).BeginInit();
            this.panel1.SuspendLayout();
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
            this.txtSimSpeech.Location = new System.Drawing.Point(318, 437);
            this.txtSimSpeech.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtSimSpeech.Multiline = true;
            this.txtSimSpeech.Name = "txtSimSpeech";
            this.txtSimSpeech.Size = new System.Drawing.Size(622, 23);
            this.txtSimSpeech.TabIndex = 13;
            // 
            // propertyGridResult
            // 
            this.propertyGridResult.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.propertyGridResult.HelpVisible = false;
            this.propertyGridResult.Location = new System.Drawing.Point(3, 435);
            this.propertyGridResult.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.propertyGridResult.Name = "propertyGridResult";
            this.propertyGridResult.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.propertyGridResult.Size = new System.Drawing.Size(309, 227);
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
            this.pictureMap.Location = new System.Drawing.Point(317, 28);
            this.pictureMap.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pictureMap.Name = "pictureMap";
            this.pictureMap.Size = new System.Drawing.Size(718, 403);
            this.pictureMap.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureMap.TabIndex = 11;
            this.pictureMap.TabStop = false;
            // 
            // btnClearLog
            // 
            this.btnClearLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearLog.Location = new System.Drawing.Point(947, 437);
            this.btnClearLog.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(88, 22);
            this.btnClearLog.TabIndex = 10;
            this.btnClearLog.Text = "Clear Log";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.BtnClearLog_Click_1);
            // 
            // toolStrip1
            // 
            this.toolStrip1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripSplitButton2,
            this.tsLabelTiming});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1047, 25);
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
            this.textBoxLog.Location = new System.Drawing.Point(317, 465);
            this.textBoxLog.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ReadOnly = true;
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxLog.Size = new System.Drawing.Size(731, 230);
            this.textBoxLog.TabIndex = 8;
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
            this.dataGridViewAlternates.Size = new System.Drawing.Size(309, 403);
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
            // buttonDelete
            // 
            this.buttonDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonDelete.Location = new System.Drawing.Point(233, 668);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(75, 23);
            this.buttonDelete.TabIndex = 15;
            this.buttonDelete.Text = "Delete";
            this.buttonDelete.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.dataGridViewAlternates);
            this.panel1.Location = new System.Drawing.Point(3, 28);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(309, 403);
            this.panel1.TabIndex = 16;
            // 
            // panelAudioCapture
            // 
            this.panelAudioCapture.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelAudioCapture.Location = new System.Drawing.Point(314, 432);
            this.panelAudioCapture.Name = "panelAudioCapture";
            this.panelAudioCapture.Size = new System.Drawing.Size(631, 32);
            this.panelAudioCapture.TabIndex = 17;
            // 
            // buttonUpdate
            // 
            this.buttonUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonUpdate.Enabled = false;
            this.buttonUpdate.Location = new System.Drawing.Point(128, 668);
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Size = new System.Drawing.Size(75, 23);
            this.buttonUpdate.TabIndex = 18;
            this.buttonUpdate.Text = "Update";
            this.buttonUpdate.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1047, 696);
            this.Controls.Add(this.buttonUpdate);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.buttonDelete);
            this.Controls.Add(this.txtSimSpeech);
            this.Controls.Add(this.propertyGridResult);
            this.Controls.Add(this.pictureMap);
            this.Controls.Add(this.btnClearLog);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.panelAudioCapture);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "Form1";
            this.Text = "Sketch-Thru-Plan SDK Editing Sample";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureMap)).EndInit();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewAlternates)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.stpItemBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.alternatesBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.alternatesBindingSource1)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

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
    private TextBox textBoxLog;
    private DataGridView dataGridViewAlternates;
    private Button buttonDelete;
    private BindingSource stpItemBindingSource;
    private BindingSource alternatesBindingSource;
    private BindingSource alternatesBindingSource1;
    private DataGridViewTextBoxColumn FullDescription;
    private DataGridViewTextBoxColumn Confidence;
    private Panel panel1;
    private Panel panelAudioCapture;
    private Button buttonUpdate;
}

