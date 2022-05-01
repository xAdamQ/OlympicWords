using System.Collections.Generic;

namespace OlympicWords.Services
{
    public abstract class RoomActor
    {
        public const int HandSize = 4;

        /// <summary>
        /// despite this exists in ActiveUser but this can exist without active user so no nav prop for active user
        /// exist to access its members
        /// </summary>
        public string Id { get; }

        public int BasraCount { get; set; }
        public int BigBasraCount { get; set; }
        public int EatenCardsCount { get; set; }

        public Room Room { get; }

        public List<int> Hand { get; set; }

        /// <summary>
        /// id in room, turn id
        /// </summary>
        public int TurnId;

        public int Index { get; }


        //todo index is subject to removal because it is not known at creating time,
        //so it may be linked to startGame function here to set these props or the creation process is changed
        public RoomActor(string id, Room room, int index)
        {
            Room = room;
            Index = index;
            Id = id;
        }
    }
}