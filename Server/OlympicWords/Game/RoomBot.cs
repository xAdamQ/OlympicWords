namespace OlympicWords.Services
{
    public class RoomBot : RoomActor
    {
        public RoomBot(string id, Room room) : base(id, room)
        {
            ChosenPowerUp = 0;
            // StaticRandom.GetRandom(3);
        }
    }
}