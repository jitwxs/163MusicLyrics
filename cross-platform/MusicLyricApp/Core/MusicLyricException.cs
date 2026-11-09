using System;

namespace MusicLyricApp.Core;

public class MusicLyricException : Exception
{
    public MusicLyricException()
    {
    }

    public MusicLyricException(string message) : base(message)
    {
    }

    public MusicLyricException(string message, Exception inner) : base(message, inner)
    {
    }

}
