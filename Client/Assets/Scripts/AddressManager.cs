using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

public class AddressManager
{
    public static AddressManager I => i ??= new AddressManager();
    private static AddressManager i;

    /// <summary>
    /// env type is the key
    /// </summary>
    /// <returns></returns>
    private readonly Dictionary<string, EnvironmentLocations> environmentLocationsLookup = new();
    private Dictionary<string, IResourceLocation> PlayerLocations;

    public IResourceLocation GetShop(string envName)
    {
        var currentEnv = RootEnv.Environments[envName];
        EnvironmentLocations environmentLocations;
        while (!environmentLocationsLookup.TryGetValue(currentEnv.Name, out environmentLocations) ||
               environmentLocations.Shop == null)
            currentEnv = currentEnv.Parent;

        return environmentLocations.Shop;
    }

    public IEnumerable<IResourceLocation> GetItemPlayerLocationsRecursive(string envName)
    {
        var currentEnv = RootEnv.Environments[envName];

        while (currentEnv != null)
        {
            if (!environmentLocationsLookup.TryGetValue(currentEnv.Name, out var environmentLocations))
            {
                currentEnv = currentEnv.Parent;
                continue;
            }

            foreach (var itemPlayer in environmentLocations.Items.Players)
                yield return itemPlayer;

            currentEnv = currentEnv.Parent;
        }
    }

    public List<IResourceLocation> GetItemPlayerLocations(string envName)
    {
        return environmentLocationsLookup.TryGetValue(RootEnv.Environments[envName].Name, out var environmentLocations)
            ? environmentLocations.Items.Players
            : new List<IResourceLocation>();
    }


    public IResourceLocation GetPlayerLocation(string id)
    {
        return PlayerLocations[id];
    }

    public const string PREFAB_ADDRESS = "Assets/Prefabs";

    public bool Initialized;

    public AddressManager()
    {
        MakeEnvironmentLocations().Forget();
    }

    public static void Reset()
    {
        i = null;
    }

    public async UniTask WaitInit()
    {
        await UniTask.WaitUntil(() => Initialized);
    }

    private Dictionary<string, IResourceLocation> GetAllLocations()
    {
        var locators = Addressables.ResourceLocators.ToList();
        var allLocations = new Dictionary<string, IResourceLocation>();

        foreach (var locator in locators)
        foreach (var locatorKey in locator.Keys)
            if (locator.Locate(locatorKey, typeof(GameObject), out var locations))
                foreach (var location in locations)
                    allLocations.TryAdd(location.InternalId, location);

        return allLocations;
    }

    private async UniTaskVoid MakeEnvironmentLocations()
    {
        await Addressables.InitializeAsync();

        var addresses = JsonConvert.DeserializeObject<Dictionary<string, EnvironmentAddresses>>
            (await File.ReadAllTextAsync("Assets/EnvironmentAddresses.json"));

        var locations = GetAllLocations();

        var envs = RootEnv.GetEnvironments();
        var envQueue = new Queue<ClientEnvironment>();
        envQueue.Enqueue(envs.Single(e => e.Name == RootEnv.Name));
        var visited = new List<ClientEnvironment>();

        while (envQueue.Count > 0)
        {
            var env = envQueue.Dequeue();
            visited.Add(env);

            foreach (var child in env.Children.Where(e => !visited.Contains(e)))
                envQueue.Enqueue(child);

            if (!addresses.TryGetValue(env.Name, out var envAddresses)) continue;

            //there is not automatic system that fill the types automatically because this is a dump automation
            environmentLocationsLookup.Add(env.Name, new EnvironmentLocations
            {
                Shop = envAddresses.Shop == null ? null : locations[envAddresses.Shop],
                Players = locations.Where(l => envAddresses.Players.Contains(l.Key)).Select(kvp => kvp.Value).ToList(),
                Items = new()
                {
                    Players = locations.Where(l => envAddresses.Items.Players.Contains(l.Key)).Select(kvp => kvp.Value)
                        .ToList(),
                },
            });
        }

        PlayerLocations = environmentLocationsLookup.SelectMany(e => e.Value.Players)
            .ToDictionary(l => Path.GetFileNameWithoutExtension(l.InternalId), l => l);

        Initialized = true;
    }

    public static void SaveEnvAddr(Dictionary<string, EnvironmentAddresses> environmentAddresses)
    {
        File.WriteAllText("Assets/EnvironmentAddresses.json", JsonConvert.SerializeObject(environmentAddresses));
    }

    public class EnvironmentLocations
    {
        /// <summary>
        /// if the environment doesn't have a shop, the parent env shop is used instead
        /// </summary>
        public IResourceLocation Shop;
        public ItemSet Items;
        public List<IResourceLocation> Players;

        public class ItemSet
        {
            public List<IResourceLocation> Players;

            // public List<IResourceLocation> Holdables; //just to show the idea
        }
    }
}

public class EnvironmentAddresses
{
    /// <summary>
    /// if the environment doesn't have a shop, the parent env shop is used instead
    /// </summary>
    public string Shop;
    public ItemSet Items;
    public List<string> Players;

    public class ItemSet
    {
        public List<string> Players;

        // public List<IResourceLocation> Holdables; //just to show the idea
    }
}