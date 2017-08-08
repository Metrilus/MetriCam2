// Copyright (c) Metrilus GmbH
// MetriCam 2 is licensed under the MIT license. See License.txt for full license text.

using System;

namespace MetriCam2.Exceptions
{
    public class ParameterNotSupportedException : MetriCam2Exception
    {
        public ParameterNotSupportedException(string message)
            : base(message)
        { }
        public ParameterNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
