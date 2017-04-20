namespace TestGUI_2
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
			this.bgWorkerGetFrames = new System.ComponentModel.BackgroundWorker();
			this.pictureBoxTL = new System.Windows.Forms.PictureBox();
			this.pictureBoxBL = new System.Windows.Forms.PictureBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panelContainerLeft = new System.Windows.Forms.Panel();
			this.panelBL = new System.Windows.Forms.Panel();
			this.panelTL = new System.Windows.Forms.Panel();
			this.panelContainerRight = new System.Windows.Forms.Panel();
			this.panelBR = new System.Windows.Forms.Panel();
			this.pictureBoxBR = new System.Windows.Forms.PictureBox();
			this.panelTR = new System.Windows.Forms.Panel();
			this.pictureBoxTR = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxTL)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxBL)).BeginInit();
			this.panelContainerLeft.SuspendLayout();
			this.panelBL.SuspendLayout();
			this.panelTL.SuspendLayout();
			this.panelContainerRight.SuspendLayout();
			this.panelBR.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxBR)).BeginInit();
			this.panelTR.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxTR)).BeginInit();
			this.SuspendLayout();
			// 
			// bgWorkerGetFrames
			// 
			this.bgWorkerGetFrames.WorkerSupportsCancellation = true;
			this.bgWorkerGetFrames.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bgWorkerGetFrames_DoWork);
			this.bgWorkerGetFrames.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bgWorkerGetFrames_RunWorkerCompleted);
			// 
			// pictureBoxTL
			// 
			this.pictureBoxTL.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBoxTL.Location = new System.Drawing.Point(0, 0);
			this.pictureBoxTL.Name = "pictureBoxTL";
			this.pictureBoxTL.Size = new System.Drawing.Size(168, 287);
			this.pictureBoxTL.TabIndex = 0;
			this.pictureBoxTL.TabStop = false;
			// 
			// pictureBoxBL
			// 
			this.pictureBoxBL.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBoxBL.Location = new System.Drawing.Point(0, 0);
			this.pictureBoxBL.Name = "pictureBoxBL";
			this.pictureBoxBL.Size = new System.Drawing.Size(168, 280);
			this.pictureBoxBL.TabIndex = 1;
			this.pictureBoxBL.TabStop = false;
			// 
			// panel1
			// 
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panel1.Location = new System.Drawing.Point(0, 577);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(1047, 100);
			this.panel1.TabIndex = 2;
			// 
			// panelContainerLeft
			// 
			this.panelContainerLeft.Controls.Add(this.panelBL);
			this.panelContainerLeft.Controls.Add(this.panelTL);
			this.panelContainerLeft.Dock = System.Windows.Forms.DockStyle.Left;
			this.panelContainerLeft.Location = new System.Drawing.Point(0, 0);
			this.panelContainerLeft.Name = "panelContainerLeft";
			this.panelContainerLeft.Size = new System.Drawing.Size(170, 577);
			this.panelContainerLeft.TabIndex = 3;
			// 
			// panelBL
			// 
			this.panelBL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelBL.Controls.Add(this.pictureBoxBL);
			this.panelBL.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelBL.Location = new System.Drawing.Point(0, 295);
			this.panelBL.Name = "panelBL";
			this.panelBL.Size = new System.Drawing.Size(170, 282);
			this.panelBL.TabIndex = 2;
			// 
			// panelTL
			// 
			this.panelTL.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelTL.Controls.Add(this.pictureBoxTL);
			this.panelTL.Dock = System.Windows.Forms.DockStyle.Top;
			this.panelTL.Location = new System.Drawing.Point(0, 0);
			this.panelTL.Name = "panelTL";
			this.panelTL.Size = new System.Drawing.Size(170, 289);
			this.panelTL.TabIndex = 1;
			// 
			// panelContainerRight
			// 
			this.panelContainerRight.Controls.Add(this.panelBR);
			this.panelContainerRight.Controls.Add(this.panelTR);
			this.panelContainerRight.Dock = System.Windows.Forms.DockStyle.Right;
			this.panelContainerRight.Location = new System.Drawing.Point(865, 0);
			this.panelContainerRight.Name = "panelContainerRight";
			this.panelContainerRight.Size = new System.Drawing.Size(182, 577);
			this.panelContainerRight.TabIndex = 4;
			// 
			// panelBR
			// 
			this.panelBR.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelBR.Controls.Add(this.pictureBoxBR);
			this.panelBR.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.panelBR.Location = new System.Drawing.Point(0, 295);
			this.panelBR.Name = "panelBR";
			this.panelBR.Size = new System.Drawing.Size(182, 282);
			this.panelBR.TabIndex = 1;
			// 
			// pictureBoxBR
			// 
			this.pictureBoxBR.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBoxBR.Location = new System.Drawing.Point(0, 0);
			this.pictureBoxBR.Name = "pictureBoxBR";
			this.pictureBoxBR.Size = new System.Drawing.Size(180, 280);
			this.pictureBoxBR.TabIndex = 2;
			this.pictureBoxBR.TabStop = false;
			// 
			// panelTR
			// 
			this.panelTR.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panelTR.Controls.Add(this.pictureBoxTR);
			this.panelTR.Dock = System.Windows.Forms.DockStyle.Top;
			this.panelTR.Location = new System.Drawing.Point(0, 0);
			this.panelTR.Name = "panelTR";
			this.panelTR.Size = new System.Drawing.Size(182, 289);
			this.panelTR.TabIndex = 0;
			// 
			// pictureBoxTR
			// 
			this.pictureBoxTR.Dock = System.Windows.Forms.DockStyle.Fill;
			this.pictureBoxTR.Location = new System.Drawing.Point(0, 0);
			this.pictureBoxTR.Name = "pictureBoxTR";
			this.pictureBoxTR.Size = new System.Drawing.Size(180, 287);
			this.pictureBoxTR.TabIndex = 1;
			this.pictureBoxTR.TabStop = false;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1047, 677);
			this.Controls.Add(this.panelContainerRight);
			this.Controls.Add(this.panelContainerLeft);
			this.Controls.Add(this.panel1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "Form1";
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.Form1_Load);
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxTL)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxBL)).EndInit();
			this.panelContainerLeft.ResumeLayout(false);
			this.panelBL.ResumeLayout(false);
			this.panelTL.ResumeLayout(false);
			this.panelContainerRight.ResumeLayout(false);
			this.panelBR.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxBR)).EndInit();
			this.panelTR.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBoxTR)).EndInit();
			this.ResumeLayout(false);

        }

        #endregion

        private System.ComponentModel.BackgroundWorker bgWorkerGetFrames;
        private System.Windows.Forms.PictureBox pictureBoxTL;
        private System.Windows.Forms.PictureBox pictureBoxBL;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panelContainerLeft;
        private System.Windows.Forms.Panel panelBL;
        private System.Windows.Forms.Panel panelTL;
        private System.Windows.Forms.Panel panelContainerRight;
        private System.Windows.Forms.Panel panelBR;
        private System.Windows.Forms.PictureBox pictureBoxBR;
        private System.Windows.Forms.Panel panelTR;
        private System.Windows.Forms.PictureBox pictureBoxTR;
    }
}

