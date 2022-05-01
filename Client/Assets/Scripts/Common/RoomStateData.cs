using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Basra.Common
{
    // [Preserve]
    public class RoomStateData
    {
        [Preserve]
        public RoomStateData() { }

        public List<int> MyCards { get; set; }
        public List<int> GroundCards { get; set; }
        public int OppoCardsCount { get; set; }
        public int EatenCards { get; set; }
        public int BasraCount { get; set; }
        public int BigBasraCount { get; set; }
    }
}