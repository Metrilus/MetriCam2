// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2.Cameras.Utilities
{
    /// <summary>
    /// Class to handle device descriptions
    /// </summary>
    internal class DeviceDescriptionModel
    {
        #region Private Fields
        private PXCMCapture.DeviceInfo deviceInfo;
        #endregion

        #region Public Properties
        /// <summary>
        /// Device name.
        /// </summary>
        public string Name { get { return this.deviceInfo.name; } }
        /// <summary>
        /// DeviceInfo Implementation Description
        /// </summary>
        public PXCMCapture.DeviceInfo DeviceInfo { get { return this.deviceInfo; } }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="deviceInfo">The device info.</param>
        public DeviceDescriptionModel(PXCMCapture.DeviceInfo deviceInfo)
        {
            this.deviceInfo = deviceInfo;
        }
        #endregion
    }
}
