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
