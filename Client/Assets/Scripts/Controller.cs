using System;
using BestHTTP;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if !UNITY_WEBGL
using Newtonsoft.Json;
#endif


[Rpc]
public class Controller : MonoModule<Controller>
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

    public ScopeReferences References;

    public Transform canvas;

    [SerializeField] private int targetFps;

    protected override void Awake()
    {
        if (I) Destroy(I.gameObject);
        DontDestroyOnLoad(this);

        base.Awake();

#if UNITY_EDITOR
        Application.targetFrameRate = targetFps;
#endif

        new BlockingOperationManager();

        HTTPManager.Logger = new MyBestHttpLogger();
    }

    public void Start()
    {
        Toast.Create().Forget();

        NetManager.I.AddRpcContainer(this);

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