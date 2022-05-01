using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Basra.Common
{
    // [Preserve]
    public class FinalizeResult
    {
        [Preserve]
        public FinalizeResult() { }

        public RoomXpReport RoomXpReport { set; get; }
        public PersonalFullUserInfo PersonalFullUserInfo { set; get; }
        public int LastEaterTurnId { get; set; }
        public List<UserRoomStatus> UserRoomStatus { set; get; }
    }
}