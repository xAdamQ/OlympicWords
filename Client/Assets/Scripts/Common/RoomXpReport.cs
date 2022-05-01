using UnityEngine.Scripting;

namespace Basra.Common
{
    // [Preserve]
    public class RoomXpReport
    {
        [Preserve]
        public RoomXpReport() { }

        public int Competition { get; set; }
        public int Basra { get; set; }
        public int BigBasra { get; set; }
        public int GreatEat { get; set; }
    }
}