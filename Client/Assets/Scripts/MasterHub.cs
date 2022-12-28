using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Common.Lobby;

public interface IRoomHub
{
    // Task<PersonalFullUserInfo> GetPersonalUserData();
    // /// <summary>
    // /// get public user data by his id
    // /// </summary>
    // Task<FullUserInfo> GetUserData(string id);
    // Task ToggleFollow(string targetId);
    // Task ToggleOpenMatches();
    // Task MakePurchase(string purchaseData, string sign);
    // Task RequestRandomRoom(int betChoice, int capacityChoice);
    // Task<MatchRequestResult> RequestMatch(string oppoId);
    // Task CancelChallengeRequest(string oppoId);
    // Task<ChallengeResponseResult> RespondChallengeRequest
    //     (string senderId, bool response);
    // Task Ready();
    // Task AskForMoneyAid();
    // Task ClaimMoneyAid();
    // Task BuyCardback(int cardbackId);
    // Task BuyBackground(int backgroundId);
    // Task SelectCardback(int cardbackId);
    // Task SelectBackground(int backgroundId);
    // Task ShowMessage(string msgId);
    Task<string> UpStreamChar(IAsyncEnumerable<char> stream);

    IAsyncEnumerable<string[]> DownStreamCharBuffer(
        [EnumeratorCancellation] CancellationToken cancellationToken);

    IAsyncEnumerable<int> DownStreamTest(
        [EnumeratorCancellation] CancellationToken cancellationToken);

    Task Surrender();
    Task LeaveFinishedRoom();
    Task SetPowerUp(int powerUp);
}

//
// public class MasterHub : MonoModule<MasterHub>, IMasterHub
// {
//     // public Task<PersonalFullUserInfo> GetPersonalUserData()
//     // {
//     //     return NetManager.I.InvokeAsync<PersonalFullUserInfo>(nameof(GetPersonalUserData));
//     // }
//     //
//     // public Task<FullUserInfo> GetUserData(string id)
//     // {
//     //     return NetManager.I.InvokeAsync<FullUserInfo>(nameof(GetUserData), id);
//     // }
//     // public Task ToggleFollow(string targetId)
//     // {
//     //     return NetManager.I.SendAsync(nameof(ToggleFollow), targetId);
//     // }
//     // public Task ToggleOpenMatches()
//     // {
//     //     return NetManager.I.SendAsync(nameof(ToggleOpenMatches));
//     // }
//     // public Task MakePurchase(string purchaseData, string sign)
//     // {
//     //     return NetManager.I.SendAsync(nameof(MakePurchase), purchaseData, sign);
//     // }
//     // public Task RequestRandomRoom(int betChoice, int capacityChoice)
//     // {
//     //     return NetManager.I.SendAsync(nameof(RequestRandomRoom), betChoice, capacityChoice);
//     // }
//     // public Task<MatchRequestResult> RequestMatch(string oppoId)
//     // {
//     //     return NetManager.I.InvokeAsync<MatchRequestResult>(oppoId);
//     // }
//     // public Task CancelChallengeRequest(string oppoId)
//     // {
//     //     throw new NotImplementedException();
//     // }
//     // public Task<ChallengeResponseResult> RespondChallengeRequest(string senderId, bool response)
//     // {
//     //     return NetManager.I.InvokeAsync<ChallengeResponseResult>
//     //         (nameof(RespondChallengeRequest), senderId, response);
//     // }
//     public Task Ready()
//     {
//         return RoomNet.I.SendAsync(nameof(Ready));
//     }
//     // public Task AskForMoneyAid()
//     // {
//     //     return NetManager.I.SendAsync(nameof(AskForMoneyAid));
//     // }
//     // public Task ClaimMoneyAid()
//     // {
//     //     return NetManager.I.SendAsync(nameof(ClaimMoneyAid));
//     // }
//     // public Task BuyCardback(int cardbackId)
//     // {
//     //     return NetManager.I.SendAsync(nameof(BuyCardback), cardbackId);
//     // }
//     // public Task BuyBackground(int backgroundId)
//     // {
//     //     return NetManager.I.SendAsync(nameof(BuyBackground), backgroundId);
//     // }
//     // public Task SelectCardback(int cardbackId)
//     // {
//     //     return NetManager.I.SendAsync(nameof(SelectCardback), cardbackId);
//     // }
//     // public Task SelectBackground(int backgroundId)
//     // {
//     //     return NetManager.I.SendAsync(nameof(SelectBackground), backgroundId);
//     // }
//     // public Task ShowMessage(string msgId)
//     // {
//     //     return NetManager.I.SendAsync(nameof(ShowMessage), msgId);
//     // }
//     //
//
//     public Task Surrender()
//     {
//         return RoomNet.I.SendAsync(nameof(Surrender));
//     }
//
//     public Task LeaveFinishedRoom()
//     {
//         return RoomNet.I.SendAsync(nameof(LeaveFinishedRoom));
//     }
//
//     public Task SetPowerUp(int powerUp)
//     {
//         return RoomNet.I.SendAsync(nameof(SetPowerUp), powerUp);
//     }
//
//     public Task<string> UpStreamChar(IAsyncEnumerable<char> stream)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IAsyncEnumerable<string[]> DownStreamCharBuffer(CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }
//
//     public IAsyncEnumerable<int> DownStreamTest(CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }
// }