namespace OlympicWords.Services
{
    public class RoomBot : RoomActor
    {
        public const int IdRange = 3; //increase this, make it store changed data like real user

        public RoomBot(string id, Room room) : base(id, room)
        {
            ChosenPowerUp = StaticRandom.GetRandom(2); //todo make it three
        }
    }
}