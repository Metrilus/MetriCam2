using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MetriGUI2D;
using MetriPrimitives.Data;
using MetriPrimitives.Geometry2D;
using System.Threading;

namespace StressTests.Sync_Display
{
    public partial class GUI : Form
    {
        #region Private Members
        int numRows;
        int numCols;
        GeoCircle[,] grid;
        bool[,] enabled;
        private int numCircles;
        private int radius;
        private Random rand;
        private BackgroundWorker bgw;
        private AutoResetEvent bgwCompleted = new AutoResetEvent(false);
        #endregion

        #region Public Properties
        public int CircleRadius
        {
            get { return radius; }
            set
            {
                if (value == radius)
                    return;
                radius = value;
                UpdateGridStructure();
            }
        }
        public int NumRows {
            get { return numRows; }
            set
            {
                if (value == numRows)
                    return;
                numRows = value;
                UpdateGridStructure();
            }
        }

        public int NumCols
        {
            get { return numCols; }
            set
            {
                if (value == numCols)
                    return;
                numCols = value;
                UpdateGridStructure();
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        public GUI()
        {
            InitializeComponent();

            // Set the application to full-screen
            //this.WindowState = FormWindowState.Maximized;
            //this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.TopMost = false;

            rand = new Random();
            floatPanelVis.BackColor = Color.Black;

            NumRows = 3;
            NumCols = 3;
            CircleRadius = 65;
        }
        #endregion

        #region Public Methods

        #endregion

        #region Private Methods
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        private void buttonCalibrate_Click(object sender, EventArgs e)
        {
            StopRandomCircles();
            UpdateGridStructure();
            ActivateAllCircles();
        }

        private void StopRandomCircles()
        {
            if (null == bgw)
                return;
            if (!bgw.IsBusy)
                return;

            bgw.CancelAsync();
            bgwCompleted.WaitOne(100);

            buttonStop.Enabled = false;
            buttonRun.Enabled = true;
        }

        private void buttonRun_Click(object sender, EventArgs e)
        {
            if (bgw != null && bgw.IsBusy)
                return;

            UpdateGridStructure();
            bgw = new BackgroundWorker();
            bgw.WorkerSupportsCancellation = true;
            bgw.DoWork += bgw_DoWork;
            bgw.RunWorkerCompleted += bgw_RunWorkerCompleted;
            bgw.RunWorkerAsync();

            buttonRun.Enabled = false;
            buttonStop.Enabled = true;
        }

        void bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bgwCompleted.Set();
        }

        void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!bgw.CancellationPending)
            {
                ActivateRandomCircles();
            }
            ActivateAllCircles();
            e.Cancel = true;
            e.Result = null;
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            StopRandomCircles();
        }

        private void ActivateAllCircles()
        {
            for (int y = 0; y < numRows; y++)
            {
                for (int x = 0; x < numCols; x++)
                {
                    enabled[y, x] = true;
                }
            }
            RenderGrid();
        }

        private void ActivateRandomCircles()
        {
            ulong code = 0;
            for (int i = 0; i < numCircles; i++)
            {
                ulong digit = (uint)rand.Next(2); // [0,2[
                code = code | digit;
                code = code << 1;
            }
            code = code >> 1;
            ActivateCircles(code);
        }

        private void ActivateCircles(ulong code)
        {
            for (int y = 0; y < numRows; y++)
            {
                for (int x = 0; x < numCols; x++)
                {
                    //int gridCode = y * numCols + numRows;
                    enabled[y, x] = (1 == (code & 1));
                    code = code >> 1;
                }
            }
            RenderGrid();
        }

        private void RenderGrid()
        {
            floatPanelVis.ClearDrawObjects();

            for (int y = 0; y < numRows; y++)
            {
                for (int x = 0; x < numCols; x++)
                {
                    if (enabled[y, x])
                    {
                        floatPanelVis.AddDrawObject(grid[y, x]);
                    }
                }
            }

            GeoText timestamp = new GeoText(Color.White, new Point(50, 50), System.Diagnostics.Stopwatch.GetTimestamp().ToString(), floatPanelVis.CurrentImageToScreenTransform);
            floatPanelVis.AddDrawObject(timestamp);

            Thread.Sleep(10);

            floatPanelVis.Invalidate();
        }

        private void UpdateGridStructure()
        {
            grid = new GeoCircle[numRows, numCols];
            enabled = new bool[numRows, numCols];
            numCircles = numRows * numCols;

            int width = floatPanelVis.Width;
            int height = floatPanelVis.Height;

            int stepX = (int)((width - numCols * CircleRadius) / (numCols + 1));
            int stepY = (int)((height - numRows * CircleRadius) / (numRows + 1));

            int firstX = (int)(stepX + CircleRadius);
            int firstY = (int)(stepY + CircleRadius);

            for (int y = 0; y < numRows; y++)
            {
                int posY = firstY + y * stepY;
                for (int x = 0; x < numCols; x++)
                {
                    int posX = firstX + x * stepX;
                    grid[y, x] = new GeoCircle(Color.Red, new Point(posX, posY), CircleRadius, 1f, floatPanelVis.CurrentImageToScreenTransform);
                    grid[y, x].FillColor = Color.White;
                }
            }

            RenderGrid();
        }

        private void GUI_Load(object sender, EventArgs e)
        {
            UpdateGridStructure();
        }
    }
}
