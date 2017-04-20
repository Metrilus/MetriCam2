using System;

namespace MetriCam2.Exceptions
{
    public class ConnectionFailedException : MetriCam2Exception
    {
        public ConnectionFailedException(string message)
            : base(message)
        { }
        public ConnectionFailedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
