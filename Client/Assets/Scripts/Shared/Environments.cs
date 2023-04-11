using System.Collections.Generic;

namespace Shared
{
    public class DiskItem
    {
        public int Price { get; set; }
        public string Id { get; set; }
        public int Level { get; set; }
    }

    public class DiskItemPlayer : DiskItem
    {
    }

    public class DiskEnvironment
    {
        public string Name { get; set; }
        public List<string> Children { get; set; }
        public string Parent { get; set; }
        public List<DiskItemPlayer> ItemPlayers { get; set; }
        public bool Playable { get; set; }
    }

    public class GameConfig
    {
        public EnvConfig[] EnvConfigs { get; set; }
    }
}