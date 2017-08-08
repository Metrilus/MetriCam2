// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using Metrilus.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MetriCam2.Controls
{
    public partial class ChannelSelectorDialog : Form
    {
        #region Private Fields
        private static MetriLog log = new MetriLog();
        #endregion

        #region Public Properties
        public Camera SelectedCamera { get; private set; }
        public string SelectedChannelName { get; private set; }
        public string SelectedChannelIdentifier { get; private set; }
        #endregion

        #region Constructors
        public ChannelSelectorDialog(Camera cam)
            : this(new List<Camera>() { cam })
        { /* empty */ }

        public ChannelSelectorDialog(List<Camera> cameras)
        {
            InitializeComponent();

            channelSelectorControl.LoadChannels(cameras);
            Relayout();
        }
        #endregion

        private void channelSelectorControl_Resize(object sender, EventArgs e)
        {
            //Relayout();
        }

        private void Relayout()
        {
            this.SuspendLayout();
            // GUI layout
            //
            // labelChannels { top: fixed; height: fixed; anchor: top | left; }
            // checkedListBoxChannels { top: labelChannels.Top + labelChannels. Height + MarginBelowLabel; height: dynamic(depends on numChannels); anchor: top | left | right; }
            // form { height: cameraSettingsControl.Top + cameraSettingsControl.Height + ...

            const int MarginBelowLabel = 2;
            const int MarginBetweenSections = 20;

            // labelChannels: everything is fixed

            channelSelectorControl.Top = labelChannels.Top + labelChannels.Height + MarginBelowLabel;
            this.channelSelectorControl.ClientSize = new Size(
                channelSelectorControl.Width,
                channelSelectorControl.GetItemRectangle(0).Height * channelSelectorControl.Items.Count);
            channelSelectorControl.Width = this.ClientSize.Width - (2 * channelSelectorControl.Left);

            this.ClientSize = new Size(
                this.ClientSize.Width,
                channelSelectorControl.Top + channelSelectorControl.Height + MarginBetweenSections + buttonCancel.Height + MarginBetweenSections / 2
            );
            this.MinimumSize = new Size(this.Width, this.Height);

            // Finally: apply anchors
            channelSelectorControl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // TODO: check if logic is still valid, now that channels come into play
            //if (!cameraSettingsControl.ContainsOneOrMoreWritableParameters)
            //{
            //    buttonOK.Visible = false;
            //    buttonCancel.Text = "&Close";
            //}
            //else
            //{
                buttonCancel.Text = "&Cancel";
            //}

            this.ResumeLayout();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            log.EnterMethod();

            //ApplyConfiguration();

            SelectedCamera = null;
            SelectedChannelName = null;
            SelectedChannelIdentifier = null;
            DialogResult = DialogResult.Ignore;

            int idx = channelSelectorControl.SelectedIndex;
            if (idx >= 0)
            {
                SelectedCamera = channelSelectorControl.Cameras[idx];
                SelectedChannelName = channelSelectorControl.ChannelNames[idx];
                SelectedChannelIdentifier = this.SelectedCamera.ChannelIdentifier(SelectedChannelName);
                channelSelectorControl.ActivateAndSelectChannels();
                DialogResult = DialogResult.OK;
            }

            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            log.EnterMethod();

            CancelDialog();
        }

        private void ChannelSelectorDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keys.Escape == e.KeyCode)
            {
                log.DebugFormat("{0} key pressed. Closing dialog.", e.KeyCode.ToString());

                CancelDialog();
                return;
            }
        }

        private void CancelDialog()
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }
    }
}
