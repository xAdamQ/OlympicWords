using System;
using System.Collections.Generic;
using System.Linq;

namespace OlympicWords.Services
{
    // public class ActiveUser
    // {
    //     public ActiveUser(string id, string connectionId, Type initialDomain)
    //     {
    //         Id = id;
    //         ConnectionId = connectionId;
    //         Domain = initialDomain;
    //         Disconnected += () => ChallengeRequestTarget = null;
    //     }
    //
    //     public string Id { get; }
    //     public string ConnectionId { get; }
    //     public Type Domain { get; set; }
    //     public bool IsDisconnected { get; set; }
    //     public int MessageIndex { get; set; }
    //
    //     public string ChallengeRequestTarget;
    //
    //     public event Action Disconnected;
    //
    //     public void Disconnect()
    //     {
    //         Disconnected?.Invoke();
    //     }
    // }


    /// <summary>
    /// there is "Any" user domain because in that case, we won't use domains at all
    /// </summary>
    public abstract class UserDomain
    {
        public class Stateless : UserDomain
        {
            // public class Startup : App
            // {
            // }

            // public class Lobby : App
            // {
            //     public class Idle : Lobby
            //     {
            //     }
            //
            //     public class Pending : Lobby
            //     {
            //     }
            //

            // }

            /// <summary>
            /// pending is not neither stateless nor room, because we can't ask for another room while pending
            /// but at the same time we don't have an active socket connection
            /// </summary>
            public class Pending : UserDomain
            {
            }
        }


        public class Room : Stateless
        {
            public class GettingReady : Room
            {
            }

            public class WaitingForOthers : Room
            {
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