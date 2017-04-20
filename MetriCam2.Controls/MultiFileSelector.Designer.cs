namespace MetriCam2.Controls
{
    partial class MultiFileSelector
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.listBoxSelectedFiles = new System.Windows.Forms.ListBox();
            this.buttonSelectFiles = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listBoxSelectedFiles
            // 
            this.listBoxSelectedFiles.Dock = System.Windows.Forms.DockStyle.Right;
            this.listBoxSelectedFiles.FormattingEnabled = true;
            this.listBoxSelectedFiles.Location = new System.Drawing.Point(26, 0);
            this.listBoxSelectedFiles.Margin = new System.Windows.Forms.Padding(0);
            this.listBoxSelectedFiles.Name = "listBoxSelectedFiles";
            this.listBoxSelectedFiles.SelectionMode = System.Windows.Forms.SelectionMode.None;
            this.listBoxSelectedFiles.Size = new System.Drawing.Size(276, 64);
            this.listBoxSelectedFiles.TabIndex = 0;
            // 
            // buttonSelectFiles
            // 
            this.buttonSelectFiles.Dock = System.Windows.Forms.DockStyle.Left;
            this.buttonSelectFiles.Location = new System.Drawing.Point(0, 0);
            this.buttonSelectFiles.Margin = new System.Windows.Forms.Padding(0);
            this.buttonSelectFiles.Name = "buttonSelectFiles";
            this.buttonSelectFiles.Size = new System.Drawing.Size(26, 64);
            this.buttonSelectFiles.TabIndex = 1;
            this.buttonSelectFiles.Text = "...";
            this.buttonSelectFiles.UseVisualStyleBackColor = true;
            this.buttonSelectFiles.Click += new System.EventHandler(this.buttonSelectFiles_Click);
            // 
            // MultiFileSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonSelectFiles);
            this.Controls.Add(this.listBoxSelectedFiles);
            this.Name = "MultiFileSelector";
            this.Size = new System.Drawing.Size(302, 64);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxSelectedFiles;
        private System.Windows.Forms.Button buttonSelectFiles;
    }
}
