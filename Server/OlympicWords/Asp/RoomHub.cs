using Microsoft.AspNetCore.SignalR;
using OlympicWords.Common;
using System.Runtime.CompilerServices;
using NuGet.Protocol;
using OlympicWords.Services.Helpers;
using static System.Threading.Tasks.Task;

namespace OlympicWords.Services
{
    public interface IRoomHub
    {
        // Task OnConnectedAsync();
        // Task OnDisconnectedAsync(Exception exception);
        // /// <summary>
        // /// get public user data by his id
        // /// </summary>
        // Task MakePurchase(string purchaseData, string sign);
        // Task RequestRandomRoom(int betChoice, int capacityChoice);
        // Task<Services.MatchMaker.MatchRequestResult> RequestMatch(string oppoId);
        // void CancelChallengeRequest(string oppoId);
        // Task<Services.MatchMaker.ChallengeResponseResult> RespondChallengeRequest
        //     (string senderId, bool response);
        // Task Ready();
        // Task AskForMoneyAid();
        // Task ClaimMoneyAid();
        // Task BuyCardback(int cardbackId);
        // Task BuyBackground(int backgroundId);
        // Task SelectCardback(int cardbackId);
        // Task SelectBackground(int backgroundId);
        // Task ShowMessage(string msgId);
        // Task<string> UpStreamChar(IAsyncEnumerable<char> stream);
        // IAsyncEnumerable<string[]> DownStreamCharBuffer(CancellationToken cancellationToken);
        // IAsyncEnumerable<int> DownStreamTest(CancellationToken cancellationToken);
        // Task Surrender();
        // void LeaveFinishedRoom();
        //
        // void SetPowerUp(int powerUp);

        // Task SmallJetJump();
        // Task MegaJetJump();


#if UNITY
        Task
#else
        void
#endif
            SetPowerUp(int powerUp);
    }

    public class RoomHub : Hub, IRoomHub
    {
        #region services
        private readonly IOfflineRepo offlineRepo;
        private readonly IScopeRepo scopeRepo;
        private readonly IGameplay gameplay;
        private readonly IMatchMaker matchMaker;
        private readonly ILogger<RoomHub> logger;
        // private readonly IChatManager chatManager;
        private readonly PersistantData persistantData;
        private readonly IFinalizer finalizer;
        private readonly IHttpContextAccessor contextAccessor;
        private readonly ILobbyManager lobbyManager;


        public RoomHub(IOfflineRepo offlineRepo, ILobbyManager lobbyManager, IScopeRepo scopeRepo,
            IGameplay gameplay, IMatchMaker matchMaker, ILogger<RoomHub> logger,
            PersistantData persistantData, IFinalizer finalizer, IHttpContextAccessor contextAccessor)
        {
            this.offlineRepo = offlineRepo;
            this.lobbyManager = lobbyManager;
            this.scopeRepo = scopeRepo;
            this.gameplay = gameplay;
            this.matchMaker = matchMaker;
            this.logger = logger;
            this.persistantData = persistantData;
            this.finalizer = finalizer;
            this.contextAccessor = contextAccessor;
        }
        #endregion

