// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2.Exceptions
{
    public class ImageAcquisitionFailedException : MetriCam2Exception
    {
        public ImageAcquisitionFailedException(string message)
            : base(message)
        { }
        public ImageAcquisitionFailedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
