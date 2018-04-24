// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MetriCam2.Controls
{
    /// <summary>
    /// GUI Component for the selection of multiple filenames 
    /// </summary>
    public partial class MultiFileSelector : UserControl, INotifyPropertyChanged
    {
        #region Properties
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Standard height of this component (is needed by the camera settings control to set a reasonable value for the required space for this component)
        /// </summary>
        /// <seealso cref="CameraSettingsControl"/>
        public static int StandardHeight 
        { 
            get
            {
                return 64;
            }
        }

        /// <summary>
        /// The currently selected file names
        /// </summary>
        private List<string> _selectedFiles = new List<string>();
        public List<string> SelectedFiles {
            get => _selectedFiles;
            set
            {
                if(value != _selectedFiles)
                {
                    _selectedFiles = value;
                    NotifyPropertyChanged(nameof(SelectedFiles));
                }
            }
        }
        #endregion

        /// <summary>
        /// Construct from parameter descriptor.
        /// </summary>
        /// <param name="desc">Parameter descriptor.</param>
        public MultiFileSelector(Camera.MultiFileParamDesc desc)
        {
            InitializeComponent();         

            if (desc.Value != null)
            {
                List<string> filenames = (List<string>)desc.Value;
                listBoxSelectedFiles.Items.AddRange(filenames.ToArray());
                SelectedFiles = filenames;
            }

            this.Enabled = desc.IsWritable;
        }

        private void buttonSelectFiles_Click(object sender, EventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Multiselect = true;
            if (diag.ShowDialog() == DialogResult.OK)
            {
                listBoxSelectedFiles.Items.Clear();
                listBoxSelectedFiles.Items.AddRange(diag.FileNames);
                SelectedFiles = diag.FileNames.ToList();
            }            
        }
    }
}
