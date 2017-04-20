namespace TestGUI
{
    partial class TestGUIForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TestGUIForm));
            this.buttonConnect = new System.Windows.Forms.Button();
            this.buttonCaptureProperties = new System.Windows.Forms.Button();
            this.buttonCaptureFilterProperties = new System.Windows.Forms.Button();
            this.pictureBoxImageStream = new System.Windows.Forms.PictureBox();
            this.backgroundWorkerGetFrames = new System.ComponentModel.BackgroundWorker();
            this.pictureBoxSecondImage = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImageStream)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSecondImage)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonConnect
            // 
            this.buttonConnect.Location = new System.Drawing.Point(13, 13);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(142, 23);
            this.buttonConnect.TabIndex = 0;
            this.buttonConnect.Text = "Connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            // 
            // buttonCaptureProperties
            // 
            this.buttonCaptureProperties.Location = new System.Drawing.Point(161, 12);
            this.buttonCaptureProperties.Name = "buttonCaptureProperties";
            this.buttonCaptureProperties.Size = new System.Drawing.Size(142, 23);
            this.buttonCaptureProperties.TabIndex = 1;
            this.buttonCaptureProperties.Text = "Capture Properties...";
            this.buttonCaptureProperties.UseVisualStyleBackColor = true;
            // 
            // buttonCaptureFilterProperties
            // 
            this.buttonCaptureFilterProperties.Location = new System.Drawing.Point(309, 13);
            this.buttonCaptureFilterProperties.Name = "buttonCaptureFilterProperties";
            this.buttonCaptureFilterProperties.Size = new System.Drawing.Size(142, 23);
            this.buttonCaptureFilterProperties.TabIndex = 2;
            this.buttonCaptureFilterProperties.Text = "Capture Filter Properties...";
            this.buttonCaptureFilterProperties.UseVisualStyleBackColor = true;
            // 
            // pictureBoxImageStream
            // 
            this.pictureBoxImageStream.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.pictureBoxImageStream.Location = new System.Drawing.Point(13, 43);
            this.pictureBoxImageStream.Name = "pictureBoxImageStream";
            this.pictureBoxImageStream.Size = new System.Drawing.Size(542, 420);
            this.pictureBoxImageStream.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxImageStream.TabIndex = 3;
            this.pictureBoxImageStream.TabStop = false;
            // 
            // backgroundWorkerGetFrames
            // 
            this.backgroundWorkerGetFrames.WorkerSupportsCancellation = true;
            this.backgroundWorkerGetFrames.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerGetFrames_DoWork);
            this.backgroundWorkerGetFrames.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerGetFrames_RunWorkerCompleted);
            // 
            // pictureBoxSecondImage
            // 
            this.pictureBoxSecondImage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxSecondImage.Location = new System.Drawing.Point(561, 43);
            this.pictureBoxSecondImage.Name = "pictureBoxSecondImage";
            this.pictureBoxSecondImage.Size = new System.Drawing.Size(559, 420);
            this.pictureBoxSecondImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxSecondImage.TabIndex = 4;
            this.pictureBoxSecondImage.TabStop = false;
            // 
            // TestGUIForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1132, 475);
            this.Controls.Add(this.pictureBoxSecondImage);
            this.Controls.Add(this.pictureBoxImageStream);
            this.Controls.Add(this.buttonCaptureFilterProperties);
            this.Controls.Add(this.buttonCaptureProperties);
            this.Controls.Add(this.buttonConnect);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TestGUIForm";
            this.Text = "Camera Test GUI";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImageStream)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSecondImage)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.Button buttonCaptureProperties;
        private System.Windows.Forms.Button buttonCaptureFilterProperties;
        private System.Windows.Forms.PictureBox pictureBoxImageStream;
        private System.ComponentModel.BackgroundWorker backgroundWorkerGetFrames;
        private System.Windows.Forms.PictureBox pictureBoxSecondImage;
    }
}
