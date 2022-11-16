using System;
using System.Linq.Expressions;
using OlympicWords.Data;

namespace OlympicWords.Services.Data
{
    public class DisplayUser
    {
        public string Name { get; set; }
        public int PlayedRooms { get; set; }
        public int Wins { get; set; }

        public static Expression<Func<User, DisplayUser>> Projection => u => new DisplayUser()
        {
            Name = u.Name,
            PlayedRooms = u.PlayedRoomsCount,
            Wins = u.WonRoomsCount,
        };
    }
}