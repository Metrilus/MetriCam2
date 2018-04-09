using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MetriCam2.Exceptions
{
    public class SettingCombinationNotSupportedException : MetriCam2Exception
    {
        public SettingCombinationNotSupportedException(string message)
            : base(message)
        { }
        public SettingCombinationNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
