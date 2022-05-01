using UnityEngine.Scripting;

namespace Basra.Common
{
    // [Preserve]
    public class UserRoomStatus
    {
        [Preserve]
        public UserRoomStatus() { }

        public int EatenCards { get; set; }
        public int Basras { get; set; }
        public int BigBasras { get; set; }
        public int WinMoney { get; set; }
    }
}