        #region on dis/connceted
        public override async Task OnConnectedAsync()
        {
            // if (contextAccessor.HttpContext == null)
            // return;

            await base.OnConnectedAsync();

            logger.LogInformation("connection established: {ContextUserIdentifier}, connId {ConnId}",
                Context.UserIdentifier, Context.ConnectionId);

            //the scope of the auth middleware is detached from the hub scope I think!!
            if (scopeRepo.UserId == null)
            {
                scopeRepo.SetRealOwner(Context.UserIdentifier);
                persistantData.FeedScope(scopeRepo);
            }

            //if you will make the other user  surrender, then handle multiple connections here, otherwise handle it 
            //in the middleware
            // if (scopeRepo.IsUserPending() ||
            //     (scopeRepo.DoesRoomUserExist(Context.UserIdentifier) && scopeRepo.RoomUser.Active == false))
            // {
            //     //todo this is totally wrong because I am ot sure if all termination calls are done and the previous
            //     //connection ic cleaned by doing this
            //     await finalizer.Surrender();
            //     logger.LogInformation(
            //         "the user {ContextUserIdentifier} is pending or active but trying to connect again," +
            //         " so the original connection is terminated", Context.UserIdentifier);
            // }

            var betChoiceString = Context.GetHttpContext()!.Request.Query["betChoice"];
            var capacityChoiceString = Context.GetHttpContext()!.Request.Query["capacityChoice"];

            if (int.TryParse(betChoiceString, out var betChoice) &&
                int.TryParse(capacityChoiceString, out var capacityChoice))
                await matchMaker.RequestRandomRoom(betChoice, capacityChoice);
            else
            {
                Context.Abort();
                throw new BadUserInputException("failed to parses the capacity and/or bet choices");
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            logger.LogInformation("{ContextUserIdentifier} Disconnected", Context.UserIdentifier);

            //this one doesn't require auth so the scope is not fed
            persistantData.FeedScope(scopeRepo);
            scopeRepo.SetRealOwner(Context.UserIdentifier);

            await finalizer.Surrender();
            //doesn't matter if you disconnected or got out by net issue

            scopeRepo.RoomUser.Disconnect();
            scopeRepo.RemoveRoomUser();

            if (scopeRepo.IsUserPending())
                scopeRepo.RemovePendingUser();

            //this means the room is pending
            if (!scopeRepo.RoomUser.Room.IsFull)
                matchMaker.RemovePendingDisconnectedUser(scopeRepo.RoomUser);

            await base.OnDisconnectedAsync(exception);
        }
        #endregion

        [RpcDomain(typeof(UserDomain.Room.Active))]
        public async Task<string> UpStreamChar(IAsyncEnumerable<char> stream)
        {
            return await gameplay.UpStreamChar(stream);
        }

        [RpcDomain(typeof(UserDomain.Room.Active))]
        public async IAsyncEnumerable<string[]> DownStreamCharBuffer
            ([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var asyncCoroutine = gameplay.DownStreamCharBuffer(cancellationToken);
            await foreach (var item in asyncCoroutine.WithCancellation(cancellationToken))
                yield return item;
        }

        [RpcDomain(typeof(UserDomain.Room.Active))]
        public async Task Surrender()
        {
            await finalizer.Surrender();
            Context.Abort();
        }

        [RpcDomain(typeof(UserDomain.Room.Finished))]
        public void LeaveFinishedRoom()
        {
            Context.Abort();
            //the room user is removed when you disconnect automatically by the abort call
        }


        [RpcDomain(typeof(UserDomain.Room.Init.GettingReady))]
        public async Task Ready()
        {
            await matchMaker.MakeRoomUserReadyRpc();
        }

        //you can set the power up whether you're ready or not yet
        [RpcDomain(typeof(UserDomain.Room.Init))]
        public void SetPowerUp(int powerUp)
        {
            scopeRepo.RoomActor.ChosenPowerUp = powerUp;
        }

        #region tests
        // [RpcDomain(typeof(UserDomain.App))]
        // public void BuieTest()
        // {
        //     throw new Exceptions.BadUserInputException("this the exc message");
        // }
        //
        // [RpcDomain(typeof(UserDomain.App))]
        // public void ThrowExc()
        // {
        //     throw new Exception("a test general exc is thrown");
        // }
        //
        // [RpcDomain(typeof(UserDomain.App))]
        // public async Task<MinUserInfo> TestReturnObject()
        // {
        //     await Delay(5000);
        //     return new MinUserInfo { Name = "some data to test" };
        // }
        //
        // [RpcDomain(typeof(UserDomain.App))]
        // public async Task TestWaitAlot()
        // {
        //     await Delay(5000);
        // }
        //
        // [RpcDomain(typeof(UserDomain.App))]
        // public async Task<string> UpStreamCharTest(IAsyncEnumerable<char> stream)
        // {
        //     // await foreach (var chr in stream)
        //     // logger.LogInformation("received {Chr}", chr);
        //
        //     logger.LogInformation("test stream done");
        //     return "done";
        // }

        [RpcDomain(typeof(UserDomain.Stateless))]
        public void TestMessage()
        {
            logger.LogInformation("my connection id is {ConnectionId}", Context.ConnectionId);

            Clients.User(Context.UserIdentifier!).SendAsync("TestMessage");
            Clients.Client(Context.ConnectionId).SendAsync("TestMessage");
            Clients.Caller.SendAsync("TestMessage");
        }

        [RpcDomain(typeof(UserDomain.Room.Active))]
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
        #endregion

        #region old
        // //todo currently there's no validation on the message id, so it can be any string
        // //how this could be misused? 1- sending very big message to the receiving client to stop his game!
        // [RpcDomain(typeof(UserDomain.Room.Active))]
        // public async Task ShowMessage(string msgId)
        // {
        // await chatManager.ShowMessage(msgId);
        // }

        #region general
        // [RpcDomain(typeof(UserDomain.App))]
        // public async Task<PersonalFullUserInfo> GetPersonalUserData()
        // {
        //     var user = await offlineRepo.GetUserByIdAsyc(Context.UserIdentifier,
        //         withFollowings: true, withFollowers: true);
        //
        //     return Mapper.ConvertUserDataToClient(user);
        // }
        //
        // /// <summary>
        // /// get public user data by his id
        // /// </summary>
        // [RpcDomain(typeof(UserDomain.App))]
        // public async Task<FullUserInfo> GetUserData(string id)
        // {
        //     logger.LogInformation("my conn id is: {ConnectionId}", Context.ConnectionId);
        //
        //     var data = await offlineRepo.GetFullUserInfoAsync(id);
        //     data.Friendship = (int)await offlineRepo.GetFriendship(Context.UserIdentifier, id);
        //     return data;
        // }
        //
        // [RpcDomain(typeof(UserDomain.App))]
        // public async Task ToggleFollow(string targetId)
        // {
        //     var user = await offlineRepo.GetCurrentUser();
        //     var target = await offlineRepo.GetUserByIdAsyc(targetId);
        //
        //     await offlineRepo.ToggleFollow(user, target);
        //
        //     await offlineRepo.SaveChangesAsync();
        // }
        //
        // [RpcDomain(typeof(UserDomain.App))]
        // public async Task ToggleOpenMatches()
        // {
        //     var user = await offlineRepo.GetUserByIdAsyc(Context.UserIdentifier);
        //     user.EnableOpenMatches = !user.EnableOpenMatches;
        //     await offlineRepo.SaveChangesAsync();
        // }
        #endregion

        #region lobby
        // [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        // public async Task MakePurchase(string purchaseData, string sign)
        // {
        //     await lobbyManager.MakePurchase(scopeRepo.ActiveUser, purchaseData, sign);
        // }
        //
        // [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        // public async Task RequestRandomRoom(int betChoice, int capacityChoice)
        // {
        //     await matchMaker.RequestRandomRoom(betChoice, capacityChoice, scopeRepo.ActiveUser);
        // }
        //
        // [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        // public async Task<MatchMaker.MatchRequestResult> RequestMatch(string oppoId)
        // {
        //     return await matchMaker.RequestMatch(scopeRepo.ActiveUser, oppoId);
        // }
        //
        // [RpcDomain(typeof(UserDomain.App.Lobby.Pending))]
        // public void CancelChallengeRequest(string oppoId)
        // {
        //     matchMaker.CancelChallengeRequest(scopeRepo.ActiveUser);
        // }
        //
        // [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        // public async Task<Services.MatchMaker.ChallengeResponseResult> RespondChallengeRequest
        //     (string senderId, bool response)
        // {
        //     return await matchMaker.RespondChallengeRequest(scopeRepo.ActiveUser, response, senderId);
        // }
        //
        // [RpcDomain(typeof(UserDomain.App.Room.GettingReady))]
        // public async Task Ready()
        // {
        //     await matchMaker.MakeRoomUserReadyRpc();
        // }
        //
        // [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        // public async Task AskForMoneyAid()
        // {
        //     await lobbyManager.RequestMoneyAid(scopeRepo.ActiveUser);
        // }
        //
        // [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        // public async Task ClaimMoneyAid()
        // {
        //     await lobbyManager.ClaimMoneyAim(scopeRepo.ActiveUser);
        // }
        //
        // [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        // public async Task BuyCardback(int cardbackId)
        // {
        //     await lobbyManager.BuyCardBack(cardbackId, scopeRepo.ActiveUser.Id);
        // }
        //
        // [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        // public async Task BuyBackground(int backgroundId)
        // {
        //     await lobbyManager.BuyBackground(backgroundId, scopeRepo.ActiveUser.Id);
        // }
        //
        // [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        // public async Task SelectCardback(int cardbackId)
        // {
        //     await lobbyManager.SelectCardback(cardbackId, scopeRepo.ActiveUser.Id);
        // }
        //
        // [RpcDomain(typeof(UserDomain.App.Lobby.Idle))]
        // public async Task SelectBackground(int backgroundId)
        // {
        //     await lobbyManager.SelectBackground(backgroundId, scopeRepo.ActiveUser.Id);
        // }
        //
        #endregion
        #endregion
    }
}