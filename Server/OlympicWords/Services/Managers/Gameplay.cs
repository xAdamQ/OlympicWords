using System.Runtime.CompilerServices;
using OlympicWords.Common;
using Microsoft.AspNetCore.SignalR;
using OlympicWords.Services.Extensions;
// using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;

namespace OlympicWords.Services
{
    public interface IGameplay
    {
        Task UpStreamChar(IAsyncEnumerable<char> stream);

        /// <summary>
        /// after ready, change domain, initial distribute, start 0 player turn
        /// </summary>
        Task StartRoom();

        IAsyncEnumerable<List<char>[]> DownStreamCharBuffer(
            [EnumeratorCancellation] CancellationToken cancellationToken);

        Task ProcessDigit(char chr, RoomActor roomActor);
        Task Surrender();
    }

    /// <summary>
    /// handle active/started room
    /// </summary>
    public class Gameplay : IGameplay
    {
        private readonly IHubContext<MasterHub> masterHub;
        private readonly ILogger<Gameplay> logger;
        private readonly IFinalizer finalizer;
        private readonly IServerLoop serverLoop;
        private readonly IScopeRepo scopeRepo;


        public Gameplay(IHubContext<MasterHub> masterHub, ILogger<Gameplay> logger,
            IFinalizer finalizer, IServerLoop serverLoop, IScopeRepo scopeRepo)
        {
            this.masterHub = masterHub;
            this.logger = logger;
            this.finalizer = finalizer;
            this.serverLoop = serverLoop;
            this.scopeRepo = scopeRepo;
        }

        public async Task StartRoom()
        {
            var room = scopeRepo.Room;

            if (room.Started)
                throw new BadUserInputException("the start room is called more than once");

            room.Started = true;

            room.SetUsersDomains(typeof(UserDomain.App.Room.Active));

            foreach (var roomBot in room.Bots)
            {
                serverLoop.BotLoop(roomBot, room.cancellationTokenSource.Token);
            }

            foreach (var roomUser in room.RoomUsers)
                await masterHub.SendOrderedAsync(roomUser.ActiveUser, "StartRoomRpc");

            room.RoomActors.ForEach(ru => ru.StartTime = DateTime.Now);
        } //no test

        //todo limit the sent chars count
        //todo check make sure they are only basic chars, no special ones, you can insert unsupported instead for every char 
        public async Task UpStreamChar(IAsyncEnumerable<char> stream)
        {
            var roomUser = scopeRepo.RoomUser;

            try
            {
                await foreach (var chr in stream.WithCancellation(roomUser.Cancellation.Token))
                {
                    if (roomUser.CharBuffer.Count > RoomActor.MAX_BUFFER)
                        throw new BadUserBehaviourException(
                            "you sent too many characters, you're sent out of the room");

                    await ProcessDigit(chr, roomUser);
                    //processing is increasing the pointer at each player and finalize at the last digit
                }
            }
            catch (OperationCanceledException e)
            {
                logger.LogInformation($"user: {roomUser.Id} has cancelled the stream");
            }
        }

        public async Task ProcessDigit(char chr, RoomActor roomActor)
        {
            logger.LogInformation("processing: " + chr);

            var room = roomActor.Room;

            roomActor.CharBuffer.Add(chr);

            if (room.Text[roomActor.StreamPointer] == chr)
            {
                roomActor.StreamPointer++;
            }

            if (roomActor.StreamPointer == room.Text.Length)
            {
                await finalizer.FinalizeUser();
            }
        }


        //todo change list here to something more lightweight
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
            var roomUser = scopeRepo.RoomUser;
            var room = scopeRepo.Room;

            while (!roomUser.Cancellation.IsCancellationRequested) //send as long as the channel is opened
                //this token is set by the server
            {
                // Check the cancellation token regularly so that the server will stop
                // producing items if the client disconnects.
                if (cancellationToken.IsCancellationRequested)
                    //this token is set by the client
                    break;

                var updateBuffer = new List<char>[room.Capacity];

                // attempt to send the real digits including the failed ones
                for (var u = 0; u < room.Capacity; u++)
                {
                    var pointer = roomUser.StreamSyncPointers[u];
                    var userBuffer = room.RoomActors[u].CharBuffer;

                    var count = userBuffer.Count - pointer;

                    updateBuffer[u] = userBuffer.GetRange(pointer, count);
                    //can return empty list

                    roomUser.StreamSyncPointers[u] += updateBuffer[u].Count;
                }

                if (updateBuffer.All(b => b.Count == 0)) continue;

                logger.LogInformation($"received digits {JsonConvert.SerializeObject(updateBuffer, Formatting.None)}");

                yield return updateBuffer;

                // Use the cancellationToken in other APIs that accept cancellation
                // tokens so the cancellation can flow down to them.
                await Task.Delay(100, cancellationToken);
            }
        }

        public async Task Surrender()
        {
            scopeRepo.RemoveRoomUser();
            await finalizer.SurrenderFinalization();
        }
    }
}