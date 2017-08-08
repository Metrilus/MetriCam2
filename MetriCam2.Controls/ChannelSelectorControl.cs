// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Metrilus.Logging;

namespace MetriCam2.Controls
{
    internal partial class ChannelSelectorControl : CheckedListBox
    {
        #region Private Fields
        private static MetriLog log = new MetriLog();
        #endregion

        #region Public Fields
        public List<Camera> Cameras = new List<Camera>();
        public List<string> ChannelNames = new List<string>();
        #endregion

        #region Constructor
        public ChannelSelectorControl()
        {
            InitializeComponent();
        }
        #endregion

        public void LoadChannels(List<Camera> cameras, string selectedChannel = null)
        {
            log.EnterMethod();

            this.Cameras.Clear();
            this.ChannelNames.Clear();
            Items.Clear();

            bool multiCamera = cameras.Count > 1;

            foreach (var camera in cameras)
            {
                foreach (var channel in camera.Channels)
                {
                    string itemName = multiCamera
                        ? camera.ChannelIdentifier(channel.Name)
                        : channel.Name;

                    int idx = Items.Add(itemName, camera.IsChannelActive(channel.Name));
                    this.Cameras.Insert(idx, camera);
                    this.ChannelNames.Insert(idx, channel.Name);
                }
            }

            if (!string.IsNullOrWhiteSpace(selectedChannel))
            {
                SelectedItem = selectedChannel;
            }
            else
            {
                if (!multiCamera)
                {
                    SelectedItem = cameras[0].SelectedChannel;
                }
            }

            RecomputeLayout();
        }
        /// <summary>
        /// Activates all channels selected by user.
        /// </summary>
        public void ActivateAndSelectChannels()
        {
            foreach (int item in CheckedIndices)
            {
                Cameras[item].ActivateChannel(ChannelNames[item]);
            }
            foreach (int item in SelectedIndices)
            {
                Cameras[item].ActivateChannel(ChannelNames[item]);
                Cameras[item].SelectChannel(ChannelNames[item]);
            }
        }

        private void RecomputeLayout()
        {
            this.SuspendLayout();
            // GUI layout
            //
            // checkedListBoxChannels { top: labelChannels.Top + labelChannels. Height + MarginBelowLabel; height: dynamic(depends on numChannels); anchor: top | left | right; }

            // compute min. width
            int minWidth = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                int itemWidth = GetItemRectangle(i).Width;
                minWidth = Math.Max(minWidth, itemWidth);
            }

            ClientSize = new Size(
                minWidth,
                GetItemRectangle(0).Height * Items.Count);

            this.MinimumSize = new Size(this.Width, this.Height);

            this.ResumeLayout();
        }
    }
}
