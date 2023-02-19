using System.Collections.Generic;
namespace Shared
{
    public class Item
    {
        public int Price { get; set; }
        public string Id { get; set; }
        public int Level { get; set; }
        public ClientEnvironment Environment;
    }
    public class ItemPlayer : Item
    {
    }
    public class DiskEnvironment
    {
        public string Name { get; set; }
        public List<string> Children { get; set; }
        public string Parent { get; set; }
        public List<ItemPlayer> ItemPlayers { get; set; }
        public bool Playable { get; set; }
    }

    public class GameConfig
    {
        public string[] OrderedEnvs { get; set; }
    }
}