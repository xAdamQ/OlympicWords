using Common.Lobby;

#if UNITY
using System.Threading.Tasks;
#endif

namespace Shared.Controllers
{
    public interface ILobbyController : IController
    {
        Task BuyPlayer(string itemId);
        Task SelectPlayer(string itemId, string env);

        Task MakePurchase(string purchaseData, string sign);
        Task<MatchRequestResult> RequestMatch(string oppoId);
        void CancelChallengeRequest(string oppoId);
        Task<ChallengeResponseResult> RespondChallengeRequest
            (string senderId, bool response);
        Task AskForMoneyAid();
        Task ClaimMoneyAid();
    }
}