namespace StressTests.Sync_Display
{
    partial class GUI
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GUI));
            this.menu1 = new MetriGUIComponents.Menu();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusBar1 = new MetriGUIComponents.StatusBar();
            this.sidebar1 = new MetriGUIComponents.Sidebar();
            this.sidebarBox1 = new MetriGUIComponents.SidebarBox();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonRun = new System.Windows.Forms.Button();
            this.buttonCalibrate = new System.Windows.Forms.Button();
            this.panelBlack1 = new System.Windows.Forms.Panel();
            this.floatPanelVis = new MetriGUI2D.ImageControls.FloatImageOverlayBox();
            this.labelTicks = new System.Windows.Forms.Label();
            this.menu1.SuspendLayout();
            this.sidebar1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sidebarBox1)).BeginInit();
            this.sidebarBox1.Content.SuspendLayout();
            this.sidebarBox1.SuspendLayout();
            this.panelBlack1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.floatPanelVis)).BeginInit();
            this.SuspendLayout();
            // 
            // menu1
            // 
            this.menu1.ForeColor = System.Drawing.Color.White;
            this.menu1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menu1.Location = new System.Drawing.Point(0, 0);
            this.menu1.Name = "menu1";
            this.menu1.Size = new System.Drawing.Size(1360, 24);
            this.menu1.TabIndex = 0;
            this.menu1.Text = "menu1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(92, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // statusBar1
            // 
            this.statusBar1.ForeColor = System.Drawing.Color.White;
            this.statusBar1.Location = new System.Drawing.Point(0, 722);
            this.statusBar1.Name = "statusBar1";
            this.statusBar1.Size = new System.Drawing.Size(1360, 22);
            this.statusBar1.TabIndex = 1;
            this.statusBar1.Text = "statusBar1";
            // 
            // sidebar1
            // 
            this.sidebar1.Controls.Add(this.sidebarBox1);
            this.sidebar1.Dock = System.Windows.Forms.DockStyle.Left;
            this.sidebar1.Location = new System.Drawing.Point(0, 0);
            this.sidebar1.Name = "sidebar1";
            this.sidebar1.SidebarBoxes.Add(this.sidebarBox1);
            this.sidebar1.Size = new System.Drawing.Size(300, 698);
            this.sidebar1.TabIndex = 0;
            // 
            // sidebarBox1
            // 
            this.sidebarBox1.BackColor = System.Drawing.Color.Transparent;
            // 
            // sidebarBox1.Content
            // 
            this.sidebarBox1.Content.BackColor = System.Drawing.Color.Transparent;
            this.sidebarBox1.Content.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.sidebarBox1.Content.Controls.Add(this.labelTicks);
            this.sidebarBox1.Content.Controls.Add(this.buttonStop);
            this.sidebarBox1.Content.Controls.Add(this.buttonRun);
            this.sidebarBox1.Content.Controls.Add(this.buttonCalibrate);
            this.sidebarBox1.Content.Location = new System.Drawing.Point(0, 34);
            this.sidebarBox1.Content.Name = "Content";
            this.sidebarBox1.Content.Size = new System.Drawing.Size(299, 262);
            this.sidebarBox1.Content.TabIndex = 3;
            this.sidebarBox1.ContentHeight = 0;
            this.sidebarBox1.IsCollapsed = false;
            this.sidebarBox1.IsCollapsible = false;
            this.sidebarBox1.Location = new System.Drawing.Point(0, 0);
            this.sidebarBox1.Name = "sidebarBox1";
            this.sidebarBox1.Size = new System.Drawing.Size(299, 296);
            this.sidebarBox1.TabIndex = 0;
            this.sidebarBox1.Title = "Control";
            // 
            // button3
            // 
            this.buttonStop.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStop.ForeColor = System.Drawing.Color.Black;
            this.buttonStop.Location = new System.Drawing.Point(194, 4);
            this.buttonStop.Name = "button3";
            this.buttonStop.Size = new System.Drawing.Size(85, 63);
            this.buttonStop.TabIndex = 2;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // buttonRun
            // 
            this.buttonRun.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonRun.ForeColor = System.Drawing.Color.Black;
            this.buttonRun.Location = new System.Drawing.Point(103, 4);
            this.buttonRun.Name = "buttonRun";
            this.buttonRun.Size = new System.Drawing.Size(85, 63);
            this.buttonRun.TabIndex = 1;
            this.buttonRun.Text = "Run!";
            this.buttonRun.UseVisualStyleBackColor = true;
            this.buttonRun.Click += new System.EventHandler(this.buttonRun_Click);
            // 
            // buttonCalibrate
            // 
            this.buttonCalibrate.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCalibrate.ForeColor = System.Drawing.Color.Black;
            this.buttonCalibrate.Location = new System.Drawing.Point(12, 4);
            this.buttonCalibrate.Name = "buttonCalibrate";
            this.buttonCalibrate.Size = new System.Drawing.Size(85, 63);
            this.buttonCalibrate.TabIndex = 0;
            this.buttonCalibrate.Text = "Calibration";
            this.buttonCalibrate.UseVisualStyleBackColor = true;
            this.buttonCalibrate.Click += new System.EventHandler(this.buttonCalibrate_Click);
            // 
            // panelBlack1
            // 
            this.panelBlack1.BackColor = System.Drawing.Color.Black;
            this.panelBlack1.Controls.Add(this.floatPanelVis);
            this.panelBlack1.Controls.Add(this.sidebar1);
            this.panelBlack1.Name = "panelBlack1";
            this.panelBlack1.Size = new System.Drawing.Size(1360, 698);
            this.panelBlack1.TabIndex = 3;
            // 
            // floatPanelVis
            // 
            this.floatPanelVis.AutoAdjustIntensityWindowCenter = true;
            this.floatPanelVis.BackColor = System.Drawing.Color.Transparent;
            this.floatPanelVis.BlueImage = null;
            this.floatPanelVis.ColorTable = null;
            this.floatPanelVis.CurrentSelectionMode = MetriGUI2D.ImageControls.FloatImageBox.SelectionMode.NoSelection;
            this.floatPanelVis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.floatPanelVis.EnableMouseIntensityWindowing = true;
            this.floatPanelVis.GreenImage = null;
            this.floatPanelVis.ImageData = null;
            this.floatPanelVis.Interpolation = System.Drawing.Drawing2D.InterpolationMode.Default;
            this.floatPanelVis.Location = new System.Drawing.Point(300, 0);
            this.floatPanelVis.Name = "floatPanelVis";
            this.floatPanelVis.OpacityBlue = 1F;
            this.floatPanelVis.OpacityGreen = 1F;
            this.floatPanelVis.OpacityRed = 1F;
            this.floatPanelVis.OverrideBitmapCreation = false;
            this.floatPanelVis.OverwriteLastObjectOnNewObjectCreation = false;
            this.floatPanelVis.RedImage = null;
            this.floatPanelVis.Size = new System.Drawing.Size(1060, 698);
            this.floatPanelVis.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.floatPanelVis.TabIndex = 1;
            this.floatPanelVis.TabStop = false;
            this.floatPanelVis.Transformation = MetriPrimitives.Transformations.ImageTranformations.None;
            // 
            // labelTicks
            // 
            this.labelTicks.AutoSize = true;
            this.labelTicks.Font = new System.Drawing.Font("Segoe UI", 30F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTicks.Location = new System.Drawing.Point(38, 164);
            this.labelTicks.Name = "labelTicks";
            this.labelTicks.Size = new System.Drawing.Size(218, 54);
            this.labelTicks.TabIndex = 3;
            this.labelTicks.Text = "Timestamp";
            // 
            // GUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1360, 744);
            this.Controls.Add(this.panelBlack1);
            this.Controls.Add(this.statusBar1);
            this.Controls.Add(this.menu1);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menu1;
            this.Name = "GUI";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "(Fill in your application title here.)";
            this.Load += new System.EventHandler(this.GUI_Load);
            this.menu1.ResumeLayout(false);
            this.menu1.PerformLayout();
            this.sidebar1.ResumeLayout(false);
            this.sidebarBox1.Content.ResumeLayout(false);
            this.sidebarBox1.Content.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sidebarBox1)).EndInit();
            this.sidebarBox1.ResumeLayout(false);
            this.panelBlack1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.floatPanelVis)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MetriGUIComponents.Menu menu1;
        private MetriGUIComponents.StatusBar statusBar1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private MetriGUIComponents.Sidebar sidebar1;
        private MetriGUIComponents.SidebarBox sidebarBox1;
        private System.Windows.Forms.Button buttonRun;
        private System.Windows.Forms.Button buttonCalibrate;
        private System.Windows.Forms.Panel panelBlack1;
        private MetriGUI2D.ImageControls.FloatImageOverlayBox floatPanelVis;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Label labelTicks;
    }
}

