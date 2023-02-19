using OlympicWords.Services.Extensions;
namespace OlympicWords.Services
{
    public class RoomBot : RoomActor
    {
        private const float MIN_SPEED_TOLERANCE = .1f, MAX_SPEED_TOLERANCE = .5f;

        public int BotTimeMin { get; private set; }
        public int BotTimeMax { get; private set; }

        public RoomBot(string id, Room room) : base(id, room)
        {
            ChosenPowerUp = 0;
        }

        public void SetSpeed(float roomWpm)
        {
            var r = new Random();

            var downTolerance = r.NextFloat(MIN_SPEED_TOLERANCE, MAX_SPEED_TOLERANCE);
            var upTolerance = r.NextFloat(MIN_SPEED_TOLERANCE, MAX_SPEED_TOLERANCE);

            var minWpm = roomWpm - roomWpm * downTolerance;
            var maxWpm = roomWpm + roomWpm * upTolerance;

            var WpmToCps = 5f / 60f;

            var minCps = minWpm * WpmToCps;
            var maxCps = maxWpm * WpmToCps;

            var botTimeMinSeconds = 1f / maxCps;
            var botTimeMaxSeconds = 1f / minCps;
            //look how to switched min/max here, because of time/speed switching

            BotTimeMin = (int)MathF.Round(botTimeMinSeconds * 1000);
            BotTimeMax = (int)MathF.Round(botTimeMaxSeconds * 1000);
        }
    }
}