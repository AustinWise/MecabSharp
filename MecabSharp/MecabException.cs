using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Austin.MecabSharp
{
    [Serializable]
    public class MecabException : Exception
    {
        public MecabException() { }
        public MecabException(string message) : base(message) { }
        public MecabException(string message, Exception inner) : base(message, inner) { }
        protected MecabException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
