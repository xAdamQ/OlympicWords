using Microsoft.AspNetCore.SignalR;

namespace OlympicWords
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