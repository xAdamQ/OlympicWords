using Microsoft.AspNetCore.SignalR;
using System.Reflection;
using OlympicWords.Common;
using System.Runtime.CompilerServices;
using OlympicWords.Services.Helpers;
using static System.Threading.Tasks.Task;

namespace OlympicWords.Services
{
    public interface IMasterHub
    {
        Task OnConnectedAsync();
        Task OnDisconnectedAsync(Exception exception);
        Task<PersonalFullUserInfo> GetPersonalUserData();
        /// <summary>
        /// get public user data by his id
        /// </summary>
        Task<FullUserInfo> GetUserData(string id);
        Task ToggleFollow(string targetId);
        Task ToggleOpenMatches();
        Task MakePurchase(string purchaseData, string sign);
        Task RequestRandomRoom(int betChoice, int capacityChoice);
        Task<Services.MatchMaker.MatchRequestResult> RequestMatch(string oppoId);
        void CancelChallengeRequest(string oppoId);
        Task<Services.MatchMaker.ChallengeResponseResult> RespondChallengeRequest
            (string senderId, bool response);
        Task Ready();
        Task AskForMoneyAid();
        Task ClaimMoneyAid();
        Task BuyCardback(int cardbackId);
        Task BuyBackground(int backgroundId);
        Task SelectCardback(int cardbackId);
        Task SelectBackground(int backgroundId);
        Task ShowMessage(string msgId);
        Task<string> UpStreamChar(IAsyncEnumerable<char> stream);
        IAsyncEnumerable<string[]> DownStreamCharBuffer(
            [EnumeratorCancellation] CancellationToken cancellationToken);
        IAsyncEnumerable<int> DownStreamTest(
            [EnumeratorCancellation] CancellationToken cancellationToken);
        Task Surrender();
        void LeaveFinishedRoom();
        void BuieTest();
        void ThrowExc();
        Task<MinUserInfo> TestReturnObject();
        Task TestWaitAlot();
        Task<string> UpStreamCharTest(IAsyncEnumerable<char> stream);

        void SetPowerUp(int powerUp);

        // Task SmallJetJump();
        // Task MegaJetJump();
    }

    public class MasterHub : Hub, IMasterHub
    {
        #region services

        private readonly IOfflineRepo offlineRepo;
        private readonly IScopeRepo scopeRepo;
        private readonly IGameplay gameplay;
        private readonly IMatchMaker matchMaker;
        private readonly ILogger<MasterHub> logger;
        private readonly IChatManager chatManager;
        private readonly PersistantData persistantData;
        private readonly IFinalizer finalizer;
        private readonly ILobbyManager lobbyManager;


        public MasterHub(IOfflineRepo offlineRepo, ILobbyManager lobbyManager, IScopeRepo scopeRepo,
            IGameplay gameplay, IMatchMaker matchMaker, ILogger<MasterHub> logger, IChatManager chatManager,
            PersistantData persistantData, IFinalizer finalizer)
        {
            this.offlineRepo = offlineRepo;
            this.lobbyManager = lobbyManager;
            this.scopeRepo = scopeRepo;
            this.gameplay = gameplay;
            this.matchMaker = matchMaker;
            this.logger = logger;
            this.chatManager = chatManager;
            this.persistantData = persistantData;
            this.finalizer = finalizer;
        }

        #endregion

        #region on dis/connceted

        public override async Task OnConnectedAsync()
        {
            logger.LogInformation($"connection established: {Context.UserIdentifier}");

            persistantData.FeedScope(scopeRepo);
            scopeRepo.SetOwner(userId: Context.UserIdentifier);

            //this feature was related to the active user functionality, you may restructure things because
            //no disconnected user which is active in a room, I can fill his place with a bot but this is unfair, unlike basra
            if (scopeRepo.IsUserActive(Context.UserIdentifier))
                ActiveUser.IsDisconnected = false;
            else
                CreateActiveUser();

            await TestSetUserMoney();

            // await InitClientGame();

            await base.OnConnectedAsync();
        }

        private async Task TestSetUserMoney()
        {
            var u = await offlineRepo.GetCurrentUser();
            u.Money = 99999999;
            await offlineRepo.SaveChangesAsync();
        }

        private void CreateActiveUser()
        {
            scopeRepo.AddActiveUser(new ActiveUser(Context.UserIdentifier, Context.ConnectionId,
                typeof(UserDomain.App.Lobby.Idle)));
        }

        // private async Task InitClientGame()
        // {
        //     var user = await offlineRepo.GetUserByIdAsyc(Context.UserIdentifier,
        //         withFollowings: true, withFollowers: true);
        //
        //     var clientPersonalInfo = Mapper.ConvertUserDataToClient(user);
        //
        //     await Clients.Caller.SendAsync("InitGame", ActiveUser.MessageIndex++,
        //         clientPersonalInfo);
        // }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            logger.LogInformation("{ContextUserIdentifier} Disconnected", Context.UserIdentifier);

