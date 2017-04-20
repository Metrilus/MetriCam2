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
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonApply = new System.Windows.Forms.Button();
            this.labelSettings = new System.Windows.Forms.Label();
            this.labelChannels = new System.Windows.Forms.Label();
            this.checkedListBoxChannels = new System.Windows.Forms.CheckedListBox();
            this.cameraSettingsControl = new MetriCam2.Controls.CameraSettingsControl();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(777, 586);
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
            this.buttonOK.Location = new System.Drawing.Point(615, 586);
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
            this.buttonApply.Location = new System.Drawing.Point(696, 586);
            this.buttonApply.Name = "buttonApply";
            this.buttonApply.Size = new System.Drawing.Size(75, 29);
            this.buttonApply.TabIndex = 5;
            this.buttonApply.Text = "&Apply";
            this.buttonApply.UseVisualStyleBackColor = true;
            this.buttonApply.Click += new System.EventHandler(this.buttonApply_Click);
            // 
            // labelSettings
            // 
            this.labelSettings.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelSettings.AutoSize = true;
            this.labelSettings.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSettings.Location = new System.Drawing.Point(12, 120);
            this.labelSettings.Name = "labelSettings";
            this.labelSettings.Size = new System.Drawing.Size(124, 21);
            this.labelSettings.TabIndex = 6;
            this.labelSettings.Text = "Camera Settings";
            // 
            // labelChannels
            // 
            this.labelChannels.AutoSize = true;
            this.labelChannels.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelChannels.Location = new System.Drawing.Point(12, 9);
            this.labelChannels.Name = "labelChannels";
            this.labelChannels.Size = new System.Drawing.Size(74, 21);
            this.labelChannels.TabIndex = 7;
            this.labelChannels.Text = "Channels";
            // 
            // checkedListBoxChannels
            // 
            this.checkedListBoxChannels.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.checkedListBoxChannels.FormattingEnabled = true;
            this.checkedListBoxChannels.Items.AddRange(new object[] {
            "Not implemented, yet."});
            this.checkedListBoxChannels.Location = new System.Drawing.Point(16, 32);
            this.checkedListBoxChannels.Name = "checkedListBoxChannels";
            this.checkedListBoxChannels.Size = new System.Drawing.Size(836, 64);
            this.checkedListBoxChannels.TabIndex = 9;
            // 
            // cameraSettingsControl
            // 
            this.cameraSettingsControl.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.cameraSettingsControl.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cameraSettingsControl.Camera = null;
            this.cameraSettingsControl.ContainsOneOrMoreWritableParameters = false;
            this.cameraSettingsControl.HeadingFont = new System.Drawing.Font("Segoe UI", 8.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))));
            this.cameraSettingsControl.LabelFont = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cameraSettingsControl.Location = new System.Drawing.Point(16, 144);
            this.cameraSettingsControl.Name = "cameraSettingsControl";
            this.cameraSettingsControl.Size = new System.Drawing.Size(840, 437);
            this.cameraSettingsControl.TabIndex = 0;
            this.cameraSettingsControl.TextColor = System.Drawing.Color.Black;
            this.cameraSettingsControl.VisibleParameters = null;
            // 
            // CameraConfigurationDialog
            // 
            this.ClientSize = new System.Drawing.Size(868, 627);
            this.Controls.Add(this.checkedListBoxChannels);
            this.Controls.Add(this.labelChannels);
            this.Controls.Add(this.labelSettings);
            this.Controls.Add(this.buttonApply);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.cameraSettingsControl);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "CameraConfigurationDialog";
            this.Text = "Camera Configuration";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.CameraConfigurationDialog_FormClosed);
            this.Load += new System.EventHandler(this.CameraConfigurationDialog_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.CameraConfigurationDialog_KeyDown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private CameraSettingsControl cameraSettingsControl;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonApply;
        private System.Windows.Forms.Label labelSettings;
        private System.Windows.Forms.Label labelChannels;
        private System.Windows.Forms.CheckedListBox checkedListBoxChannels;
    }
}