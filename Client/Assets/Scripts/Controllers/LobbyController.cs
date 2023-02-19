using System.Threading.Tasks;
using Common.Lobby;
using Shared.Controllers;
public class LobbyController : ControllerBase, ILobbyController
{
    public Task BuyPlayer(string itemId)
    {
        return SendAsync(nameof(BuyPlayer), (nameof(itemId), itemId));
    }
    public Task SelectPlayer(string itemId, string env)
    {
        return SendAsync(nameof(SelectPlayer), (nameof(itemId), itemId), (nameof(env), env));
    }

    public Task<MatchRequestResult> RequestMatch(string oppoId)
    {
        throw new System.NotImplementedException();
    }
    public void CancelChallengeRequest(string oppoId)
    {
        throw new System.NotImplementedException();
    }
    public Task<ChallengeResponseResult> RespondChallengeRequest(string senderId, bool response)
    {
        throw new System.NotImplementedException();
    }
    public Task AskForMoneyAid()
    {
        throw new System.NotImplementedException();
    }
    public Task ClaimMoneyAid()
    {
        throw new System.NotImplementedException();
    }
}