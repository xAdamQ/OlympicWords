using Microsoft.AspNetCore.SignalR;

namespace OlympicWords.Services.Exceptions
{
    [System.Serializable]
    public class BadUserInputException : HubException
    {
        public BadUserInputException() : base()
        {
        }

        public BadUserInputException(string message) : base(message)
        {
        }
    }
}