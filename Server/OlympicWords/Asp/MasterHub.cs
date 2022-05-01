using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Reflection;
using OlympicWords.Services.Exceptions;
using Basra.Common;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Runtime.CompilerServices;
using OlympicWords.Services.Helpers;
using OlympicWords.Services;

namespace OlympicWords.Services
{
    public class MasterHub : Hub
    {
        #region services

        private readonly IOfflineRepo offlineRepo;
        private readonly IOnlineRepo onlineRepo;
        private readonly IRoomManager roomManager;
        private readonly IMatchMaker matchMaker;
        private readonly ILogger<MasterHub> logger;
        private readonly IScopeInfo scopeInfo;
        private readonly ILobbyManager lobbyManager;


        public MasterHub(IOfflineRepo offlineRepo, ILobbyManager lobbyManager,
            IOnlineRepo onlineRepo, IRoomManager roomManager, IMatchMaker matchMaker,
            ILogger<MasterHub> logger, IScopeInfo scopeInfo)
        {
            this.offlineRepo = offlineRepo;
            this.lobbyManager = lobbyManager;
            this.onlineRepo = onlineRepo;
            this.roomManager = roomManager;
            this.matchMaker = matchMaker;
            this.logger = logger;
            this.scopeInfo = scopeInfo;
        }

        #endregion

        #region on dis/connceted

        public override async Task OnConnectedAsync()
        {
            logger.LogInformation($"connection established: {Context.UserIdentifier}");

            //this feature was related to the active user functionality, you may restructure things because
            //no disconnected user which is active in a room, I can fill his place with a bot but this is unfair, unlike basra
            if (onlineRepo.IsUserActive(Context.UserIdentifier))
                ActiveUser.IsDisconnected = false;
            else
                CreateActiveUser();

            await InitClientGame();

            await base.OnConnectedAsync();
        }

        private void CreateActiveUser()
        {
            onlineRepo.AddActiveUser(new ActiveUser(Context.UserIdentifier, Context.ConnectionId,
                typeof(UserDomain.App.Lobby.Idle)));
        }

        private async Task InitClientGame()
        {
            var user = await offlineRepo.GetUserByIdAsyc(Context.UserIdentifier);
            var clientPersonalInfo = Mapper.ConvertUserDataToClient(user);
            //you travel to db 2 more times
            clientPersonalInfo.Followers =
                await offlineRepo.GetFollowersAsync(Context.UserIdentifier);
            clientPersonalInfo.Followings =
                await offlineRepo.GetFollowingsAsync(Context.UserIdentifier);

            await Clients.Caller.SendAsync("InitGame", ++ActiveUser.MessageIndex, clientPersonalInfo,
                ActiveUser.MessageIndex);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            logger.LogInformation($"{Context.UserIdentifier} Disconnected");

            ActiveUser.Disconnect();

            //remove pending room user
            if (RoomUser != null && !RoomUser.Room.IsFull)
                matchMaker.RemovePendingDisconnectedUser(RoomUser);

            //RoomUser.Room is null when he was the last player in pending room and disconnected

            //mark user in room as disconnected
            if (RoomUser is {Room: { }}) //todo test get non existing user
                //this means the room user is not null it's room is not also
                ActiveUser.IsDisconnected = true;
            else
                onlineRepo.RemoveActiveUser(Context.UserIdentifier);

            await base.OnDisconnectedAsync(exception);
        }

        #endregion

        private RoomUser roomUser;

        private RoomUser RoomUser =>
            roomUser ??= onlineRepo.GetRoomUserWithId(Context.UserIdentifier);

        private ActiveUser activeUser;

        private ActiveUser ActiveUser =>
            activeUser ??= onlineRepo.GetActiveUser(Context.UserIdentifier);

        #region general

        [RpcDomain(typeof(UserDomain.App))]
        public async Task<PersonalFullUserInfo> GetPersonalUserData()
        {
            return Mapper.ConvertUserDataToClient(
                await offlineRepo.GetUserByIdAsyc(Context.UserIdentifier));
        }

        /// <summary>
        /// get public user data by his id
        /// </summary>
        [RpcDomain(typeof(UserDomain.App))]
        public async Task<FullUserInfo> GetUserData(string id)
        {
            var data = await offlineRepo.GetFullUserInfoAsync(id);
            data.Friendship = (int) offlineRepo.GetFriendship(Context.UserIdentifier, id);
            return data;
        }

        [RpcDomain(typeof(UserDomain.App))]
        public async Task ToggleFollow(string targetId)
        {
            offlineRepo.ToggleFollow(Context.UserIdentifier, targetId);
            await offlineRepo.SaveChangesAsync();
        }

        [RpcDomain(typeof(UserDomain.App))]
        public async Task ToggleOpenMatches()
        {
            var user = await offlineRepo.GetUserByIdAsyc(Context.UserIdentifier);
            user.EnableOpenMatches = !user.EnableOpenMatches;
            await offlineRepo.SaveChangesAsync();
        }

        #endregion

        #region lobby

