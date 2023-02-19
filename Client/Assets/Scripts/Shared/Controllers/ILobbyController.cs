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

        Task BuyPlayer(string itemId);
        Task SelectPlayer(string itemId, string env);

// #if UNITY
//         Task
// #else
//         void
// #endif
//             SetPowerUp(int powerUp);

        Task<MatchRequestResult> RequestMatch(string oppoId);
        void CancelChallengeRequest(string oppoId);
        Task<ChallengeResponseResult> RespondChallengeRequest
            (string senderId, bool response);
        Task AskForMoneyAid();
        Task ClaimMoneyAid();
    }
}