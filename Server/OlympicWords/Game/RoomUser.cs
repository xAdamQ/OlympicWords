namespace OlympicWords.Services
{
    public class RoomUser : RoomActor
    {
        public string ConnectionId { get; set; }

        public bool IsReady { get; set; }

        public ActiveUser ActiveUser { get; set; }

        public int[] StreamPointer { get; }
        //which is the last char the user received

        //false when the game is finished or the user surrendered, kicked or disconnected on pending
        public bool InRoom { get; set; }

        public RoomUser(string id, Room room, int index, string connectionId) : base(id, room, index)
        {
            ConnectionId = connectionId;
        }
    }
}