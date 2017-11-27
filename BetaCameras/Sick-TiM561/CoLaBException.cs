using System;
using System.Runtime.Serialization;

namespace SICK_TIM561_Client
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
