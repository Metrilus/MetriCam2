using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2.Exceptions
{
    public class SettingsCombinationNotSupportedException : MetriCam2Exception
    {
        public SettingsCombinationNotSupportedException(string message)
            : base(message)
        { }
        public SettingsCombinationNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
