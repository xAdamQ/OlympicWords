using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Shared;
using UnityEngine.UIElements;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Server
{
    public class HelperEditorUI : EditorWindow
    {
        private TextField pathField;

        [MenuItem("MyTools/HelperEditorUI")]
        static void OpenWindow()
        {
            var window = GetWindow<HelperEditorUI>();
            window.Show();
        }

        private void CreateGUI()
        {
            if (!Directory.Exists("Assets/Editor/Data"))
                Directory.CreateDirectory("Assets/Editor/Data");

            var exportPath = "Assets/Editor/Data/exportPath.txt";

            if (!File.Exists(exportPath))
                File.WriteAllText(exportPath, "");

            pathField = new TextField("path")
            {
                value = File.ReadAllText(exportPath)
            };
            pathField.RegisterValueChangedCallback(e => File.WriteAllText(exportPath, e.newValue));

            var exportButton = new Button(ExportServerData)
            {
                text = "export shared config",
            };

            var deletePrefsButton = new Button(PlayerPrefs.DeleteAll)
            {
                text = "delete prefs"
            };

            rootVisualElement.Add(new Button(TestLocators)
            {
                text = "test locators"
            });

            rootVisualElement.Add(new Button(() => { Addressables.CleanBundleCache(new[] { "GraphJump" }); })
            {
                text = "clear bundles cache"
            });

            rootVisualElement.Add(new Button(() =>
            {
                UniTask.Create(async () =>
                {
                    await Addressables.InitializeAsync();

                    var rm = Addressables.ResourceManager;

                    var settings = AddressableAssetSettingsDefaultObject.Settings;
                    List<AddressableAssetEntry> entries = new();
                    settings.GetAllAssets(entries, false);

                    var locators = Addressables.ResourceLocators.ToList();
                    var allLocations = new List<IResourceLocation>();
                    var logMessage = string.Empty;
                    foreach (var resourceLocator in locators)
                    {
                        foreach (var resourceLocatorKey in resourceLocator.Keys)
                        {
                            if (resourceLocator.Locate(resourceLocatorKey, typeof(GameObject), out var locations))
                            {
                                allLocations.AddRange(locations);
                                logMessage += resourceLocatorKey.ToString() + '\n';
                            }
                        }
                    }
                    Debug.Log("successful keys are: " + logMessage);
                    Debug.Log("all locations count: " + allLocations.Count);
                    await Addressables.InstantiateAsync(allLocations[int.Parse(pathField.value)]);
                });
            })
            {
                text = "remote addressables test"
            });

            rootVisualElement.Add(new Button(AddressableEditorUtility.UpdateSystem)
            {
                text = "update system"
            });

            rootVisualElement.Add(pathField);
            rootVisualElement.Add(exportButton);
            rootVisualElement.Add(deletePrefsButton);
        }

        public void ExportServerData()
        {
            ExportServerDataAsync().Forget(e => throw e);
        }

        private async UniTask ExportServerDataAsync()
        {
            AddressManager.Reset();
            await AddressManager.I.WaitInit();

            var envs = RootEnv.GetEnvironments();
            var diskEnvs = envs.Select(e => new DiskEnvironment
            {
                Name = e.Name,
                Parent = e.Parent?.Name,
                Children = e.Children.Select(c => c.Name).ToList(),
                Playable = !RootEnv.EnvTypes[e.Name].IsAbstract,
            }).ToList();

            foreach (var diskEnvironment in diskEnvs)
            {
                var locations = AddressManager.I.GetItemPlayerLocations(diskEnvironment.Name);
                diskEnvironment.ItemPlayers = new();
                foreach (var resourceLocation in locations)
                {
                    var go = await Addressables.LoadAssetAsync<GameObject>(resourceLocation);
                    var item = go.GetComponent<ItemPlayer>();

                    diskEnvironment.ItemPlayers.Add(new Shared.ItemPlayer
                    {
                        Id = item.Id,
                        Level = item.Level,
                        Price = item.Price,
                    });
                }
            }

            var envString = JsonConvert.SerializeObject(diskEnvs, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            await File.WriteAllTextAsync(Path.Combine(pathField.value, "Environments.json"), envString);

            var config = new GameConfig
            {
                OrderedEnvs = RootEnv.OrderedEnvs.Select(e => RootEnv.EnvIds[e]).ToArray(),
            };
            await File.WriteAllTextAsync(Path.Combine(pathField.value, "GameConfig.json"),
                JsonConvert.SerializeObject(config));
        }

        private void TestLocators()
        {
            EditorCoroutineUtility.StartCoroutineOwnerless(routine());
            IEnumerator routine()
            {
                var l = Addressables.LoadResourceLocationsAsync("GraphJump");
                yield return l;
            }
            // var aRef = aRefField.;
            // var lcoator = Addressables.AddResourceLocator()
            // var location = Addressables.LoadAssetAsync()
            // var locactions = Addressables.LoadResourceLocationsAsync()
        }
    }
}