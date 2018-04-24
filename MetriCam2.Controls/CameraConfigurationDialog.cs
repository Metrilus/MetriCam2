// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using MetriCam2;
using Metrilus.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MetriCam2.Controls
{
    /// <summary>
    /// GUI control which displays a camera's channels and parameters.
    /// </summary>
    /// <todo>
    /// Add support for .SerialNumber and .Name
    /// </todo>
    public partial class CameraConfigurationDialog : Form
    {
        #region Private Fields
        private static MetriLog log = new MetriLog();

        private Camera camera = null;

        private CultureInfo oldCurrentCulture;
        private CultureInfo oldCurrentUICulture;
        #endregion

        #region Constructor
        public CameraConfigurationDialog(Camera cam)
        {
            camera = cam;

            InitializeComponent();

            // Init camera settings
            cameraSettingsControl.TextColor = Color.Black;
            cameraSettingsControl.Camera = camera;

            LoadChannels();
        }
        #endregion

        #region GUI Event Handlers
        private void CameraConfigurationDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            RestorePreviousCulture();
        }

        private void CameraConfigurationDialog_Load(object sender, EventArgs e)
        {
            StoreCurrentCulture();
        }

        private void CameraConfigurationDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keys.Escape == e.KeyCode)
            {
                log.DebugFormat("{0} key pressed. Closing dialog.", e.KeyCode.ToString());

                CancelDialog();
                return;
            }
        }
        #endregion

        #region Private Methods

        private void LoadChannels()
        {
            log.EnterMethod();

            checkedListBoxChannels.Items.Clear();

            if (0 == camera.NumChannels)
            {
                checkedListBoxChannels.Enabled = false;
                string dummyChannelName = "Camera has no channels";
                if (!camera.IsConnected)
                {
                    dummyChannelName += " (Connecting may help)";
                }
                dummyChannelName += ".";
                checkedListBoxChannels.Items.Add(dummyChannelName);
                return;
            }

            foreach (var item in camera.Channels)
            {
                int idx = checkedListBoxChannels.Items.Add(item.Name);
                if (idx > -1 && camera.IsChannelActive(item.Name))
                {
                    checkedListBoxChannels.SetItemChecked(idx, true);
                }
            }

            checkedListBoxChannels.Enabled = true;
            checkedListBoxChannels.SelectedItem = camera.SelectedChannel;

            checkedListBoxChannels.ItemCheck += (sender, e) => {
                var item = checkedListBoxChannels.Items[e.Index];
                string channel = item.ToString();
                if (e.NewValue == CheckState.Checked && e.CurrentValue == CheckState.Unchecked)
                {
                    try
                    {
                        camera.ActivateChannel(channel);
                    }
                    catch (Exception ex)
                    {
                        log.ErrorFormat("Could not activate channel '{0}': {1}", channel, ex.Message);
                    }
                }
                else if (e.NewValue == CheckState.Unchecked && e.CurrentValue == CheckState.Checked)
                {
                    try
                    {
                        camera.DeactivateChannel(channel);
                    }
                    catch (Exception ex)
                    {
                        log.ErrorFormat("Could not deactivate channel '{0}': {1}", channel, ex.Message);
                    }
                }
            };

            checkedListBoxChannels.SelectedIndexChanged += (sender, e) =>
            {
                if (1 == camera.ActiveChannels.Count)
                {
                    camera.SelectChannel(camera.ActiveChannels[0].Name);
                }
                else if (1 == checkedListBoxChannels.SelectedItems.Count && checkedListBoxChannels.CheckedItems.Contains(checkedListBoxChannels.SelectedItem))
                {
                    camera.SelectChannel(checkedListBoxChannels.SelectedItem.ToString());
                }
            };
        }

        private void CancelDialog()
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        private void StoreCurrentCulture()
        {
            // Store current culture info and set it to invariant
            oldCurrentCulture = Thread.CurrentThread.CurrentCulture;
            oldCurrentUICulture = Thread.CurrentThread.CurrentUICulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        private void RestorePreviousCulture()
        {
            // Restore previous culture info
            Thread.CurrentThread.CurrentCulture = oldCurrentCulture;
            Thread.CurrentThread.CurrentUICulture = oldCurrentUICulture;
        }
        #endregion
    }
}
