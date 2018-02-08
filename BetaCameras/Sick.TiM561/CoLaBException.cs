using System;
using System.Runtime.Serialization;

namespace MetriCam2.Cameras
{
    [Serializable]
    public class CoLaBException : ApplicationException
    {
        public CoLaBException()
        {
        }

        public CoLaBException(string message)
            : base(message)
        {
        }

        public CoLaBException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected CoLaBException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
