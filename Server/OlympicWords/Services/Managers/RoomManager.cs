using Basra.Common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OlympicWords;
using OlympicWords.Services.Extensions;
using BadUserInputException = OlympicWords.Services.Exceptions.BadUserInputException;


namespace OlympicWords.Services
{
    public interface IRoomManager
    {
        Task UpStreamChar(IAsyncEnumerable<char> stream);

        /// <summary>
        /// after ready, change domain, initial distribute, start 0 player turn
        /// </summary>
        Task StartRoom(Room room);

        /// <summary>
        /// when the client is aware that he missed his turn, it sends this message to make
        /// force play faster
        /// </summary>
        Task MissTurnRpc(RoomUser roomUser);

        /// <summary>
        /// is called from timeout
        /// </summary>
        Task ForceUserPlay(RoomUser roomUser);

        /// <summary>
        /// this is the throw function
        /// </summary>
        Task UserPlayRpc(RoomUser roomUser, int cardIndexInHand);

        /// <summary>
        /// is called from timeout
        /// </summary>
        Task BotPlay(RoomBot roomBot);

        Task ShowMessage(RoomUser roomUser, string msgId);
    }

    /// <summary>
    /// handle active/started room
    /// </summary>
    public class RoomManager : IRoomManager
    {
        private readonly IHubContext<MasterHub> masterHub;
        private readonly IOfflineRepo offlineRepo;
        private readonly IServerLoop serverLoop;
        private readonly ILogger<RoomManager> logger;
        private readonly IFinalizeManager finalizeManager;

        private readonly IScopeInfo
            scopeInfo; //todo I think I will remove all repos and leave the scope info do the caching for asking for data in the scope

        public RoomManager(IHubContext<MasterHub> masterHub, IOfflineRepo offlineRepo,
            IServerLoop serverLoop, ILogger<RoomManager> logger, IFinalizeManager finalizeManager, IScopeInfo scopeInfo)
        {
            this.masterHub = masterHub;
            this.offlineRepo = offlineRepo;
            this.serverLoop = serverLoop;
            this.logger = logger;
            this.finalizeManager = finalizeManager;
            this.scopeInfo = scopeInfo;
        }

        public async Task UpStreamChar(IAsyncEnumerable<char> stream)
        {
            var user = scopeInfo.RoomUser;
            var room = user.Room;

            await foreach (var chr in stream)
            {
                room.CharBuffer[user.Index].Add(chr);
                logger.LogInformation($"the send char is: {chr}");
            }
        }

        public async Task StartRoom(Room room)
        {
            if (room.Started)
            {
                logger.LogWarning("the start room is called twice!");
                return;
            }

            room.Started = true;

            room.SetUsersDomains(typeof(UserDomain.App.Room.Active));
            GenerateRoomDeck(room);

            InitialTurn(room);
            await InitialDistribute(room);
        } //no test

        private void GenerateRoomDeck(Room room)
        {
            room.Deck = new List<int>(Enumerable.Range(0, Room.DeckSize));
            room.Deck.Shuffle();
        }

        private void InitialTurn(Room room)
        {
            var roomActor = room.RoomActors[room.CurrentTurn];

            if (roomActor is RoomUser roomUser)
            {
                serverLoop.SetupTurnTimeout(roomUser);
            }
            else
            {
                serverLoop.BotPlay(roomActor as RoomBot);
            }
        }

        private async Task InitialDistribute(Room room)
        {
            room.GroundCards = room.Deck.CutRange(RoomActor.HandSize);

            foreach (var roomActor in room.RoomActors)
                roomActor.Hand = room.Deck.CutRange(RoomActor.HandSize);

            foreach (var roomUser in room.RoomUsers)
                await masterHub.SendOrderedAsync(roomUser.ActiveUser, "StartRoomRpc", roomUser.Hand,
                    roomUser.Room.GroundCards);
        } //the cut part can be tested, but it's relatively easy

        private async Task Distribute(Room room)
        {
            foreach (var roomActor in room.RoomActors)
                roomActor.Hand = roomActor.Room.Deck.CutRange(RoomActor.HandSize);

            var callName = room.Deck.Count > 0 ? "Distribute" : "LastDistribute";

            foreach (var roomUser in room.RoomUsers)
                await masterHub.SendOrderedAsync(roomUser.ActiveUser, callName, roomUser.Hand);
        } //trivial to test

