// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;

namespace MetriCam2.Exceptions
{
    public class MetriCam2Exception : Exception
    {
        public MetriCam2Exception(string str)
            : base(str)
        { }
        public MetriCam2Exception(string str, Exception innerException)
            : base(str, innerException)
        { }
    }
}
