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
        Task<string> UpStreamChar(IAsyncEnumerable<char> stream);

        /// <summary>
        /// after ready, change domain, initial distribute, start 0 player turn
        /// </summary>
        Task StartRoom();

        IAsyncEnumerable<string[]> DownStreamCharBuffer(CancellationToken cancellationToken);

        Task ProcessDigit(char chr);
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
                serverLoop.BotLoop(roomBot, room.cancellationTokenSource.Token);

            foreach (var roomUser in room.RoomUsers)
                await masterHub.SendOrderedAsync(roomUser.ActiveUser, "StartRoomRpc");

            room.RoomActors.ForEach(ru => ru.StartTime = DateTime.Now);
        } //no test

        //todo limit the sent chars count
        //todo check make sure they are only basic chars, no special ones, you can insert unsupported instead for every char 
        public async Task<string> UpStreamChar(IAsyncEnumerable<char> stream)
        {
            var roomUser = scopeRepo.RoomUser;

            try
            {
                await foreach (var chr in stream.WithCancellation(roomUser.Cancellation.Token))
                {
                    logger.LogInformation("received {Chr}", chr);

                    await ProcessDigit(chr);
                }
            }
            catch (OperationCanceledException e)
            {
                logger.LogInformation("user: {RoomUserId} has cancelled the stream", roomUser.Id);
            }

            return "done";
        }

        public async Task ProcessDigit(char chr)
        {
            var roomActor = scopeRepo.RoomActor;
            var room = roomActor.Room;

            roomActor.CharBuffer[roomActor.BufferPointer] = chr;
            roomActor.BufferPointer++;

            if (roomActor.BufferPointer > RoomActor.MAX_BUFFER - 1)
            {
                await Surrender();
                logger.LogInformation("user has surrendered because of exceeding the possible amount of inputs");
                return;
            }

            logger.LogInformation("received: {Chr}, expected: {Exp} == chr, current pointer: {Pointer}",
                chr, room.Text[roomActor.TextPointer], roomActor.TextPointer);

            if (room.Text[roomActor.TextPointer] == chr)
                roomActor.TextPointer++;

            if (roomActor.TextPointer == room.Text.Length)
                await finalizer.FinalizeUser();
        }

        public async IAsyncEnumerable<string[]> DownStreamCharBuffer
            ([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var roomUser = scopeRepo.RoomUser;
            var room = scopeRepo.Room;

            //send as long as the channel is opened
            //this token is set by the server
            while (!roomUser.Cancellation.IsCancellationRequested)
            {
                // Check the cancellation token regularly so that the server will stop
                // producing items if the client disconnects.
                if (cancellationToken.IsCancellationRequested) break;
                //this token is set by the client

                var updateBuffer = new string[room.Capacity];

                // attempt to send the real digits including the failed ones
                for (var u = 0; u < room.Capacity; u++)
                {
                    var otherActor = room.RoomActors[u];

                    if (roomUser.BufferSyncPointers[u] == otherActor.BufferPointer)
                    {
                        updateBuffer[u] = string.Empty;
                        continue;
                    }

                    updateBuffer[u] = new string
                        (otherActor.CharBuffer[roomUser.BufferSyncPointers[u]..otherActor.BufferPointer]);
                    //can return empty list

                    roomUser.BufferSyncPointers[u] = otherActor.BufferPointer;
                }

                if (!updateBuffer.All(string.IsNullOrEmpty))
                {
                    logger.LogInformation("received digits {SerializeObject}",
                        JsonConvert.SerializeObject(updateBuffer, Formatting.None));

                    yield return updateBuffer;
                }

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