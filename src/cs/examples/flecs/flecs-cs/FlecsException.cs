using System;

public class FlecsException : Exception
{
    public FlecsException(string message)
    {
        Message = message;
    }

    public override string Message { get; }
}