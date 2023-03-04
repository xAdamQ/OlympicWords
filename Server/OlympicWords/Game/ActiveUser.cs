namespace OlympicWords.Services
{
    /// <summary>
    /// there is no "Any" user domain because in that case, we won't use domains at all
    /// </summary>
    public abstract class UserDomain
    {
        public class Stateless : UserDomain
        {
            public class Free : Stateless
            {
            }
            /// <summary>
            /// pending is not neither stateless nor room, because we can't ask for another room while pending
            /// but at the same time we don't have an active socket connection
            /// </summary>
            public class Pending : Stateless
            {
            }
        }

        public class Room : UserDomain
        {
            public class Init : Room
            {
                /// <summary>
                /// this means the room is not full yet, this is the default state for the newly created room user
                /// </summary>
                public class InComplete : Init
                {
                }

                /// <summary>
                /// this means the room is full and the player is not ready yet
                /// </summary>
                public class GettingReady : Init
                {
                }

                /// <summary>
                /// this means he is ready and waiting for others to be ready
                /// </summary>
                public class Ready : Init
                {
                }
            }

            public class Finished : Room
            {
            }

            internal class Active : Room
            {
            }

            public class ReadyGo : Room
            {
            }
        }
    }
}