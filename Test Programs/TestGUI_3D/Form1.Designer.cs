namespace TestGUI_3D
{
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
            this.pictureBoxSecondImage = new System.Windows.Forms.PictureBox();
            this.pictureBoxImageStream = new System.Windows.Forms.PictureBox();
            this.buttonConnect = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.backgroundWorkerGetFrames = new System.ComponentModel.BackgroundWorker();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSecondImage)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImageStream)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBoxSecondImage
            // 
            this.pictureBoxSecondImage.Location = new System.Drawing.Point(12, 282);
            this.pictureBoxSecondImage.Name = "pictureBoxSecondImage";
            this.pictureBoxSecondImage.Size = new System.Drawing.Size(320, 240);
            this.pictureBoxSecondImage.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxSecondImage.TabIndex = 7;
            this.pictureBoxSecondImage.TabStop = false;
            // 
            // pictureBoxImageStream
            // 
            this.pictureBoxImageStream.Location = new System.Drawing.Point(12, 42);
            this.pictureBoxImageStream.Name = "pictureBoxImageStream";
            this.pictureBoxImageStream.Size = new System.Drawing.Size(320, 240);
            this.pictureBoxImageStream.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBoxImageStream.TabIndex = 6;
            this.pictureBoxImageStream.TabStop = false;
            // 
            // buttonConnect
            // 
            this.buttonConnect.Location = new System.Drawing.Point(12, 12);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(142, 23);
            this.buttonConnect.TabIndex = 5;
            this.buttonConnect.Text = "Connect";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.buttonConnect_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.Location = new System.Drawing.Point(332, 42);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(640, 480);
            this.panel1.TabIndex = 8;
            // 
            // backgroundWorkerGetFrames
            // 
            this.backgroundWorkerGetFrames.WorkerSupportsCancellation = true;
            this.backgroundWorkerGetFrames.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerGetFrames_DoWork);
            this.backgroundWorkerGetFrames.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerGetFrames_RunWorkerCompleted);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(994, 537);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.pictureBoxSecondImage);
            this.Controls.Add(this.pictureBoxImageStream);
            this.Controls.Add(this.buttonConnect);
            this.Name = "Form1";
            this.Text = "TestGUI_3D";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxSecondImage)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImageStream)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBoxSecondImage;
        private System.Windows.Forms.PictureBox pictureBoxImageStream;
        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.Panel panel1;
        private System.ComponentModel.BackgroundWorker backgroundWorkerGetFrames;
    }
}

