using System;
using System.Runtime.Serialization;

namespace MusicLyricApp.Exception
{
    [Serializable]
    public class MusicLyricException : System.Exception
    {
        public MusicLyricException()
        {
        }

        public MusicLyricException(string message) : base(message)
        {
        }

        public MusicLyricException(string message, System.Exception inner) : base(message, inner)
        {
        }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected MusicLyricException(SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}