using System;

[Serializable]
public class BadUserInputException : Exception
{
    public BadUserInputException()
    {
    }
    public BadUserInputException(string message) : base(message)
    {
    }
}