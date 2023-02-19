using OlympicWords.Services.Extensions;
using Hangfire;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;
using OlympicWords.Services.Exceptions;

namespace OlympicWords.Services
{
    public interface ILobbyManager
    {
        Task RequestMoneyAid();
        Task ClaimMoneyAim();

        Task MakePurchase(string purchaseData, string sign);

        Task BuyPlayer(string id);
        Task SelectPlayer(string id, string env);
    }

    public class LobbyManager : ILobbyManager
    {
        private readonly IOfflineRepo offlineRepo;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IHubContext<RoomHub> masterHub;
        private readonly IScopeRepo scopeRepo;
        // private readonly IRequestCache _requestCache;

        public LobbyManager(IOfflineRepo offlineRepo, IBackgroundJobClient backgroundJobClient,
            IHubContext<RoomHub> masterHub, IScopeRepo scopeRepo)
        {
            this.offlineRepo = offlineRepo;
            this.backgroundJobClient = backgroundJobClient;
            this.masterHub = masterHub;
            this.scopeRepo = scopeRepo;
        }

        public async Task RequestMoneyAid()
        {
            var user = await offlineRepo.GetCurrentUserAsync();
            if (user.IsMoneyAidProcessing)
                throw new BadUserInputException(
                    "the user requested money while there's a waiting request");
            if (user.RequestedMoneyAidToday >= 4)
                throw new BadUserInputException(
                    "the user was trying to request money aid above limit");
            if (user.Money >= Room.MinBet)
                throw new BadUserInputException(
                    "the user was trying to request money aid while he have enough money");
            //not tested because logic is trivial

            user.LastMoneyAimRequestTime = DateTime.UtcNow;
            user.RequestedMoneyAidToday++;

            // _backgroundJobClient.Schedule(() => MakeMoneyAimClaimable(activeUser.Id), ConstData.MoneyAimTime);

            await offlineRepo.SaveChangesAsync();
        } //trivial to test, best in integration

        // public async Task MakeMoneyAimClaimable(string userId)
        // {
        //     var user = await _masterRepo.GetUserByIdAsyc(userId);
        //
        //     // user.IsMoneyAidClaimable = true;
        //     // user.LastMoneyAimRequestTime = null;
        //
        //     //we don't notify the client for 2 reasons:
        //     //he could be inactive and when he request/start he know the remaining time
        //     //when he checks for claimable flag he can claim
        //     await _masterRepo.SaveChangesAsync();
        // } //issue changes, no test

        public async Task ClaimMoneyAim()
        {
            var user = await offlineRepo.GetCurrentUserAsync();

            if (user.LastMoneyAimRequestTime == null)
                throw new BadUserInputException(
                    "the user was trying to claim while he didn't request");
            if (user.LastMoneyAimRequestTime.SecondsPassedSince() < ConstData.MoneyAimTime)
                throw new BadUserInputException(
                    "the user was trying to claim while the time is not done");

            user.LastMoneyAimRequestTime = null;
            user.Money += Room.MinBet;

            await offlineRepo.SaveChangesAsync();
            //I didn't send the user to let the client sync the state and figure out because the
            //situation is customized so not returning error in itself means the client can make the receive money
            //animation and ui update
        } //don't test

        public async Task BuyPlayer(string id)
        {
            if (!OfflineRepo.ItemPlayers.TryGetValue(id, out var item))
                throw new BadUserInputException("client give player id that doesn't exist");

            var user = await offlineRepo.GetCurrentUserAsync();

            if (user.Money < item.Price)
                throw new BadUserInputException
                    ("the client is trying to buy a player without enough money");

            if (user.OwnedItemPlayers.Contains(id))
                throw new BadUserInputException
                    ("the client is trying to buy a player that he already owns");

            user.Money -= item.Price;
            user.OwnedItemPlayers.Add(id);

            offlineRepo.MarkUserPropertyModified(user, u => u.OwnedItemPlayers);

            await offlineRepo.SaveChangesAsync();
        }

        /// <summary>
        /// the chosen env is a top level playable env
        /// </summary>
        public async Task SelectPlayer(string id, string env)
        {
            if (!OfflineRepo.ItemPlayers.TryGetValue(id, out var item))
                throw new BadUserInputException("client give player id that doesn't exist");

            if (!item.Environment.Matching.TryGetValue(env, out var matchingEnv))
                throw new BadUserInputException($"environment {env} doesn't match the item or doesn't exist");

            if (!matchingEnv.Playable)
                throw new BadUserInputException($"environment {env} is not a playable environment");

            var user = await offlineRepo.GetCurrentUserAsync();

            if (!user.OwnedItemPlayers.Contains(id))
                throw new BadUserInputException($"the user is trying to select a player {item.Id} " +
                                                $"he doesn't own {string.Join(", ", user.OwnedCardBackIds)}");

            if (user.SelectedItemPlayer.TryGetValue(env, out var selectedPlayerId))
            {
                if (selectedPlayerId == id) return;
                //already selected

                user.SelectedItemPlayer[env] = id;
            }
            else
            {
                user.SelectedItemPlayer.Add(env, id);
            }

            offlineRepo.MarkUserPropertyModified(user, u => u.SelectedItemPlayer);

            await offlineRepo.SaveChangesAsync();
        }

        public void BuyItem(string id, string env, int type)
        {
            //we shouldn't have the option of extending the functionality, so a shared function is less useful
            //anyway, for shared logic use the type int to get the needed item list
        }

        public async Task MakePurchase(string purchaseData, string sign)
        {
            dynamic dataObj = JObject.Parse(purchaseData);
            string productId = dataObj.productId;

            switch (productId)
            {
                case "money500":
                    await AddMoney(500);
                    break;

                case "money3000":
                    await AddMoney(3000);
                    break;
            }
        }

        private async Task AddMoney(int amount)
        {
            var dUser = await offlineRepo.GetCurrentUserAsync();

            dUser.Money += amount;

            await offlineRepo.SaveChangesAsync();

            await masterHub.SendOrderedAsync(scopeRepo.RoomUser, "AddMoney", amount);
        }

        public static bool VerifyIapSign(string content, string sign, string pubKey)
        {
            var contentBytes = Convert.FromBase64String(content);
            var signBytes = Encoding.UTF8.GetBytes(sign);
            var pubKeyBytes = Convert.FromBase64String(pubKey);

            var rsa = new RSACryptoServiceProvider();
            rsa.ImportSubjectPublicKeyInfo(pubKeyBytes, out _);

            var hashData = SHA256.Create().ComputeHash(contentBytes);

            var res1 = rsa.VerifyData(contentBytes, CryptoConfig.MapNameToOID("SHA256"), signBytes);
            var res2 = rsa.VerifyHash(hashData, CryptoConfig.MapNameToOID("SHA256"), signBytes);
            var res3 = rsa.VerifyHash(hashData, signBytes, HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            var res4 = rsa.VerifyData(contentBytes, signBytes, HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            return res1 && res2 && res3 && res4;
        }
    }
}