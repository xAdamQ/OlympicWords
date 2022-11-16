using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace OlympicWords.Services
{
    public class RoomUser : RoomActor
    {
        public bool IsReady { get; set; }

        public ActiveUser ActiveUser { get; set; }

        public int[] BufferSyncPointers { get; set; }

        //false when the game is finished or the user surrendered, kicked or disconnected on pending
        public bool InRoom { get; set; }


        public RoomUser(string id, Room room, ActiveUser activeUser) : base(id, room)
        {
            ActiveUser = activeUser;

            BufferSyncPointers = new int[room.Capacity];
        }

        public CancellationTokenSource Cancellation { get; } = new();


    }
}