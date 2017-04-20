using MetriCam2;
using Metrilus.Logging;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MetriCam2.Controls
{
    public partial class CameraExplorerControl : UserControl
    {
        #region Public Types
        public class CameraListViewItem : ListViewItem
        {
            //private CameraListViewItem item;

            #region Public Properties
            public Camera Cam { get; set; }
            #endregion

            #region Constructors
            public CameraListViewItem(string text, string imageKey)
                : base(text, imageKey)
            { /* empty */ }

            public CameraListViewItem(CameraListViewItem other)
                : this(other.Text, other.ImageKey)
            {
                this.Cam = other.Cam;
            }
            #endregion
        }
        #endregion

        #region Public Properties
        public bool ShowAddButton
        {
            get { return showAddButton; }
            set
            {
                showAddButton = value;
                buttonAddAssembly.Visible = showAddButton;
            }
        }
        public View View
        {
            get { return listViewAvailable.View; }
            set 
            {
                listViewAvailable.View = value;
                listViewSelected.View = value;
            }
        }
        public ListView.ListViewItemCollection SelectedCameras
        {
            get
            {
                return listViewSelected.Items;
            }
        }
        public ImageList LargeImageList { get { return imageListLarge; } }
        public ImageList SmallImageList { get { return imageListSmall; } }
        #endregion

        #region Private Fields
        private static CameraManagement cameraManagement;
        private static readonly int IMAGE_SIZE_SMALL = 32;
        private static readonly int IMAGE_SIZE_LARGE = 64;
        private static MetriLog log = new MetriLog();
        private bool showAddButton = true;
        #endregion

        #region Constructor
        public CameraExplorerControl()
        {
            InitializeComponent();

            cameraManagement = CameraManagement.GetInstance(CameraManagement.ScanForCameraDLLs);
            cameraManagement.SelectedCamerasChanged += cameraManagement_SelectedCamerasChanged;
            RefreshAvailableCameras();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Scans an assembly file for MetriCam2 camera implementations, loads them, and updates the control.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="CameraManagement.AddCamerasFromDLL"/>, passes through any exceptions from it.
        /// </remarks>
        /// <param name="filename"></param>
        public void AddCamerasFromDLL(string filename)
        {
            try
            {
                cameraManagement.AddCamerasFromDLL(filename);
            }
            catch (Exception)
            {
                MessageBox.Show(string.Format("File '{0}' could not be loaded.", filename), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshAvailableCameras();
        }

        public void RefreshAvailableCameras()
        {
            listViewAvailable.Items.Clear();
            imageListSmall.Images.Clear();
            imageListLarge.Images.Clear();
            listViewAvailable.BeginUpdate();
            foreach (Camera cam in cameraManagement.AvailableCameras)
            {
                CameraListViewItem listViewItem = new CameraListViewItem(cam.Name, cam.GetType().ToString());
                listViewItem.Name = cam.GetType().ToString();
                listViewItem.Cam = cam;

                Bitmap bmp = ScaleBitmap(cam.CameraIcon, IMAGE_SIZE_LARGE);
                imageListLarge.Images.Add(cam.GetType().ToString(), (Bitmap)bmp.Clone());
                imageListSmall.Images.Add(cam.GetType().ToString(), ScaleBitmap(cam.CameraIcon, IMAGE_SIZE_SMALL));

                OverlayBitmap(bmp, Properties.Resources.greenDot);
                imageListLarge.Images.Add(cam.GetType().ToString() + "_C", bmp);
                imageListSmall.Images.Add(cam.GetType().ToString() + "_C", ScaleBitmap(bmp, IMAGE_SIZE_SMALL));

                this.listViewAvailable.Items.Add(listViewItem);
            }
            listViewAvailable.EndUpdate();
        }
        #endregion

        #region Private Methods
        private void SelectCameras()
        {
            listViewSelected.BeginUpdate();
            foreach (ListViewItem item in listViewAvailable.SelectedItems)
            {
                CameraListViewItem clvi = (CameraListViewItem)item;
                cameraManagement.SelectCamera(clvi.Cam);
            }
            listViewSelected.EndUpdate();
        }

        private void DeselectCameras()
        {
            listViewSelected.BeginUpdate();
            foreach (ListViewItem item in listViewSelected.SelectedItems)
            {
                CameraListViewItem clvi = (CameraListViewItem)item;
                cameraManagement.DeselectCamera(clvi.Cam);
            }
            listViewSelected.EndUpdate();
        }

        private void cameraManagement_SelectedCamerasChanged(object sender, CameraManagement.SelectedCamerasChangedArgs args)
        {
            if (args.Deselected)
            {
                if (args.Camera.IsConnected)
                {
                    log.Warn(String.Format("CameraExplorer: Camera {0} was deselected by CameraManagement but is currently connected. Camera will not be deselected in CameraExplorer", args.Camera.ToString()));
                    return;
                }

                for (int i = 0; i < listViewSelected.Items.Count; i++)
                {
                    if (((CameraListViewItem)listViewSelected.Items[i]).Cam == args.Camera)
                    {
                        listViewSelected.Items.RemoveAt(i);
                        break;
                    }
                }
                args.Camera.OnConnected -= cam_OnConnected;
                args.Camera.OnDisconnected -= cam_OnDisconnected;
            }
            else
            {
                Camera cam = args.Camera;
                cam.OnConnected += cam_OnConnected;
                cam.OnDisconnected += cam_OnDisconnected;
                CameraListViewItem clvi = null;
                for (int i = 0; i < listViewAvailable.Items.Count; i++)
                {
                    if (listViewAvailable.Items[i].Text == cam.Name)
                    {
                        clvi = new CameraListViewItem(listViewAvailable.Items[i].Text, listViewAvailable.Items[i].ImageKey);
                        clvi.Name = listViewAvailable.Items[i].Name;
                        break;
                    }
                }
                if (clvi == null)
                    return;
                clvi.Cam = cam;
                clvi.SubItems.Add("Not connected");
                clvi.SubItems.Add("N/A");
                clvi.SubItems.Add("");
                this.listViewSelected.Items.Add(clvi);
            }
        }

        private void cam_OnConnected(Camera sender)
        {
            listViewSelected.BeginUpdate();
            Bitmap bmp = ScaleBitmap(sender.CameraIcon, IMAGE_SIZE_LARGE);
            // Overlay icon with green dot, that indicates its connection status.
            OverlayBitmap(bmp, Properties.Resources.greenDot);
            foreach (ListViewItem lvi in listViewSelected.Items)
            {
                CameraListViewItem clvi = (CameraListViewItem)lvi;
                if (clvi.Cam == sender)
                {
                    string name = clvi.Name;
                    string text = clvi.Text;
                    clvi.SubItems.Clear();
                    clvi.ImageKey = sender.GetType().ToString() + "_C";
                    clvi.Text = text;
                    clvi.Name = name;
                    clvi.SubItems.Add("Connected", Color.DarkGreen, Color.LightGreen, null);
                    clvi.SubItems.Add(sender.SerialNumber);
                    clvi.SubItems.Add(sender.NumChannels.ToString());
                }
            }
            listViewSelected.EndUpdate();
        }

        private void cam_OnDisconnected(Camera sender)
        {
            listViewSelected.BeginUpdate();
            foreach (ListViewItem item in listViewSelected.Items)
            {
                CameraListViewItem clvi = (CameraListViewItem)item;
                if (clvi.Cam == sender)
                {
                    string name = clvi.Name;
                    string text = clvi.Text;
                    clvi.SubItems.Clear();
                    clvi.ImageKey = clvi.Cam.GetType().ToString();
                    clvi.Text = text;
                    clvi.Name = name;
                    clvi.SubItems.Add("Not connected");
                    clvi.SubItems.Add("N/A");
                    clvi.SubItems.Add("");
                }
            }
            listViewSelected.EndUpdate();
        }

        private Bitmap ScaleBitmap(Bitmap image, int targetSidelength)
        {
            Brush brush = new SolidBrush(Color.Transparent);

            float scale = Math.Min((float)targetSidelength / image.Width, (float)targetSidelength / image.Height);

            Bitmap result = new Bitmap((int)targetSidelength, (int)targetSidelength, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(result);
            graphics.InterpolationMode = InterpolationMode.High;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            int scaleWidth = (int)(image.Width * scale);
            int scaleHeight = (int)(image.Height * scale);

            graphics.FillRectangle(brush, new RectangleF(0, 0, targetSidelength, targetSidelength));
            graphics.DrawImage(image, new Rectangle(((int)targetSidelength - scaleWidth) / 2, ((int)targetSidelength - scaleHeight) / 2, scaleWidth, scaleHeight));

            return result;
        }

        private void OverlayBitmap(Bitmap background, Bitmap overlay)
        {
            Brush brush = new SolidBrush(Color.Black);
            Graphics graphics = Graphics.FromImage(background);
            graphics.InterpolationMode = InterpolationMode.High;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.DrawImage(overlay, new Rectangle(0, background.Height - overlay.Height, overlay.Width, overlay.Height));
        }

        private void buttonChangeView_Click(object sender, EventArgs e)
        {
            switch (View)
            {
                case View.Details:
                    View = View.LargeIcon;
                    break;
                case View.LargeIcon:
                    View = View.SmallIcon;
                    break;
                case View.List:
                    View = View.Details;
                    break;
                case View.SmallIcon:
                    View = View.List;
                    break;
                default:
                    View = View.LargeIcon;
                    break;
            }
        }

        private void buttonSelect_Click(object sender, EventArgs e)
        {
            SelectCameras();
        }

        private void buttonDeselect_Click(object sender, EventArgs e)
        {
            DeselectCameras();
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);

            int offsetDeselectBtn = buttonDeselect.Location.X - buttonSelect.Location.X;
            int btnsWidth = offsetDeselectBtn+ buttonDeselect.Width;
            int left = (panel2.Width - btnsWidth) / 2;
            buttonSelect.Location = new Point(left, buttonSelect.Location.Y);
            buttonDeselect.Location = new Point(left + offsetDeselectBtn, buttonSelect.Location.Y);
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);

            listViewAvailable.Height = splitContainer1.Panel1.Height - panel1.Height;
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint(e);

            listViewSelected.Height = splitContainer1.Panel2.Height - panel2.Height;
        }

        private void listViewAvailable_DoubleClick(object sender, EventArgs e)
        {
            SelectCameras();
        }

        private void listViewSelected_DoubleClick(object sender, EventArgs e)
        {
            DeselectCameras();
        }

        private void buttonAddAssembly_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.Filter = "Managed Assemblies (*.dll)|*.dll";
            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            AddCamerasFromDLL(ofd.FileName);
        }
        #endregion
    }
}
