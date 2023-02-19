using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildScriptPackedMine.asset",
    menuName = "Addressables/Content Builders/Default Build Script Mine")]
public class BuildScriptPackedModeMine : BuildScriptPackedMode
{
    protected override TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput,
        AddressableAssetsBuildContext aaContext)
    {
        return base.DoBuild<TResult>(builderInput, aaContext);
    }
    protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput builderInput)
    {
        var result = base.BuildDataImplementation<TResult>(builderInput);

        List<AddressableAssetEntry> entries = new();
        builderInput.AddressableSettings.GetAllAssets(entries, false);
        File.WriteAllText("Assets/tst.json", JsonConvert.SerializeObject(entries.Select(e => e.address),
            new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));

        return result;
    }
}