namespace MetriCam2.Controls
{
    partial class CameraConfigurationDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CameraConfigurationDialog));
            this.ButtonPanel = new System.Windows.Forms.Panel();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonApply = new System.Windows.Forms.Button();
            this.cameraSettingsControl = new MetriCam2.Controls.CameraSettingsControl();
            this.labelSettings = new System.Windows.Forms.Label();
            this.checkedListBoxChannels = new System.Windows.Forms.CheckedListBox();
            this.labelChannels = new System.Windows.Forms.Label();
            this.SettingsPanel = new System.Windows.Forms.Panel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.ButtonPanel.SuspendLayout();
            this.SettingsPanel.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ButtonPanel
            // 
            this.ButtonPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ButtonPanel.Controls.Add(this.buttonApply);
            this.ButtonPanel.Controls.Add(this.buttonOK);
            this.ButtonPanel.Controls.Add(this.buttonCancel);
            this.ButtonPanel.Location = new System.Drawing.Point(667, 701);
            this.ButtonPanel.Name = "ButtonPanel";
            this.ButtonPanel.Size = new System.Drawing.Size(264, 37);
            this.ButtonPanel.TabIndex = 11;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(181, 4);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 29);
            this.buttonCancel.TabIndex = 4;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(19, 4);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 29);
            this.buttonOK.TabIndex = 3;
            this.buttonOK.Text = "&OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonApply
            // 
            this.buttonApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonApply.Location = new System.Drawing.Point(100, 4);
            this.buttonApply.Name = "buttonApply";
            this.buttonApply.Size = new System.Drawing.Size(75, 29);
            this.buttonApply.TabIndex = 5;
            this.buttonApply.Text = "&Apply";
            this.buttonApply.UseVisualStyleBackColor = true;
            this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
            // 
            // cameraSettingsControl
            // 
            this.cameraSettingsControl.Camera = null;
            this.cameraSettingsControl.ContainsOneOrMoreWritableParameters = false;
            this.cameraSettingsControl.HeadingFont = new System.Drawing.Font("Segoe UI", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))));
            this.cameraSettingsControl.LabelFont = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cameraSettingsControl.Location = new System.Drawing.Point(0, 0);
            this.cameraSettingsControl.Name = "cameraSettingsControl";
            this.cameraSettingsControl.Size = new System.Drawing.Size(851, 498);
            this.cameraSettingsControl.TabIndex = 0;
            this.cameraSettingsControl.TextColor = System.Drawing.Color.Black;
            this.cameraSettingsControl.VisibleParameters = null;
            // 
            // labelSettings
            // 
            this.labelSettings.AutoSize = true;
            this.labelSettings.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSettings.Location = new System.Drawing.Point(10, 143);
            this.labelSettings.Margin = new System.Windows.Forms.Padding(10, 10, 3, 0);
            this.labelSettings.Name = "labelSettings";
            this.labelSettings.Size = new System.Drawing.Size(124, 21);
            this.labelSettings.TabIndex = 6;
            this.labelSettings.Text = "Camera Settings";
            // 
            // checkedListBoxChannels
            // 
            this.checkedListBoxChannels.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.checkedListBoxChannels.Dock = System.Windows.Forms.DockStyle.Fill;
            this.checkedListBoxChannels.FormattingEnabled = true;
            this.checkedListBoxChannels.Items.AddRange(new object[] {
            "Not implemented, yet."});
            this.checkedListBoxChannels.Location = new System.Drawing.Point(10, 41);
            this.checkedListBoxChannels.Margin = new System.Windows.Forms.Padding(10);
            this.checkedListBoxChannels.Name = "checkedListBoxChannels";
            this.checkedListBoxChannels.Size = new System.Drawing.Size(914, 82);
            this.checkedListBoxChannels.TabIndex = 9;
            // 
            // labelChannels
            // 
            this.labelChannels.AutoSize = true;
            this.labelChannels.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelChannels.Location = new System.Drawing.Point(10, 10);
            this.labelChannels.Margin = new System.Windows.Forms.Padding(10, 10, 3, 0);
            this.labelChannels.Name = "labelChannels";
            this.labelChannels.Size = new System.Drawing.Size(74, 21);
            this.labelChannels.TabIndex = 7;
            this.labelChannels.Text = "Channels";
            // 
            // SettingsPanel
            // 
            this.SettingsPanel.AutoScroll = true;
            this.SettingsPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SettingsPanel.Controls.Add(this.cameraSettingsControl);
            this.SettingsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SettingsPanel.Location = new System.Drawing.Point(10, 174);
            this.SettingsPanel.Margin = new System.Windows.Forms.Padding(10, 10, 10, 3);
            this.SettingsPanel.Name = "SettingsPanel";
            this.SettingsPanel.Size = new System.Drawing.Size(914, 506);
            this.SettingsPanel.TabIndex = 10;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.ButtonPanel, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.SettingsPanel, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.checkedListBoxChannels, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.labelSettings, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.labelChannels, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(934, 741);
            this.tableLayoutPanel1.TabIndex = 12;
            // 
            // CameraConfigurationDialog
            // 
            this.ClientSize = new System.Drawing.Size(934, 741);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.MinimumSize = new System.Drawing.Size(500, 500);
            this.Name = "CameraConfigurationDialog";
            this.Text = "Camera Configuration";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.CameraConfigurationDialog_FormClosed);
            this.Load += new System.EventHandler(this.CameraConfigurationDialog_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CameraConfigurationDialog_KeyDown);
            this.ButtonPanel.ResumeLayout(false);
            this.SettingsPanel.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel ButtonPanel;
        private System.Windows.Forms.Button buttonApply;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private CameraSettingsControl cameraSettingsControl;
        private System.Windows.Forms.Label labelSettings;
        private System.Windows.Forms.CheckedListBox checkedListBoxChannels;
        private System.Windows.Forms.Label labelChannels;
        private System.Windows.Forms.Panel SettingsPanel;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}