using OlympicWords.Services.Exceptions;
using OlympicWords.Services.Extensions;
using Hangfire;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Linq;

namespace OlympicWords.Services
{
    public interface ILobbyManager
    {
        Task RequestMoneyAid(ActiveUser activeUser);
        Task ClaimMoneyAim(ActiveUser activeUser);

        Task BuyCardBack(int cardbackId, string activeUserId);
        Task BuyBackground(int backgroundId, string activeUserId);
        Task SelectCardback(int cardbackId, string activeUserId);
        Task SelectBackground(int backgroundId, string activeUserId);
        Task MakePurchase(ActiveUser activeUser, string purchaseData, string sign);
    }

    //todo split this to shop and other things for example
    public class LobbyManager : ILobbyManager
    {
        private readonly IOfflineRepo offlineRepo;
        private readonly IBackgroundJobClient backgroundJobClient;
        private readonly IHubContext<MasterHub> masterHub;
        // private readonly IRequestCache _requestCache;

        public LobbyManager(IOfflineRepo offlineRepo, IBackgroundJobClient backgroundJobClient,
            IHubContext<MasterHub> masterHub)
        {
            this.offlineRepo = offlineRepo;
            this.backgroundJobClient = backgroundJobClient;
            this.masterHub = masterHub;
        }

        public async Task RequestMoneyAid(ActiveUser activeUser)
        {
            var user = await offlineRepo.GetUserByIdAsyc(activeUser.Id);
            if (user.IsMoneyAidProcessing)
                throw new Exceptions.BadUserInputException(
                    "the user requested money while there's a waiting request");
            if (user.RequestedMoneyAidToday >= 4)
                throw new Exceptions.BadUserInputException(
                    "the user was trying to request money aid above limit");
            if (user.Money >= Room.MinBet)
                throw new Exceptions.BadUserInputException(
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

        public async Task ClaimMoneyAim(ActiveUser activeUser)
        {
            var user = await offlineRepo.GetUserByIdAsyc(activeUser.Id);

            if (user.LastMoneyAimRequestTime == null)
                throw new Exceptions.BadUserInputException(
                    "the user was trying to claim while he didn't request");
            if (user.LastMoneyAimRequestTime.SecondsPassedSince() < ConstData.MoneyAimTime)
                throw new Exceptions.BadUserInputException(
                    "the user was trying to claim while the time is not done");

            user.LastMoneyAimRequestTime = null;
            user.Money += Room.MinBet;

            await offlineRepo.SaveChangesAsync();
            //I didn't send the user to let the client sync the state and figure out because the
            //situation is customized so not returning error in itself means the client can make the receive money
            //animation and ui update
        } //don't test

        /// <summary>
        /// I can't remove items in the future, that's why their price order is the id
        /// </summary>
        private static readonly int[] CardbackPrices =
            { 50, 65, 120, 450, 800, 1100, 2000, 3000, 5000 };
        /// <summary>
        /// I can't remove items in the future, that's why their price order is the id
        /// </summary>
        private static readonly int[] BackgroundPrices =
            { 50, 65, 300, 600, 1000, 2100, 3050, 3900, 6000, 9000 };

        /*
        so say I want to add an item, what to do?
        
        1- add it's price to the server
        2- add it's string adress to the client and append this address in the enum as last element
        */

        public async Task BuyCardBack(int cardbackId, string activeUserId)
        {
            var user = await offlineRepo.GetUserByIdAsyc(activeUserId);

            if (cardbackId < 0 || cardbackId >= CardbackPrices.Length)
                throw new Exceptions.BadUserInputException("client give cardback id exceed count");
            if (user.Money < CardbackPrices[cardbackId])
                throw new Exceptions.BadUserInputException(
                    "the client is trying to buy cardback without enough money");
            if (user.OwnedCardBackIds.Contains(cardbackId))
                throw new Exceptions.BadUserInputException(
                    "the client is trying to buy cardback that he already owns");

            user.Money -= CardbackPrices[cardbackId];
            user.OwnedCardBackIds.Add(cardbackId);
            //and here the issue is raised
            //(1) if the client got success result, then the money is taken and the card is bought and he can do this
            //logic of updating the money and unlock cardback
            //(2) but also I can sync the whole user data so the money will be updated and the cardback will be unlocked
            //so the difference between the 2 approached is (1) I know what is the result in the client
            //(2) I don't know, I will update the data
            //and to avoid updating the whole data you can pass the change name and value as return
            //which is something the client will expect also so this is meaningless

            await offlineRepo.SaveChangesAsync();
        }
        public async Task BuyBackground(int backgroundId, string activeUserId)
        {
            var user = await offlineRepo.GetUserByIdAsyc(activeUserId);

            if (backgroundId < 0 || backgroundId >= BackgroundPrices.Length)
                throw new Exceptions.BadUserInputException("client give background id exceed count");
            if (user.Money < BackgroundPrices[backgroundId])
                throw new Exceptions.BadUserInputException(
                    "the client is trying to buy background without enough money");
            if (user.OwnedBackgroundIds.Contains(backgroundId))
                throw new Exceptions.BadUserInputException(
                    "the client is trying to buy background that he already owns");

            user.Money -= BackgroundPrices[backgroundId];
            user.OwnedBackgroundIds.Add(backgroundId);

            await offlineRepo.SaveChangesAsync();
        }
        //I tested cardback bu bu it's applicable on both

        public async Task SelectCardback(int cardbackId, string activeUserId)
        {
            var user = await offlineRepo.GetUserByIdAsyc(activeUserId);

            if (!user.OwnedCardBackIds.Contains(cardbackId))
                throw new Exceptions.BadUserInputException(
                    $"the user is trying to select a cardback {cardbackId} he doesn't own {string.Join(", ", user.OwnedCardBackIds)}");

            if (user.SelectedCardback == cardbackId) return;

            user.SelectedCardback = cardbackId;

            await offlineRepo.SaveChangesAsync();
        } //trivial to test
        public async Task SelectBackground(int backgroundId, string activeUserId)
        {
            var user = await offlineRepo.GetUserByIdAsyc(activeUserId);

            if (!user.OwnedBackgroundIds.Contains(backgroundId))
                throw new Exceptions.BadUserInputException(
                    $"the user is trying to select a background {backgroundId} he doesn't own {string.Join(", ", user.OwnedBackgroundIds)}");

            if (user.SelectedBackground == backgroundId) return;

            user.SelectedBackground = backgroundId;

            await offlineRepo.SaveChangesAsync();
        }

        public async Task MakePurchase(ActiveUser activeUser, string purchaseData, string sign)
        {
            dynamic dataObj = JObject.Parse(purchaseData);
            string productId = dataObj.productId;

            switch (productId)
            {
                case "money500":
                    await AddMoney(activeUser, 500);
                    break;

                case "money3000":
                    await AddMoney(activeUser, 3000);
                    break;
            }
        }

        private async Task AddMoney(ActiveUser activeUser, int amount)
        {
            var dUser = await offlineRepo.GetUserByIdAsyc(activeUser.Id);

            dUser.Money += amount;

            await offlineRepo.SaveChangesAsync();

            await masterHub.SendOrderedAsync(activeUser, "AddMoney", amount);
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