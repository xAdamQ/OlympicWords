﻿using Common.Lobby;
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

    [ActionDomain(typeof(UserDomain.Stateless.Free))]
    public async Task RequestRandomRoom(int betChoice, int capChoice)
    {
        await matchMaker.RequestRandomRoom(betChoice, capChoice);
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

    // [ActionDomain(typeof(UserDomain.App.Lobby.Idle))]
    public async Task MakePurchase(string purchaseData, string sign)
    {
        await lobbyManager.MakePurchase(purchaseData, sign);
    }

    // [ActionDomain(typeof(UserDomain.App.Lobby.Idle))]
    public async Task AskForMoneyAid()
    {
        // await lobbyManager.RequestMoneyAid(scopeRepo.ActiveUser);
    }

    // [ActionDomain(typeof(UserDomain.App.Lobby.Idle))]
    public async Task ClaimMoneyAid()
    {
        // await lobbyManager.ClaimMoneyAim(scopeRepo.ActiveUser);
    }

    // [ActionDomain(typeof(UserDomain.App.Lobby.Idle))]
    public async Task BuyCardback(int cardbackId)
    {
        // await lobbyManager.BuyCardBack(cardbackId, scopeRepo.ActiveUser.Id);
    }

    // [ActionDomain(typeof(UserDomain.App.Lobby.Idle))]
    public async Task BuyBackground(int backgroundId)
    {
        // await lobbyManager.BuyBackground(backgroundId, scopeRepo.ActiveUser.Id);
    }

    // [ActionDomain(typeof(UserDomain.App.Lobby.Idle))]
    public async Task SelectCardback(int cardbackId)
    {
        // await lobbyManager.SelectCardback(cardbackId, scopeRepo.ActiveUser.Id);
    }

    // [ActionDomain(typeof(UserDomain.App.Lobby.Idle))]
    public async Task SelectBackground(int backgroundId)
    {
        // await lobbyManager.SelectBackground(backgroundId, scopeRepo.ActiveUser.Id);
    }
    #endregion
}