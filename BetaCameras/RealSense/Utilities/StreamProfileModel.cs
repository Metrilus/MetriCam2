// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2.Cameras.Utilities
{
    /// <summary>
    /// Class to manage stream profiles
    /// </summary>
    internal class StreamProfileModel
    {
        #region Private Fields
        private PXCMCapture.StreamType streamType;
        private PXCMCapture.Device.StreamProfileSet streamProfileSet;
        #endregion

        #region Public Properties
        /// <summary>
        /// Profile name in a readable format
        /// </summary>
        public string Name
        {
            get
            {
                return (string.Format(
                  "{0} ({1} at {2} Hz [{3}x{4}])",
                  this.streamType,
                  this.streamProfileSet[this.streamType].imageInfo.format,
                  this.streamProfileSet[this.streamType].frameRate.max,
                  this.streamProfileSet[this.streamType].imageInfo.width,
                  this.streamProfileSet[this.streamType].imageInfo.height));
            }
        }

        /// <summary>
        /// Stream type
        /// </summary>
        public PXCMCapture.StreamType StreamType { get { return (this.streamType); } }
        /// <summary>
        /// Stream profile set
        /// </summary>
        public PXCMCapture.Device.StreamProfileSet StreamProfileSet { get { return (this.streamProfileSet); } }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="streamProfileSet">The stream profile set.</param>
        /// <param name="streamType">The stream type.</param>
        public StreamProfileModel(PXCMCapture.Device.StreamProfileSet streamProfileSet, PXCMCapture.StreamType streamType)
        {
            this.streamProfileSet = streamProfileSet;
            this.streamType = streamType;
        }
        #endregion
    }
}
