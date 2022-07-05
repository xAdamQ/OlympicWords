using Basra.Common;
using BestHTTP;
using BestHTTP.SignalRCore;
using BestHTTP.SignalRCore.Encoders;
using BestHTTP.SignalRCore.Messages;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
#if !UNITY_WEBGL
using Newtonsoft.Json;
#endif

public interface IController : IGameObject
{
    Transform canvas { get; set; }
    void TstStartClient(string id);
    UniTask<FullUserInfo> GetPublicFullUserInfo(string userId);
    UniTask RequestRandomRoom(int betChoice, int capacityChoice);
}

[Rpc]
public class Controller : MonoModule<Controller>
{
    private void Update()
    {
        Keyboard.current.onTextInput += c => { Debug.Log(c); };
    }

    public Transform canvas { get; set; }

    protected override void Awake()
    {
        base.Awake();

#if UNITY_EDITOR
        Application.targetFrameRate = 165;
#endif

        DontDestroyOnLoad(this);

        new BlockingOperationManager();

        HTTPManager.Logger = new MyBestHttpLogger();
    }

    public void Start()
    {
        Toast.Create().Forget();

        NetManager.I.AddRpcContainer(this);
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