            persistantData.FeedScope(scopeRepo);
            scopeRepo.SetOwner(userId: Context.UserIdentifier);

            ActiveUser.Disconnect();

            var roomUser = scopeRepo.RoomUser;
            if (roomUser != null)
            {
                await finalizer.Surrender();
                //doesn't matter if you disconnected or got out by net issue

                //remove pending room user
                if (!roomUser.Room.IsFull)
                    matchMaker.RemovePendingDisconnectedUser(roomUser);
                //RoomUser.Room is null when he was the last player in pending room and disconnected
            }

            scopeRepo.RemoveActiveUser(Context.UserIdentifier);

            await base.OnDisconnectedAsync(exception);
        }

        #endregion


        private ActiveUser activeUser;

        private ActiveUser ActiveUser =>
            activeUser ??= scopeRepo.GetActiveUser(Context.UserIdentifier);

        #region general

        [RpcDomain(typeof(UserDomain.App))]
        public async Task<PersonalFullUserInfo> GetPersonalUserData()
        {
            var user = await offlineRepo.GetUserByIdAsyc(Context.UserIdentifier,
                withFollowings: true, withFollowers: true);

            return Mapper.ConvertUserDataToClient(user);
        }

        /// <summary>
        /// get public user data by his id
        /// </summary>
        [RpcDomain(typeof(UserDomain.App))]
        public async Task<FullUserInfo> GetUserData(string id)
        {
            var data = await offlineRepo.GetFullUserInfoAsync(id);
            data.Friendship = (int)await offlineRepo.GetFriendship(Context.UserIdentifier, id);
            return data;
        }

        [RpcDomain(typeof(UserDomain.App))]
        public async Task ToggleFollow(string targetId)
        {
            var user = await offlineRepo.GetCurrentUser();
            var target = await offlineRepo.GetUserByIdAsyc(targetId);

            await offlineRepo.ToggleFollow(user, target);

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
        public async Task<MatchMaker.MatchRequestResult> RequestMatch(string oppoId)
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
            await matchMaker.MakeRoomUserReadyRpc();
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

        [RpcDomain(typeof(UserDomain.App.Lobby.Pending))]
        public void SetPowerUp(int powerUp)
        {
            scopeRepo.RoomActor.ChosenPowerUp = powerUp;
        }

        #endregion

        #region room

        //todo currently there's no validation on the message id, so it can be any string
        //how this could be misused? 1- sending very big message to the receiving client to stop his game!
        [RpcDomain(typeof(UserDomain.App.Room.Active))]
        public async Task ShowMessage(string msgId)
        {
            await chatManager.ShowMessage(msgId);
        }

        [RpcDomain(typeof(UserDomain.App.Room.Active))]
        public async Task<string> UpStreamChar(IAsyncEnumerable<char> stream)
        {
            return await gameplay.UpStreamChar(stream);
        }

        [RpcDomain(typeof(UserDomain.App.Room.Active))]
        public async IAsyncEnumerable<string[]> DownStreamCharBuffer(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var asyncCoroutine = gameplay.DownStreamCharBuffer(cancellationToken);
            await foreach (var item in asyncCoroutine.WithCancellation(cancellationToken))
                yield return item;
        }


        [RpcDomain(typeof(UserDomain.App.Room.Active))]
        public async IAsyncEnumerable<int> DownStreamTest(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < 100; i++)
            {
                if (cancellationToken.IsCancellationRequested) yield break;

                yield return i;

                await Delay(8000000);
            }
        }

        [RpcDomain(typeof(UserDomain.App.Room.Active))]
        public async Task Surrender()
        {
            await finalizer.Surrender();
        }
        //
        // [RpcDomain(typeof(UserDomain.App.Room.Active))]
        // public async Task SmallJetJump()
        // {
        //     await scopeRepo.RoomActor.SmallJetJump(finalizer);
        // }
        // [RpcDomain(typeof(UserDomain.App.Room.Active))]
        // public async Task MegaJetJump()
        // {
        //     await scopeRepo.RoomActor.MegaJetJump(finalizer);
        // }

        #endregion

        #region finished room

        [RpcDomain(typeof(UserDomain.App.Room.Finished))]
        public void LeaveFinishedRoom()
        {
            scopeRepo.RemoveRoomUser();
            scopeRepo.ActiveUser.Domain = typeof(UserDomain.App.Lobby.Idle);
        }

        #endregion

        #region tests

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
            await Delay(5000);
            return new MinUserInfo { Name = "some data to test" };
        }

        [RpcDomain(typeof(UserDomain.App))]
        public async Task TestWaitAlot()
        {
            await Delay(5000);
        }

        [RpcDomain(typeof(UserDomain.App))]
        public async Task<string> UpStreamCharTest(IAsyncEnumerable<char> stream)
        {
            // await foreach (var chr in stream)
            // logger.LogInformation("received {Chr}", chr);

            logger.LogInformation("test stream done");
            return "done";
        }

        #endregion

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