        private async Task NextTurn(Room room)
        {
            room.CurrentTurn = ++room.CurrentTurn % room.Capacity;
            var actorInTurn = room.RoomActors[room.CurrentTurn];

            if (actorInTurn.Hand.Count == 0)
            {
                if (actorInTurn.Room.Deck.Count == 0)
                {
                    await finalizeManager.FinalizeRoom(actorInTurn.Room);
                    return;
                }
                else
                {
                    await Distribute(actorInTurn.Room);
                }
            }

            if (actorInTurn is RoomUser roomUser)
            {
                if (roomUser.ActiveUser.IsDisconnected) await ForceUserPlay(roomUser);
                else serverLoop.SetupTurnTimeout(roomUser);
            }
            else
            {
                serverLoop.BotPlay(actorInTurn as RoomBot);
                //this is correct because sever loop is singleton not scoped
            }
        }

        private ThrowResult PlayBase(RoomActor roomActor, int cardIndexInHand)
        {
            var eaten = Eat(roomActor.Hand[cardIndexInHand], roomActor.Room.GroundCards,
                out var basra,
                out var bigBasra);

            var card = roomActor.Hand.Cut(cardIndexInHand);

            if (eaten != null && eaten.Count != 0)
            {
                roomActor.Room.LastEater = roomActor;

                roomActor.Room.GroundCards.RemoveAll(c => eaten.Contains(c));

                roomActor.EatenCardsCount += eaten.Count + 1; //1 is my card
                if (basra) roomActor.BasraCount++;
                if (bigBasra) roomActor.BigBasraCount++;
            }
            else
            {
                roomActor.Room.GroundCards.Add(card);
            }

            return new ThrowResult
            {
                ThrownCard = card,
                Basra = basra,
                BigBasra = bigBasra,
                EatenCardsIds = eaten,
            };
        }

        private async Task SendCurrentOppoThrow(RoomActor roomActor, ThrowResult throwResult)
        {
            foreach (var otherAu in roomActor.Room.RoomUsers
                         .Where(ru => ru != roomActor).Select(ru => ru.ActiveUser))
                await masterHub.SendOrderedAsync(otherAu, "CurrentOppoThrow", throwResult);
        }

        public async Task UserPlayRpc(RoomUser roomUser, int cardIndexInHand)
        {
            if (roomUser.TurnId != roomUser.Room.CurrentTurn ||
                !General.IsInRange(cardIndexInHand, roomUser.Hand.Count))
                throw new Exceptions.BadUserInputException();
            //this is invoked by the server also, and may be a server error and it's handle way is ignoring and
            //terminate the action hub exc are not handled when the actor is the system

            await UserPlay(roomUser, cardIndexInHand);
        }

        private async Task UserPlay(RoomUser roomUser, int cardIndexInHand)
        {
            serverLoop.CancelTurnTimeout(roomUser);

            var throwResult = PlayBase(roomUser, cardIndexInHand);

            await masterHub.SendOrderedAsync(roomUser.ActiveUser, "MyThrowResult", throwResult);
            await SendCurrentOppoThrow(roomUser, throwResult);

            logger.LogInformation(
                $"user has played card {cardIndexInHand} with value {throwResult.ThrownCard} userId {roomUser.Id}");

            await NextTurn(roomUser.Room);
        }

        public async Task MissTurnRpc(RoomUser roomUser)
            //the difference is that rpc contains validation
        {
            if (roomUser.TurnId != roomUser.Room.CurrentTurn)
                //this check is done by the domain, but i think it should be done like this because we already have the turn stuff here
                //so we don't maintain in both

                throw new Exceptions.BadUserInputException();

            await RandomUserPlay(roomUser, notification: true);
        }

        public async Task ForceUserPlay(RoomUser roomUser)
        {
            await RandomUserPlay(roomUser, notification: false);
        }

