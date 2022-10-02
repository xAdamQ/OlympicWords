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
        base.Awake();

#if UNITY_EDITOR
        Application.targetFrameRate = targetFps;
#endif

        DontDestroyOnLoad(this);

        new BlockingOperationManager();

        HTTPManager.Logger = new MyBestHttpLogger();
    }

    public void Start()
    {
        Toast.Create().Forget();

        NetManager.I.AddRpcContainer(this);

        References.SetSources();
    }

    [Rpc]
    public void InitGame(PersonalFullUserInfo myFullUserInfo)
    {
        Debug.Log("InitGame is being called");

        new Repository(myFullUserInfo, null, null);

        Repository.I.PersonalFullInfo.DecreaseMoneyAimTimeLeft().Forget();

        SceneManager.LoadScene("Lobby");
    }

    public string InitGameName => nameof(InitGame);

    private Dictionary<string, object> TransitionData = new();

    public void AddTransitionData(string name, object obj)
    {
        TransitionData.Add(name, obj);
    }

    public object TakeTransitionData(string name)
    {
        var data = TransitionData[name];
        TransitionData.Remove(name);
        return data;
    }

    #region out rpcs

    // public void ToggleFollow(string targetId)
    // {
    //     SendAsync("ToggleFollow", targetId).Forget();
    // }
    //
    // public async UniTask<bool> IsFollowing(string targetId)
    // {
    //     return await InvokeAsync<bool>("IsFollowing", targetId);
    // }

    #endregion
}