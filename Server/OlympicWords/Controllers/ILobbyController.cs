using Common.Lobby;

#if UNITY
using System.Threading.Tasks;
#endif

namespace Shared.Controllers
{
    public interface ILobbyController : IController
    {
        // Task RequestRandomRoom(int betChoice, int capacityChoice);
        // Task Ready();

        Task MakePurchase(string purchaseData, string sign);
        Task<MatchRequestResult> RequestMatch(string oppoId);
        void CancelChallengeRequest(string oppoId);
        Task<ChallengeResponseResult> RespondChallengeRequest
            (string senderId, bool response);
        Task AskForMoneyAid();
        Task ClaimMoneyAid();
        Task BuyCardback(int cardbackId);
        Task BuyBackground(int backgroundId);
        Task SelectCardback(int cardbackId);
        Task SelectBackground(int backgroundId);
    }
}