        /// <param name="notification">
        /// if client side send he missed his turn, or if it happened because of internal timout
        /// </param>
        private async Task RandomUserPlay(RoomUser roomUser, bool notification)
        {
            var randomCardIndex = StaticRandom.GetRandom(roomUser.Hand.Count);

            // await UserPlay(roomUser, randomCardIndex);

            if (notification)
                serverLoop.CancelTurnTimeout(roomUser);

            var throwResult = PlayBase(roomUser, randomCardIndex);


            if (!roomUser.ActiveUser.IsDisconnected)
                await masterHub.SendOrderedAsync(roomUser.ActiveUser, "ForcePlay", throwResult);

            //todo then you have to do the same assertion on this!

            await SendCurrentOppoThrow(roomUser, throwResult);


            await NextTurn(roomUser.Room);
        }
        //no test, you can test the "concurrent random"

        public async Task BotPlay(RoomBot roomBot)
        {
            //can happen with logic
            var randomCardIndex = StaticRandom.GetRandom(roomBot.Hand.Count);

            var throwResult = PlayBase(roomBot, randomCardIndex);

            await SendCurrentOppoThrow(roomBot, throwResult);

            await NextTurn(roomBot.Room);
            logger.LogInformation($"bot {roomBot.Id} has played card {randomCardIndex}");
        } //no test, you can test the "concurrent random"

        private const int KomiId = 19, BoyValue = 11;
        private static readonly int[] BoyIds = {10, 23, 36, 49};

        public static List<int> Eat(int cardId, List<int> ground, out bool basra,
            out bool bigBasra)
        {
            basra = false;
            bigBasra = false;

            var cardValue = CardValueFromId(cardId);

            if (cardId == KomiId)
            {
                basra = ground.Count == 1;

                return ground.ToList();
            }

            if (cardValue == BoyValue)
            {
                bigBasra = ground.TrueForAll(c => CardValueFromId(c) == BoyValue);

                return ground.ToList();
            }

            if (cardValue > 10)
            {
                var eaten = ground.Where(c => CardValueFromId(c) == cardValue).ToList();
                basra = eaten.Count != 0 && eaten.Count == ground.Count;
                return eaten;
            }

            var groups = ground.Permutations();
            var bestGroupLength = -1;
            var bestGroup = new List<int>();
            foreach (var group in groups)
            {
                if (group.Select(c => CardValueFromId(c)).Sum() == cardValue &&
                    group.Count > bestGroupLength)
                {
                    bestGroup = group;
                    bestGroupLength = bestGroup.Count;
                }
            }

            //since you're here
            basra = bestGroup.Count != 0 && bestGroup.Count == ground.Count;

            return bestGroup;

            int CardValueFromId(int id)
            {
                return (id % 13) + 1;
            }
        } //tested

        // public async Task Surrender(RoomUser roomUser)(RoomUser roomUser)
        // {
        //     var room = roomUser.Room;

        //     var currentActor = room.RoomActors[room.CurrentTurn];
        //     if (currentActor is RoomUser ru)
        //         _serverLoop.CancelTurnTimeout(ru);

        //     var otherUsers = room.RoomUsers.Where(ru => ru != roomUser);
        //     await Task.WhenAll(otherUsers.Select(u =>
        //         _masterHub.Clients.User(u.Id).SendAsync("UserSurrender", roomUser.TurnId)));
        //     //blocks the client and waits for finalize result

        //     // room.RoomActors.First(_ => _ == roomUser);

        //     // if(room)
        //     await _finalizeManager.FinalizeRoom(roomUser.Room, roomUser);
        // }


        private static readonly HashSet<string> EmojiIds = new()
        {
            "angle",
            "angry",
            "dead",
            "cry",
            "devil",
            "heart",
            "cat1",
            "cat2",
            "cat3",
            "moon",
            "mindBlow",
            "bigEye",
            "frog",
            "laughCry",
        };

        private static readonly HashSet<string> TextIds = new()
        {
            "soLucky",
            "comeAgain",
            "congrates",
            "tough",
            "kofta",
            "anyWords",
            "kossa",
        };

        public async Task ShowMessage(RoomUser roomUser, string msgId)
        {
            if (!EmojiIds.Contains(msgId) && !TextIds.Contains(msgId))
                throw new Exceptions.BadUserInputException("message Id is not valid");

            var oppos = roomUser.Room.RoomUsers.Where(u => u != roomUser).Select(u => u.ActiveUser);

            foreach (var oppo in oppos)
                await masterHub.SendOrderedAsync(oppo, "ShowMessage", roomUser.TurnId, msgId);
        }
        
    }
}