namespace MetriCam2.Controls
{
    partial class CameraExplorerControl
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
            this.components = new System.ComponentModel.Container();
            this.imageListLarge = new System.Windows.Forms.ImageList(this.components);
            this.imageListSmall = new System.Windows.Forms.ImageList(this.components);
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.listViewAvailable = new System.Windows.Forms.ListView();
            this.columnHeaderTitleAvail = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonAddAssembly = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonChangeView = new System.Windows.Forms.Button();
            this.listViewSelected = new System.Windows.Forms.ListView();
            this.columnHeaderTitle = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderConnectionStatus = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderSerialNumber = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderNumChannels = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.panel2 = new System.Windows.Forms.Panel();
            this.buttonDeselect = new System.Windows.Forms.Button();
            this.buttonSelect = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // imageListLarge
            // 
            this.imageListLarge.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageListLarge.ImageSize = new System.Drawing.Size(64, 64);
            this.imageListLarge.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // imageListSmall
            // 
            this.imageListSmall.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imageListSmall.ImageSize = new System.Drawing.Size(32, 32);
            this.imageListSmall.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listViewAvailable);
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            this.splitContainer1.Panel1.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainer1_Panel1_Paint);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.listViewSelected);
            this.splitContainer1.Panel2.Controls.Add(this.panel2);
            this.splitContainer1.Panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.splitContainer1_Panel2_Paint);
            this.splitContainer1.Size = new System.Drawing.Size(500, 440);
            this.splitContainer1.SplitterDistance = 220;
            this.splitContainer1.TabIndex = 6;
            // 
            // listViewAvailable
            // 
            this.listViewAvailable.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderTitleAvail});
            this.listViewAvailable.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.listViewAvailable.LargeImageList = this.imageListLarge;
            this.listViewAvailable.Location = new System.Drawing.Point(0, 42);
            this.listViewAvailable.Name = "listViewAvailable";
            this.listViewAvailable.Size = new System.Drawing.Size(500, 178);
            this.listViewAvailable.SmallImageList = this.imageListSmall;
            this.listViewAvailable.TabIndex = 4;
            this.listViewAvailable.TileSize = new System.Drawing.Size(64, 64);
            this.listViewAvailable.UseCompatibleStateImageBehavior = false;
            this.listViewAvailable.DoubleClick += new System.EventHandler(this.listViewAvailable_DoubleClick);
            // 
            // columnHeaderTitleAvail
            // 
            this.columnHeaderTitleAvail.Text = "Name";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.buttonAddAssembly);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.buttonChangeView);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(500, 42);
            this.panel1.TabIndex = 3;
            // 
            // buttonAddAssembly
            // 
            this.buttonAddAssembly.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAddAssembly.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonAddAssembly.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAddAssembly.Location = new System.Drawing.Point(431, 6);
            this.buttonAddAssembly.Name = "buttonAddAssembly";
            this.buttonAddAssembly.Size = new System.Drawing.Size(30, 30);
            this.buttonAddAssembly.TabIndex = 11;
            this.buttonAddAssembly.Text = "+";
            this.buttonAddAssembly.UseVisualStyleBackColor = true;
            this.buttonAddAssembly.Click += new System.EventHandler(this.buttonAddAssembly_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(405, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Select or deselect cameras using the up-/down arrows or double-click on their ico" +
    "ns.";
            // 
            // buttonChangeView
            // 
            this.buttonChangeView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonChangeView.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonChangeView.Image = global::MetriCam2.Controls.Properties.Resources.changeView;
            this.buttonChangeView.Location = new System.Drawing.Point(467, 6);
            this.buttonChangeView.Name = "buttonChangeView";
            this.buttonChangeView.Size = new System.Drawing.Size(30, 30);
            this.buttonChangeView.TabIndex = 9;
            this.buttonChangeView.UseVisualStyleBackColor = true;
            this.buttonChangeView.Click += new System.EventHandler(this.buttonChangeView_Click);
            // 
            // listViewSelected
            // 
            this.listViewSelected.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderTitle,
            this.columnHeaderConnectionStatus,
            this.columnHeaderSerialNumber,
            this.columnHeaderNumChannels});
            this.listViewSelected.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.listViewSelected.LargeImageList = this.imageListLarge;
            this.listViewSelected.Location = new System.Drawing.Point(0, 42);
            this.listViewSelected.Name = "listViewSelected";
            this.listViewSelected.Size = new System.Drawing.Size(500, 174);
            this.listViewSelected.SmallImageList = this.imageListSmall;
            this.listViewSelected.TabIndex = 7;
            this.listViewSelected.UseCompatibleStateImageBehavior = false;
            this.listViewSelected.DoubleClick += new System.EventHandler(this.listViewSelected_DoubleClick);
            // 
            // columnHeaderTitle
            // 
            this.columnHeaderTitle.Text = "Camera";
            this.columnHeaderTitle.Width = 300;
            // 
            // columnHeaderConnectionStatus
            // 
            this.columnHeaderConnectionStatus.Text = "Connection Status";
            this.columnHeaderConnectionStatus.Width = 100;
            // 
            // columnHeaderSerialNumber
            // 
            this.columnHeaderSerialNumber.DisplayIndex = 3;
            this.columnHeaderSerialNumber.Text = "SerialNumber";
            this.columnHeaderSerialNumber.Width = 100;
            // 
            // columnHeaderNumChannels
            // 
            this.columnHeaderNumChannels.DisplayIndex = 2;
            this.columnHeaderNumChannels.Text = "# Channels";
            this.columnHeaderNumChannels.Width = 75;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.buttonDeselect);
            this.panel2.Controls.Add(this.buttonSelect);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(500, 42);
            this.panel2.TabIndex = 6;
            this.panel2.Paint += new System.Windows.Forms.PaintEventHandler(this.panel2_Paint);
            // 
            // buttonDeselect
            // 
            this.buttonDeselect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDeselect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonDeselect.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonDeselect.Location = new System.Drawing.Point(254, 6);
            this.buttonDeselect.Name = "buttonDeselect";
            this.buttonDeselect.Size = new System.Drawing.Size(30, 30);
            this.buttonDeselect.TabIndex = 10;
            this.buttonDeselect.Text = "A";
            this.buttonDeselect.UseVisualStyleBackColor = true;
            this.buttonDeselect.Click += new System.EventHandler(this.buttonDeselect_Click);
            // 
            // buttonSelect
            // 
            this.buttonSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonSelect.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonSelect.Location = new System.Drawing.Point(218, 6);
            this.buttonSelect.Name = "buttonSelect";
            this.buttonSelect.Size = new System.Drawing.Size(30, 30);
            this.buttonSelect.TabIndex = 9;
            this.buttonSelect.Text = "V";
            this.buttonSelect.UseVisualStyleBackColor = true;
            this.buttonSelect.Click += new System.EventHandler(this.buttonSelect_Click);
            // 
            // CameraExplorerControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "CameraExplorerControl";
            this.Size = new System.Drawing.Size(500, 440);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ImageList imageListLarge;
        private System.Windows.Forms.ImageList imageListSmall;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListView listViewAvailable;
        private System.Windows.Forms.ColumnHeader columnHeaderTitleAvail;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonChangeView;
        private System.Windows.Forms.ListView listViewSelected;
        private System.Windows.Forms.ColumnHeader columnHeaderTitle;
        private System.Windows.Forms.ColumnHeader columnHeaderConnectionStatus;
        private System.Windows.Forms.ColumnHeader columnHeaderSerialNumber;
        private System.Windows.Forms.ColumnHeader columnHeaderNumChannels;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button buttonDeselect;
        private System.Windows.Forms.Button buttonSelect;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonAddAssembly;
    }
}
