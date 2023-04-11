namespace Shared;

public class Item
{
    public int Price { get; set; }
    public string Id { get; set; }
    public int Level { get; set; }
    public Environment Environment;
}

public class ItemPlayer : Item
{
}

public class Environment
{
    public string Name { get; set; }
    public List<Environment> Children { get; set; }
    public Environment Parent { get; set; }
    public List<ItemPlayer> ItemPlayers { get; set; }
    public bool Playable { get; set; }

    /// <summary>
    /// contains this environment and all its children at all levels
    /// </summary>
    public Dictionary<string, Environment> Matching { get; set; }
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
    public EnvConfig[] EnvConfigs { get; set; }
}

public class EnvConfig
{
    public string Name { get; set; }
    public string DefaultPlayer { get; set; }
}