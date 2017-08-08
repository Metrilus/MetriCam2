// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Windows.Forms;

namespace MetriCam2.Controls
{
    public partial class CameraExplorerDialog : Form
    {
        #region Public Properties
        public bool ShowAddButton
        {
            get { return cameraExplorerCtrl.ShowAddButton; }
            set { cameraExplorerCtrl.ShowAddButton = value; }
        }
        public ListView.ListViewItemCollection SelectedCameras { get { return cameraExplorerCtrl.SelectedCameras; } }
        public ImageList LargeImageList { get { return cameraExplorerCtrl.LargeImageList; } }
        public ImageList SmallImageList { get { return cameraExplorerCtrl.SmallImageList; } }
        #endregion

        #region Constructor
        public CameraExplorerDialog()
        {
            InitializeComponent();
        }
        #endregion

        #region Public Methods
        public void AddCamerasFromDLL(string filename)
        {
            cameraExplorerCtrl.AddCamerasFromDLL(filename);
        }
        public void RefreshAvailableCameras()
        {
            cameraExplorerCtrl.RefreshAvailableCameras();
        }
        #endregion

        #region Private Methods
        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }
        #endregion
    }
}
