using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace OlympicWords.Services
{
    public abstract class RoomActor
    {
        ///<summary>
        /// despite this exists in ActiveUser but this can exist without active user so no nav prop for active user
        /// exist to access its members
        /// </summary>
        public string Id { get; }

        public Room Room { get; }

        /// <summary>
        /// id in room, turn id
        /// </summary>
        public int TurnId;

        public int Index { get; }

        public int TextPointer { get; set; }
        public int BufferPointer { get; set; }
        public char[] CharBuffer { get; }

        public const int MAX_BUFFER = 10000;
        //max chars player can input in a game, if passed player will lose

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }
        //wpm is calculated by these and room words

        //todo index is subject to removal because it is not known at creating time,
        //so it may be linked to startGame function here to set these props or the creation process is changed
        public RoomActor(string id, Room room, int index)
        {
            Room = room;
            Index = index;
            Id = id;

            CharBuffer = new char[MAX_BUFFER];
        }
    }
}