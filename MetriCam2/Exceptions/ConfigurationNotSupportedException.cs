using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2.Exceptions
{
    public class ConfigurationNotSupportedException : MetriCam2Exception
    {
        public ConfigurationNotSupportedException(string message)
            : base(message)
        { }
        public ConfigurationNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
