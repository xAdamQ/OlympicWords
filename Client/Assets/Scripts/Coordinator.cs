using System;
using System.Collections;
using BestHTTP;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Coordinator : MonoModule<Coordinator>
{
    [Serializable]
    public class ScopeReferences
    {
        public MonoModule<LevelUpPanel> LevelUpView;
        public GameObject
            BlockingPanelPrefab,
            PopupPrefab,
            FuvPrefab,
            PersonalFuvPrefab,
            MuvPrefab,
            ToastPrefab;

        public void SetSources()
        {
            LevelUpPanel.SetSource(LevelUpView.gameObject, I.canvas);
        }
    }


    [ContextMenu("TestLocators")]
    private async UniTask TestLocators()
    {
        // var l = await Addressables.LoadResourceLocationsAsync("GraphJump");
        // foreach (var resourceLocation in l)
        // {
        //     Debug.Log(JsonConvert.SerializeObject(resourceLocation, Formatting.Indented));
        // }

        // Debug.Log(JsonUtility.ToJson(Settings));

        // Debug.Log(JsonConvert.SerializeObject(Settings,
        // new JsonSerializerSettings
        // {
        // ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        // }));


        // Settings.OnDataBuilderComplete += (settings, builder, arg3) => { Debug.Log("builder complete"); };

        // List<AddressableAssetEntry> entries = new();
        // Settings.GetAllAssets(entries, true);
    }

    public ScopeReferences References;

    public Transform canvas;

    [SerializeField] private int targetFps;

    protected override void Awake()
    {

        if (I) Destroy(I.gameObject);
        DontDestroyOnLoad(this);

        base.Awake();

// #if UNITY_EDITOR
//         Application.targetFrameRate = targetFps;
// #endif

        HTTPManager.Logger = new MyBestHttpLogger();

        TestLocators().Forget(e => throw e);
    }


    public void Start()
    {
        Toast.Create().Forget();
        AddressManager.Init().Forget(e => throw e);

        References.SetSources();
    }

    private readonly Dictionary<string, object> transitionData = new();
    public void AddTransitionData(string name, object obj)
    {
        transitionData.Add(name, obj);
    }
    public object TakeTransitionData(string name)
    {
        var data = transitionData[name];
        transitionData.Remove(name);
        return data;
    }
}