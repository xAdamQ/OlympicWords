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

        Task ProcessChar(char chr);
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

            room.SetUsersDomain<UserDomain.App.Room.ReadyGo>();
            logger.LogInformation("all users are 321");

            foreach (var roomUser in room.RoomUsers)
                await masterHub.SendOrderedAsync(roomUser.ActiveUser, "StartRoomRpc");

#pragma warning disable CS4014
            Task.Delay(TimeSpan.FromSeconds(1.5f))
                .ContinueWith(_ => serverLoop.StartGame(room));
#pragma warning restore CS4014

            logger.LogInformation("awaited successfully");
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
                    // logger.LogInformation("received {Chr}", chr);

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
                if (roomActor is RoomUser)
                    logger.LogInformation(
                        "received: {Chr}, expected: {Exp} == chr, current pointer: {Pointer}, text size {TextSize}",
                        chr, room.Text[roomActor.TextPointer], roomActor.TextPointer, room.Text.Length);

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

        public async IAsyncEnumerable<string[]> DownStreamCharBuffer
            ([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var roomUser = scopeRepo.RoomUser;
            var room = scopeRepo.Room;

            //send as long as the channel is opened
            //this token is set by the server, second token is set by the client
            while (!roomUser.Cancellation.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Check the cancellation token regularly so that the server will stop
                // producing items if the client disconnects.

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
                    (otherActor.CharBuffer[
                        roomUser.BufferSyncPointers[u]..otherActor.BufferPointer]);
                    //can return empty list

                    roomUser.BufferSyncPointers[u] = otherActor.BufferPointer;
                }

                if (!updateBuffer.All(string.IsNullOrEmpty))
                {
                    // logger.LogInformation("received digits {SerializeObject}",
                    //     JsonConvert.SerializeObject(updateBuffer, Formatting.None));

                    yield return updateBuffer;
                }

                // Use the cancellationToken in other APIs that accept cancellation
                // tokens so the cancellation can flow down to them.
                await Task.Delay(100, cancellationToken);
            }
        }
    }
}