        [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        public async Task MakePurchase(string purchaseData, string sign)
        {
            await lobbyManager.MakePurchase(ActiveUser, purchaseData, sign);
        }

        [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        public async Task RequestRandomRoom(int betChoice, int capacityChoice)
        {
            await matchMaker.RequestRandomRoom(betChoice, capacityChoice, ActiveUser);
        }

        [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        public async Task<Services.MatchMaker.MatchRequestResult> RequestMatch(string oppoId)
        {
            return await matchMaker.RequestMatch(ActiveUser, oppoId);
        }

        [RpcDomain(typeof(UserDomain.App.Lobby.Pending))]
        public void CancelChallengeRequest(string oppoId)
        {
            matchMaker.CancelChallengeRequest(ActiveUser);
        }

        [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        public async Task<Services.MatchMaker.ChallengeResponseResult> RespondChallengeRequest
            (string senderId, bool response)
        {
            return await matchMaker.RespondChallengeRequest(ActiveUser, response, senderId);
        }

        [RpcDomain(typeof(UserDomain.App.Lobby.GettingReady))]
        public async Task Ready()
        {
            await matchMaker.MakeRoomUserReadyRpc(ActiveUser, RoomUser);
        }

        [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        public async Task AskForMoneyAid()
        {
            await lobbyManager.RequestMoneyAid(ActiveUser);
        }

        [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        public async Task ClaimMoneyAid()
        {
            await lobbyManager.ClaimMoneyAim(ActiveUser);
        }

        [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        public async Task BuyCardback(int cardbackId)
        {
            await lobbyManager.BuyCardBack(cardbackId, ActiveUser.Id);
        }

        [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        public async Task BuyBackground(int backgroundId)
        {
            await lobbyManager.BuyBackground(backgroundId, ActiveUser.Id);
        }

        [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        public async Task SelectCardback(int cardbackId)
        {
            await lobbyManager.SelectCardback(cardbackId, ActiveUser.Id);
        }

        [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        public async Task SelectBackground(int backgroundId)
        {
            await lobbyManager.SelectBackground(backgroundId, ActiveUser.Id);
        }

        #endregion

        #region room

        //todo currently there's no validation on the message id, so it can be any string
        //how this could be misused? 1- sending very big message to the receiving client to stop his game!
        [RpcDomain(typeof(UserDomain.App.Room.Active))]
        public async Task ShowMessage(string msgId)
        {
            await roomManager.ShowMessage(RoomUser, msgId);
        }

        [RpcDomain(typeof(UserDomain.App.Room.Active))]
        public async Task UpStreamChar(IAsyncEnumerable<char> stream)
        {
            await roomManager.UpStreamChar(stream);
        }

        [RpcDomain(typeof(UserDomain.App.Room.Active))]
        public async IAsyncEnumerable<List<char>[]> DownStreamCharBuffer(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            //other cool thing could be sending the current char index for each, why? because this is stateless
            //putting anti-cheating in mind: if the client know his chars, he can send them all at once
            //a better option would be: not sending all: like? send one get one(word)
            //get five by five? client buffer! but the problem with it is?
            //overall is it even possible to get around this? I don't think so because it is easy to 
            //know the current letter buffer and send, the only blocking thing would the network
            //however sending the no letter is a little dumb, because it won't make me able to make statistics 
            //and easier to cheat the system
            //the anti-cheat will work on the whole paragraph because of the network delay make shoot many chars at once
            //so the only thing I can make about the hacking is limit wpm to 250 or 300

            //A. each player send a number represent how many char are moved forward in a fixed update
            //B. in an inner loop check if any player is moved forward
            //-- I think a fixed update is always there even if we ignored it because it is very fast

            while (true) //send as long as the channel is opened
            {
                var player = scopeInfo.RoomUser;
                var room = player.Room;

                // Check the cancellation token regularly so that the server will stop
                // producing items if the client disconnects.
                cancellationToken.ThrowIfCancellationRequested();

                var subBuffer = new List<char>[room.Capacity];

                for (var i = 0; i < subBuffer.Length; i++)
                {
                    var pointer = player.StreamPointer[i];
                    var currentBuffer = room.CharBuffer[i];
                    subBuffer[i] = currentBuffer.GetRange(pointer, currentBuffer.Count - pointer);
                    //can return empty list

                    player.StreamPointer[i] = currentBuffer.Count;
                }

                yield return subBuffer;

                //foreach (var list in charBuffer) list.Clear();

                // Use the cancellationToken in other APIs that accept cancellation
                // tokens so the cancellation can flow down to them.
                await Task.Delay(10, cancellationToken);
            }
            // ReSharper disable once IteratorNeverReturns
        }

        #endregion


        [RpcDomain(typeof(UserDomain.App))]
        public void BuieTest()
        {
            throw new Exceptions.BadUserInputException("this the exc message");
        }

        [RpcDomain(typeof(UserDomain.App))]
        public void ThrowExc()
        {
            throw new Exception("a test general exc is thrown");
        }

        [RpcDomain(typeof(UserDomain.App))]
        public async Task<MinUserInfo> TestReturnObject()
        {
            await Task.Delay(5000);
            return new MinUserInfo {Name = "some data to test"};
        }

        [RpcDomain(typeof(UserDomain.App))]
        public async Task TestWaitAlot()
        {
            await Task.Delay(5000);
        }

        public class MethodDomains
        {
            public MethodDomains()
            {
                var rpcs =
                    typeof(MasterHub).GetMethods(BindingFlags.Public | BindingFlags.Instance);

                foreach (var rpc in rpcs)
                {
                    var attribute = rpc.GetCustomAttribute<RpcDomainAttribute>();

                    if (attribute == null) continue;

                    Domains.Add(rpc.Name, attribute.Domain);
                }
            }

            private Dictionary<string, Type> Domains { get; } = new();

            public Type GetDomain(string method)
            {
                return !Domains.ContainsKey(method) ? null : Domains[method];
                // throw new Exception("the request function is not listed in the hub public methods");
            }
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false)]
        private sealed class RpcDomainAttribute : Attribute
        {
            public Type Domain { get; }

            public RpcDomainAttribute(Type domain)
            {
                Domain = domain;
            }
        }
    }
}