namespace OlympicWords.Services
{
    public class RoomBot : RoomActor
    {
        public RoomBot(string id, Room room) : base(id, room)
        {
            ChosenPowerUp = 2;
            // StaticRandom.GetRandom(3);
        }
    }
}