using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class AddressableEditorUtility
{
    private static AddressableAssetSettings settings => AddressableAssetSettingsDefaultObject.Settings;
    public static AddressableAssetGroup GetOrCreateGroup(string groupName)
    {
        var group = settings.FindGroup(groupName);
        if (!group)
            group = settings.CreateGroup(groupName, false, false, true, null, typeof(ContentUpdateGroupSchema),
                typeof(BundledAssetGroupSchema));

        return group;
    }
    public static List<AddressableAssetEntry> AddressAssets(string groupName, params Object[] objs)
    {
        if (objs.Any(o => o == null))
            throw new ArgumentException("objs contains null");
        if (string.IsNullOrEmpty(groupName))
            throw new ArgumentException("group name can't be empty", nameof(groupName));

        var group = GetOrCreateGroup(groupName);

        var entries = new List<AddressableAssetEntry>();

        foreach (var o in objs)
        {
            var assetPath = AssetDatabase.GetAssetPath(o);
            var guid = AssetDatabase.AssetPathToGUID(assetPath);

            var entry = settings.CreateOrMoveEntry(guid, group, false, true);
            entries.Add(entry);
        }

        group.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries, true, true);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries, true, false);

        return entries;
    }

    public static void UpdateSystem()
    {
        if (!settings) throw new NullReferenceException("settings for addressables was not found");

        var envs = RootEnv.GetEnvironments();
        var envQueue = new Queue<ClientEnvironment>();
        envQueue.Enqueue(envs.Single(e => e.Type == typeof(RootEnv)));
        var visited = new List<ClientEnvironment>();

        var envAddr = new Dictionary<string, EnvironmentAddresses>();
        var currentDirectory = Directory.GetCurrentDirectory();

        while (envQueue.Count > 0)
        {
            var env = envQueue.Dequeue();
            visited.Add(env);

            foreach (var child in env.Children.Where(e => !visited.Contains(e)))
                envQueue.Enqueue(child);

            var envPath = Path.Combine(
                Path.Combine(currentDirectory, AddressManager.PREFAB_ADDRESS),
                Path.Combine(env.GetParentsNames()));
            //root/graph/jump for example

            if (!Directory.Exists(envPath)) continue;

            GetOrCreateGroup(env.Type.Name);

            var topGos = getObjectsAt(envPath, SearchOption.TopDirectoryOnly);
            var shop = topGos.SingleOrDefault(o => o.go.GetComponent<Shop>());
            if (shop != default((string, GameObject)))
                AddressAssets(env.Type.Name, shop.go);

            // var scene = EditorSceneManager.GetSceneByName(env.Type.Namespace);
            // if (scene != default)
            // {
            // AddressAssets(env.Type.Name, scene);
            // }

            //I leave it to null because it's the rule of the address manager to re-route
            // else
            // shop.path = envAddr[env.Parent.Name].Shop;

            var itemPath = Path.Combine(envPath, "Item/Player");
            List<(string path, GameObject go)> itemPlayers = new();
            if (Directory.Exists(itemPath))
            {
                var itemPlayersAll = getObjectsAt(itemPath, SearchOption.AllDirectories);
                itemPlayers = itemPlayersAll.Where(o => o.go.GetComponent<ItemPlayer>()
                                                        && !o.go.name.ToLower().Contains("base")).ToList();
                AddressAssets(env.Type.Name, itemPlayers.Select(o => (Object)o.go).ToArray());
            }

            var playerPath = Path.Combine(envPath, "Player");
            List<(string path, GameObject go)> players = new();
            if (Directory.Exists(playerPath))
            {
                var playersAll = getObjectsAt(playerPath, SearchOption.AllDirectories);
                players = playersAll.Where(o => o.go.GetComponent<Player>() &&
                                                !o.go.name.ToLower().Contains("base")).ToList();
                AddressAssets(env.Type.Name, players.Select(o => (Object)o.go).ToArray());
            }

            envAddr.Add(env.Type.Name, new EnvironmentAddresses
            {
                Shop = shop.path?.Replace('\\', '/'),
                Players = players.Select(o => o.path.Replace('\\', '/')).ToList(),
                Items = new()
                {
                    Players = itemPlayers.Select(o => o.path.Replace('\\', '/')).ToList(),
                },
            });
        }

        AddressManager.SaveEnvAddr(envAddr);

        List<(string path, GameObject go)> getObjectsAt(string path, SearchOption searchOption)
        {
            var topFiles = Directory.GetFiles(path, "*.prefab", searchOption);
            return topFiles.Select(f =>
            {
                var assetPath = f.Replace(currentDirectory, "")[1..];
                return (assetPath, go: AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
            }).ToList();
        }
    }
}