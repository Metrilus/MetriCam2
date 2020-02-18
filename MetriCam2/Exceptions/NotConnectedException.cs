// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;

namespace MetriCam2.Exceptions
{
    public class NotConnectedException : MetriCam2Exception
    {
        public NotConnectedException(string message)
            : base(message)
        { }
        public NotConnectedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
