// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2.Cameras.Utilities
{
    /// <summary>
    /// Class to manage capture modules
    /// </summary>
    internal class CaptureModuleModel
    {
        #region Private Fields
        private PXCMSession.ImplDesc implDesc;
        #endregion

        #region Public Properties
        public string Name { get { return this.implDesc.friendlyName; }}
        public PXCMSession.ImplDesc ImplDesc { get { return this.implDesc; }}
        #endregion

        #region Constructor
        public CaptureModuleModel(PXCMSession.ImplDesc implDesc)
        {
            this.implDesc = implDesc;
        }
        #endregion
    }
}
