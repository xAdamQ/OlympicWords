namespace OlympicWords.Services
{
    public class RoomUser : RoomActor
    {
        public string ConnectionId { get; set; }
        public Type Domain { get; set; }
        public int MessageIndex { get; set; }

        public event Action Disconnected;

        public void Disconnect()
        {
            Active = false;
            Disconnected?.Invoke();
        }

        public bool IsReady { get; set; }

        public int[] BufferSyncPointers { get; set; }

        //false when the game is finished or the user surrendered, kicked or disconnected on pending
        public bool Active { get; private set; }


        public RoomUser(string id, Room room) : base(id, room)
        {
            Domain = typeof(UserDomain.Room.Init);
            BufferSyncPointers = new int[room.Capacity];
        }

        public CancellationTokenSource Cancellation { get; } = new();
    }
}