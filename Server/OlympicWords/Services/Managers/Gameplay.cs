using System.Runtime.CompilerServices;
using OlympicWords.Common;
using Microsoft.AspNetCore.SignalR;
using OlympicWords.Services.Extensions;
// using Microsoft.VisualStudio.Threading;
using Newtonsoft.Json;
using OlympicWords.Services.Exceptions;

namespace OlympicWords.Services
{
    public interface IGameplay
    {
        Task<string> UpStreamChar(IAsyncEnumerable<char> stream);

        /// <summary>
        /// after ready, change domain, initial distribute, start 0 player turn
        /// </summary>
        Task ReadyGo();

        IAsyncEnumerable<ArraySegment<char>[]> DownStreamCharBuffer(CancellationToken clientCancellationToken);

        Task ProcessChar(char chr);
    }

    /// <summary>
    /// handle active/started room
    /// </summary>
    public class Gameplay : IGameplay
    {
        private readonly IHubContext<RoomHub> masterHub;
        private readonly ILogger<Gameplay> logger;
        private readonly IFinalizer finalizer;
        private readonly IServerLoop serverLoop;
        private readonly IScopeRepo scopeRepo;
        private readonly IOfflineRepo offlineRepo;

        public Gameplay(IHubContext<RoomHub> masterHub, ILogger<Gameplay> logger,
            IFinalizer finalizer, IServerLoop serverLoop, IScopeRepo scopeRepo, IOfflineRepo offlineRepo)
        {
            this.masterHub = masterHub;
            this.logger = logger;
            this.finalizer = finalizer;
            this.serverLoop = serverLoop;
            this.scopeRepo = scopeRepo;
            this.offlineRepo = offlineRepo;
        }

        public async Task ReadyGo()
        {
            var room = scopeRepo.Room;

            //this is not necessarily because the user is malicious, concurrency is a thing
            if (room.IsStarted)
            {
                logger.LogWarning("room already started");
                return;
            }

            room.SetUsersDomain<UserDomain.Room.ReadyGo>();
            logger.LogInformation("ready go..");

            foreach (var roomUser in room.RoomUsers)
                await masterHub.SendOrderedAsync(roomUser, "StartRoomRpc");

            await Task.Delay(TimeSpan.FromSeconds(1.5f));
            await StartGame();
        }

        private async Task StartGame()
        {
            var room = scopeRepo.Room;

            room.SetUsersDomain<UserDomain.Room.Active>();
            logger.LogInformation("all users are active");

            await room.Start(offlineRepo);

            serverLoop.StartGame(room);
        }


        //todo limit the sent chars count
        //todo check make sure they are only basic chars, no special ones, you can insert unsupported instead for every char 
        public async Task<string> UpStreamChar(IAsyncEnumerable<char> stream)
        {
            var roomUser = scopeRepo.RoomUser;

            try
            {
                await foreach (var chr in stream.WithCancellation(roomUser.Cancellation.Token))
                {
                    await ProcessChar(chr);
                }
            }
            catch (OperationCanceledException e)
            {
                logger.LogInformation("user: {RoomUserId} has cancelled the stream", roomUser.Id);
            }

            return "done";
        }

        public async Task ProcessChar(char chr)
        {
            var roomActor = scopeRepo.RoomActor;
            var room = roomActor.Room;

            if (roomActor.TextPointer == room.Text.Length) return;
            //in case the last input was string not char, and the finalization was already done

            roomActor.CharBuffer[roomActor.BufferPointer] = chr;
            roomActor.BufferPointer++;

            if (roomActor.BufferPointer > RoomActor.MAX_BUFFER - 1)
            {
                await finalizer.Surrender();
                logger.LogInformation(
                    "user was forced to surrender because of exceeding the possible amount of inputs");
                return;
            }

            //todo check from linux and make if the new line code the same
            if (chr == '\r')
            {
                roomActor.JetJump();
            }
            else
            {
                // if (roomActor is RoomUser)
                //     logger.LogInformation(
                //         "received: {Chr}, expected: {Exp} == chr, current pointer: {Pointer}, text size {TextSize}",
                //         chr, room.Text[roomActor.TextPointer], roomActor.TextPointer, room.Text.Length);

                if (room.Text[roomActor.TextPointer] == chr)
                {
                    if (room.Text[roomActor.TextPointer] == ' ')
                        roomActor.WordPointer++;

                    roomActor.TextPointer++;
                    //in the last char, this pointer is out of range
                }
            }

            if (roomActor.TextPointer == room.Text.Length)
                await finalizer.FinalizeUser();

            while (roomActor.FillersWords is { Count: > 0 } && roomActor.WordPointer == roomActor.FillersWords[0])
            {
                roomActor.JumpWords(1);
                roomActor.FillersWords.RemoveAt(0);
            }
        }

        public async IAsyncEnumerable<ArraySegment<char>[]> DownStreamCharBuffer(
            [EnumeratorCancellation] CancellationToken clientCancellationToken)
        {
            var roomUser = scopeRepo.RoomUser;
            var room = scopeRepo.Room;

            var updateBuffer = new ArraySegment<char>[room.Capacity];

            //send as long as the channel is opened
            //the first token is set by the server, the second token is set by the client
            while (!roomUser.Cancellation.IsCancellationRequested && !clientCancellationToken.IsCancellationRequested)
            {
                // logger.LogInformation("downstream for the player {rooUserId}", roomUser.Id);
                // Check the cancellation token regularly so that the server will stop
                // producing items if the client disconnects.

                // attempt to send the real digits including the failed ones
                for (var u = 0; u < room.Capacity; u++)
                {
                    var otherActor = room.RoomActors[u];

                    if (roomUser.BufferSyncPointers[u] == otherActor.BufferPointer)
                    {
                        updateBuffer[u] = ArraySegment<char>.Empty;
                        continue;
                    }

                    updateBuffer[u] = new ArraySegment<char>
                        (otherActor.CharBuffer[roomUser.BufferSyncPointers[u]..otherActor.BufferPointer]);

                    roomUser.BufferSyncPointers[u] = otherActor.BufferPointer;
                }

                if (updateBuffer.Any(x => x.Count > 0))
                    yield return updateBuffer;

                await Task.Delay(200);
            }

            //we don't reach here, probably some silent exception happen above when we close the connection
            logger.LogInformation
            ("downstream finished, is server cancelled: {serverCancelled}, is client cancelled {clientCancelled}",
                roomUser.Cancellation.IsCancellationRequested, clientCancellationToken.IsCancellationRequested);
        }
    }
}