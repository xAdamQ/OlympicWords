using Common.Lobby;
using Microsoft.AspNetCore.Mvc;
using OlympicWords.Filters;
using OlympicWords.Services;
using Shared.Controllers;

namespace OlympicWords.Controllers;
[Route("[controller]/[action]")]
[ApiController]
public class LobbyController : ControllerBase, ILobbyController
{
    private readonly ILobbyManager lobbyManager;
    private readonly IMatchMaker matchMaker;
    private readonly IScopeRepo scopeRepo;
    public LobbyController(ILobbyManager lobbyManager, IMatchMaker matchMaker, IScopeRepo scopeRepo)
    {
        this.lobbyManager = lobbyManager;
        this.matchMaker = matchMaker;
        this.scopeRepo = scopeRepo;
    }

    #region not used
    [ActionDomain(typeof(UserDomain.Stateless))]
    public async Task<MatchRequestResult> RequestMatch(string oppoId)
    {
        throw new NotImplementedException();
        // return await matchMaker.RequestMatch(scopeRepo.ActiveUser, oppoId);
    }

    // [ActionDomain(typeof(UserDomain.App.Lobby.Pending))]
    public void CancelChallengeRequest(string oppoId)
    {
        // matchMaker.CancelChallengeRequest(scopeRepo.ActiveUser);
    }

    // [ActionDomain(typeof(UserDomain.App.Lobby.Idle))]
    public async Task<ChallengeResponseResult> RespondChallengeRequest
        (string senderId, bool response)
    {
        throw new NotImplementedException();
        // return await matchMaker.RespondChallengeRequest(scopeRepo.ActiveUser, response, senderId);
    }
    public Task AskForMoneyAid()
    {
        throw new NotImplementedException();
    }
    public Task ClaimMoneyAid()
    {
        throw new NotImplementedException();
    }

    [ActionDomain(typeof(UserDomain.Stateless))]
    public async Task BuyPlayer(string itemId)
    {
        await lobbyManager.BuyPlayer(itemId);
    }

    [ActionDomain(typeof(UserDomain.Stateless))]
    public async Task SelectPlayer(string itemId, string env)
    {
        await lobbyManager.SelectPlayer(itemId, env);
    }

    public async Task MakePurchase(string purchaseData, string sign)
    {
        await lobbyManager.MakePurchase(purchaseData, sign);
    }
    #endregion
}