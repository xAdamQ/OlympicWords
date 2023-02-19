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


    /// <summary>
    /// this is not due to user hack, but for misusing like exceeding the limit, the message her is shown to the user
    /// </summary>
    [Serializable]
    public class BadUserBehaviourException : HubException
    {
        public BadUserBehaviourException() : base()
        {
        }

        public BadUserBehaviourException(string message) : base(message)
        {
        }
    }
}