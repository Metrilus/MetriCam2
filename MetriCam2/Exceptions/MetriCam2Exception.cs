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
