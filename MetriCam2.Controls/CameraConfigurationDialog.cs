﻿// Copyright (c) Metrilus GmbH
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

            AdjustLayout();
        }
        #endregion

        #region GUI Event Handlers
        private void buttonOK_Click(object sender, EventArgs e)
        {
            log.EnterMethod();

            ApplyConfiguration();
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            log.EnterMethod();

            CancelDialog();
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            log.EnterMethod();

            ApplyConfiguration();
        }

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
        private void AdjustLayout()
        {
            this.SuspendLayout();
            checkedListBoxChannels.Anchor = AnchorStyles.None;
            labelSettings.Anchor = AnchorStyles.None;
            cameraSettingsControl.Anchor = AnchorStyles.None;
            // GUI layout
            //
            // labelChannels { top: fixed; height: fixed; anchor: top | left; }
            // checkedListBoxChannels { top: labelChannels.Top + labelChannels. Height + MarginBelowLabel; height: dynamic(depends on numChannels); anchor: top | left | right; }
            // labelSettings { top: checkedListBoxChannels.Top + checkedListBoxChannels.Height + MarginBetweenSections; height: fixed; anchor: top | left; }
            // cameraSettingsControl { top: labelSettings.Top + labelSettings. Height + MarginBelowLabel; height: dynamic(depends on number of parameters); anchor: top | left | right; }
            // form { height: cameraSettingsControl.Top + cameraSettingsControl.Height + ...

            const int MarginBelowLabel = 2;
            const int MarginBetweenSections = 20;

            // labelChannels: everything is fixed

            checkedListBoxChannels.Top = labelChannels.Top + labelChannels.Height + MarginBelowLabel;
            this.checkedListBoxChannels.ClientSize = new Size(
                checkedListBoxChannels.Width,
                checkedListBoxChannels.GetItemRectangle(0).Height * checkedListBoxChannels.Items.Count);
            checkedListBoxChannels.Width = this.ClientSize.Width - (2 * checkedListBoxChannels.Left);

            labelSettings.Top = checkedListBoxChannels.Top + checkedListBoxChannels.Height + MarginBetweenSections;

            cameraSettingsControl.Top = labelSettings.Top + labelSettings.Height + MarginBelowLabel;
            cameraSettingsControl.Width = this.ClientSize.Width - (2 * cameraSettingsControl.Left);

            this.ClientSize = new Size(
                this.ClientSize.Width,
                cameraSettingsControl.Top + cameraSettingsControl.Height + MarginBetweenSections + buttonApply.Height + MarginBetweenSections / 2
            );           
            this.MinimumSize = new Size(this.Width, this.Height);

            // Finally: apply anchors
            checkedListBoxChannels.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            labelSettings.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            cameraSettingsControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            if (!cameraSettingsControl.ContainsOneOrMoreWritableParameters && checkedListBoxChannels.Items.Count == 0)
            {
                buttonApply.Visible = false;
                buttonOK.Visible = false;
            }

            this.ResumeLayout();
        }

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
        }

        private void ApplyConfiguration()
        {
            log.EnterMethod();

            int nrChannelsBeforeConfigurationChange = camera.Channels.Count;

            List<string> channelsNotDeactivated = new List<string>();
            List<string> channelsNotActivated = new List<string>();

            // BUG: If currently selected channel will be deactivated, then we are in trouble

            Task channelsTask = Task.Factory.StartNew(() =>
            {
                log.Debug("Deactivate unchecked channels");
                for (int i = 0; i < checkedListBoxChannels.Items.Count; i++)
                {
                    var item = checkedListBoxChannels.Items[i];
                    string channel = item.ToString();
                    if (!checkedListBoxChannels.CheckedItems.Contains(item))
                    {
                        try
                        {
                            camera.DeactivateChannel(channel);
                        }
                        catch (Exception ex)
                        {
                            log.ErrorFormat("Could not deactivate channel '{0}': {1}", channel, ex.Message);
                            channelsNotDeactivated.Add(channel);
                        }
                    }
                }
            }).ContinueWith((t) =>
            {
                log.Debug("Activate checked channels");
                for (int i = 0; i < checkedListBoxChannels.CheckedItems.Count; i++)
                {
                    var item = checkedListBoxChannels.CheckedItems[i];
                    string channel = item.ToString();
                    try
                    {
                        camera.ActivateChannel(channel);
                    }
                    catch (Exception ex)
                    {
                        log.ErrorFormat("Could not activate channel '{0}': {1}", channel, ex.Message);
                        channelsNotActivated.Add(channel);
                    }
                }
            });

            channelsTask.Wait();

            log.Debug("Try to select a channel");
            if (1 == camera.ActiveChannels.Count)
            {
                camera.SelectChannel(camera.ActiveChannels[0].Name);
            }
            else if (1 == checkedListBoxChannels.SelectedItems.Count && checkedListBoxChannels.CheckedItems.Contains(checkedListBoxChannels.SelectedItem))
            {
                camera.SelectChannel(checkedListBoxChannels.SelectedItem.ToString());
            }

            if (channelsNotDeactivated.Count + channelsNotActivated.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                if (channelsNotDeactivated.Count > 0)
                {
                    sb.AppendLine(string.Format("Could not deactivate the channels '{0}'", string.Join("', '", channelsNotDeactivated)));
                }
                if (channelsNotDeactivated.Count > 0)
                {
                    sb.AppendLine(string.Format("Could not activate the channels '{0}'", string.Join("', '", channelsNotActivated)));
                }

                MessageBox.Show(sb.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            log.Debug("Apply camera parameters");
            cameraSettingsControl.ApplyCameraSettings();

            //If configuring the camera involves an automatic change of the available channels, we should update the channel panel size.
            //TODO: Perform a deep comparison of all channels instead of just comparing the number of elements.
            if (camera.Channels.Count != nrChannelsBeforeConfigurationChange)
            {
                LoadChannels();
                AdjustLayout();
            }
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
