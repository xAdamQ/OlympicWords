using UnityEngine.Scripting;

namespace Basra.Common
{
    // [Preserve]
    public class UserRoomStatus
    {
        [Preserve]
        public UserRoomStatus()
        {
        }

        public float Wpm { get; set; }
        public int Score { get; set; }
        public int EarnedMoney { get; set; }
        public int FinalPosition { get; set; }
    }
}