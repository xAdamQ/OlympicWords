using System.Collections.Generic;


namespace Basra.Common
{
    public class RoomStateData
    {
        public List<int> MyCards { get; set; }
        public List<int> GroundCards { get; set; }
        public int OppoCardsCount { get; set; }
        public int EatenCards { get; set; }
        public int BasraCount { get; set; }
        public int BigBasraCount { get; set; }
    }
}