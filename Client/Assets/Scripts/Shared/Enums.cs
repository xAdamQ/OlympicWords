namespace Common.Lobby
{
    public enum ChallengeResponseResult
    {
        Offline, //player is offline whatever the response
        Canceled, //player is not interested anymore
        Success, //successful whatever the response
    }

    public enum MatchRequestResult
    {
        Offline,
        Playing,
        NoMoney,
        Available,
    }
}