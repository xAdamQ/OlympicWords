using Basra.Common;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace Basra.Common
{
    // [Preserve]
    public class ThrowResult
    {
        [Preserve]
        public ThrowResult() { }

        public int ThrownCard { set; get; }
        public List<int> EatenCardsIds { set; get; }
        public bool Basra { set; get; }
        public bool BigBasra { set; get; }
    }
}