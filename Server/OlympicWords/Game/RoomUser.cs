namespace OlympicWords.Services
{
    public class RoomUser : RoomActor
    {
        public bool IsReady { get; set; }

        public ActiveUser ActiveUser { get; set; }

        public int[] StreamSyncPointers { get; set; }

        //false when the game is finished or the user surrendered, kicked or disconnected on pending
        public bool InRoom { get; set; }


        public RoomUser(string id, Room room, int index, ActiveUser activeUser) : base(id, room, index)
        {
            ActiveUser = activeUser;

            StreamSyncPointers = new int[room.Capacity];
        }

        public CancellationTokenSource Cancellation { get; } = new();
    }
}