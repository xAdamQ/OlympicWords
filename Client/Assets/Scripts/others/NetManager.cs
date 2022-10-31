﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using BestHTTP;
using BestHTTP.SignalRCore;
using UnityEngine;
using BestHTTP.SignalRCore.Encoders;
using BestHTTP.SignalRCore.Messages;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

/*
 "authResponse": {
    "accessToken": "EAAGqLasqeUoBAKyHSnT82cRMZCGZBdlu0uV2lt3UtHaX1VBlAivuZAUOHtontRzY6HGpLjyGdLHPLBPaBdlKl4cqu7ZC1r5ZCxL5Bh7xJhyVXbIrRczVq0ZAnI7lYiVyGact9GLKjaJLuq6V9b8DZCUoEGrmYFmQqrsMGljD6nGqp6A7I4nZBZAw697ZBuCZAm2hak8vdmDhbnKU1JamZB7prxWm",
    "userID": "1954944418032338",
    "expiresIn": 4625,
    "signedRequest": "mr7aC6G9v0NXkkQKEHRxy65Uz6d5K9k5p2AhOQy1cR4.eyJ1c2VyX2lkIjoiMTk1NDk0NDQxODAzMjMzOCIsImNvZGUiOiJBUUQ0ZC1LMUlIaTVZeE5EcDRDUGcteGxGZDdQbjV5VnVWcHJIckJqNktiY0d0NGVpaTMyUkU3WGVKclFsSmFZYXQ5SGFONWMyNVgtcjMtYjBoV1J6bHN0em1oVTFTNl92aWtzT1dxSndJX2FrRFFvbG9La1JrNkpYSFBUcjl0bFV6UUZ6R2xfSVRnUHhiek5xS21WLU9CaXpybEJkQ2ZoM1k1MGtOUGNiaXV1M3phU3BxTkJVdjF0VkxtUE1KOUpubTR1alliRV81RUtybzJKMEpDdkhMaXgzWkx6VTlfNzFCWWNFZWVRcm1adDhGcnRQNGd0SDAwczVCU0lyeGpvSUZkV3RvT2l4WWNvMUl6VmdLckpMblkwaGlpZDhiY1U3MTd3dTM3S3cybGRMVDdTMUJDbl9XckFWSWZtb3FKN0xTQzdZOG1xY1NGbWdNVjVpano5Sm1aWiIsImFsZ29yaXRobSI6IkhNQUMtU0hBMjU2IiwiaXNzdWVkX2F0IjoxNjY3MDY4OTc1fQ",
    "graphDomain": "facebook",
    "data_access_expiration_time": 1674844975
  },
  "status": "connected"
 
 * 
 */

public class FbManager
{
    private class FbLoginStatus
    {
        public FbAuthResponse AuthResponse { get; set; }
        public string Status { get; set; }

        [UsedImplicitly]
        public class FbAuthResponse
        {
            public string AccessToken { get; set; }
            public string ExpiresIn { get; set; }
            public string SignedRequest { get; set; }
            public string UserID { get; set; }
        }
    }

    [UsedImplicitly]
    private class FbValidationResponse
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public ValidationData data { get; set; }

        [UsedImplicitly]
        public class ValidationData
        {
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public bool is_valid { get; set; }
        }
    }

    private async UniTask<bool> ValidateFbAccToken(string token)
    {
        const string clientToken = "468588098648394|CwbC4U-0WDoPAaeP79TTG7ELfD4";
        const string fbBaseAddress = "https://graph.facebook.com/v15.0/";

        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams.Add("input_token", token);
        queryParams.Add("access_token", clientToken);

        const string address = fbBaseAddress + "debug_token";

        var uri = new UriBuilder(address) { Query = queryParams.ToString()! }.ToString();

        try
        {
            var response = await NetManager.I.GetAsync<FbValidationResponse>(uri);
            return response.data.is_valid;
        }
        catch (Exception e)
        {
            Console.WriteLine("couldn't validate the cached token due to the error: " + e);
            return false;
        }
    }

    public async UniTask FbLoginWorks()
    {
        var cachedToken = PlayerPrefs.GetString("fbToken");

        if (string.IsNullOrEmpty(cachedToken) || !await ValidateFbAccToken(cachedToken))
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            JsManager.ShowFbButton();
#endif
            return;
        }

        NetManager.I.ConnectToServer(cachedToken, "facebook");
    }


    public void FbLogin(string responseStr)
    {
        Debug.Log("fb login in unity called with data: " + responseStr);

        var response = JsonConvert.DeserializeObject<FbLoginStatus>(responseStr);
        if (response == null) throw new NullReferenceException("rb response is null");

        PlayerPrefs.SetString("fbToken", response.AuthResponse.AccessToken);

        NetManager.I.ConnectToServer(response.AuthResponse.AccessToken, "facebook");
    }
}

public class NetManager : MonoModule<NetManager>
{
    private readonly IProtocol protocol = new JsonProtocol(new LitJsonEncoder());
    private readonly MyReconnectPolicy myReconnectPolicy = new();
    private HubConnection hubConnection;
    private UpStreamItemController<string> upStreamItemController;
    private const int MAX_DEBUG_LENGTH = 200;
    public bool Connected;
    public readonly FbManager FbManager = new();

    private readonly JsonSerializerSettings serializationSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    [ContextMenu("tst")]
    public void tst()
    {
        var res = PlayerPrefs.GetString("sfhjjksdfsd");
        Debug.Log(string.IsNullOrEmpty(res));
    }

    protected override void Awake()
    {
        base.Awake();

        DontDestroyOnLoad(this);

        FetchRpcInfos();

        serverAddressChoice.ChoiceChanged += _ => chosenAddressText.text = GetServerAddress();
    }

    // ReSharper disable once Unity.IncorrectMethodSignature
    private async UniTaskVoid Start()
    {
        try
        {
            await FbManager.FbLoginWorks();
        }
        catch (Exception e)
        {
            Debug.LogError("failed to auto login fb due to: " + e);
#if UNITY_WEBGL && !UNITY_EDITOR
            JsManager.ShowFbButton();
#endif
        }
    }

    [UsedImplicitly]
    public void FbLogin(string responseStr)
    {
        FbManager.FbLogin(responseStr);
    }

    private void DownStream()
    {
        var controller = hubConnection.GetDownStreamController<string[]>("DownStreamCharBuffer");

        controller.OnItem(DigitsReceived)
            .OnSuccess(_ => Debug.Log("Streaming finished!"))
            .OnError(error => Debug.Log("Error: " + error));

        // A stream request can be cancelled any time by calling the controller's Cancel method
        // controller.Cancel();
    }

    public void DownStreamTest()
    {
        var controller = hubConnection.GetDownStreamController<int>("DownStreamTest");

        controller.OnItem(Callback)
            .OnSuccess(_ => Debug.Log("Streaming finished!"))
            .OnError(error => Debug.Log("Error: " + error));
    }

    void Callback(int i)
    {
        Debug.Log(i);
    }

    private void DigitsReceived(string[] playerBuffers)
    {
        try
        {
            Debug.Log($"received: {JsonConvert.SerializeObject(playerBuffers)}");
            for (var p = 0; p < playerBuffers.Length; p++)
                if (p != RoomBase.I.MyTurn)
                    foreach (var digit in playerBuffers[p])
                        EnvBase.I.Players[p].TakeInput(digit);
        }
        catch (Exception e)
        {
            Debug.LogError("exception caught in my downstream code"
                           + "\n--------------\n buffer lengths: " +
                           playerBuffers.Length
                           + "\n--------------\n players count: " +
                           EnvBase.I.Players.Count
                           + "\n--------------\n exce message: " +
                           e.Message
                           + "\n--------------\n" +
                           e.InnerException
                           + "\n--------------\n" +
                           JsonUtility.ToJson(e));
        }
    }

    private void UpStream()
    {
        upStreamItemController = hubConnection.GetUpStreamController<string, char>("UpStreamChar");
        upStreamItemController.UploadParam((char)Random.Range(0, 200));
        upStreamItemController.UploadParam((char)Random.Range(0, 200));
        upStreamItemController.UploadParam((char)Random.Range(0, 200));
        // upStreamItemController.OnSuccess(result => { Debug.Log($"Upload finished: {result}"); });
    }

    public void StartStreaming()
    {
        UpStream();
        DownStream();
    }

    public void StreamChar(char chr)
    {
        upStreamItemController.UploadParam(chr);
    }

    public async Task<object> SendAsync(string method, params object[] args)
    {
        return await hubConnection.SendAsync(method, args);
    }

    public void Send(string method, params object[] args)
    {
        hubConnection.Send(method, args);
    }

    public Task<T> InvokeAsync<T>(string method, params object[] args)
    {
        return hubConnection.InvokeAsync<T>(method, args);
    }

    [SerializeField] private int selectedAddress;
    [SerializeField] private string[] addresses;
    [SerializeField] private ChoiceButton serverAddressChoice;
    [SerializeField] private TMP_InputField customAddress;
    [SerializeField] private TMP_Text chosenAddressText;

    private (string token, string provider) currentAuth;

    public async UniTask<T> GetAsync<T>(string uri,
        (string key, string value)[] queryParams = null,
        string json = null)
    {
        var uriBuilder = new UriBuilder(uri);

        if (queryParams?.Length > 0)
        {
            for (var i = 0; i < queryParams.Length - 1; i++)
                uriBuilder.Query += $"{queryParams[i].key}={queryParams[i].value}&";

            uriBuilder.Query += $"{queryParams.Last().key}={queryParams.Last().value}&";
        }

        var request = new HTTPRequest(uriBuilder.Uri);

        // request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json");

        if (json != null)
            request.AddField("data", json);

        var response = await request.GetHTTPResponseAsync();

        if (request.Exception is not null)
            throw request.Exception;

        if (!response.IsSuccess)
            throw new ServerRequestException(
                $"request didn't end successfully, " +
                $"request is {JsonConvert.SerializeObject(request, serializationSettings)[..MAX_DEBUG_LENGTH]} \n" +
                $"full response is {JsonConvert.SerializeObject(response, serializationSettings)[..MAX_DEBUG_LENGTH]}");

        response.Headers.TryGetValue("Content-Type", out var contentTypes);
        if (contentTypes == null)
            response.Headers.TryGetValue("content-type", out contentTypes);

        if (contentTypes == null)
            throw new Exception("the response doesn't have a content type header");

        if (!contentTypes.Contains("application/json"))
            throw new Exception(
                $"the content types: {string.Join(", ", contentTypes)} for http requests is not supported");

        return JsonConvert.DeserializeObject<T>(response.DataAsText);
    }

    public string GetServerAddress()
    {
        return serverAddressChoice.CurrentChoice >= addresses.Length
            ? customAddress.text
            : addresses[serverAddressChoice.CurrentChoice];
    }

    public NameValueCollection GetAuthQuery()
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        query["access_token"] = currentAuth.token;
        query["provider"] = currentAuth.provider;

        return query;
    }


    //I use event functions because awaiting returns hub conn and this is useless
    public void ConnectToServer(string accessToken, string provider)
    {
        currentAuth = (accessToken, provider);

        Debug.Log("connecting to server");

        var query = GetAuthQuery();

        var uriBuilder = new UriBuilder(Extensions.UriCombine(GetServerAddress(), "/connect"))
        {
            Query = query.ToString()
        };

        Debug.Log($"connecting with url {uriBuilder}");

        var hubOptions = new HubOptions
        {
            SkipNegotiation = true,
            PreferedTransport = TransportTypes.WebSocket,
        };

        hubConnection = new HubConnection(uriBuilder.Uri, protocol, hubOptions)
        {
            ReconnectPolicy = myReconnectPolicy,
        };

        //I don't have this term "authentication" despite I make token authentication
        // HubConnection.AuthenticationProvider = new DefaultAccessTokenAuthenticator(HubConnection);

        hubConnection.OnConnected += OnConnected;
        hubConnection.OnError += OnError;
        hubConnection.OnClosed += OnClosed;
        hubConnection.OnMessage += OnMessage;
        hubConnection.OnReconnecting += OnReconnecting;

        BlockingOperationManager.I.Forget(hubConnection.ConnectAsync().AsUniTask());
    }

    private bool OnMessage(HubConnection arg1, Message msg)
    {
        if (msg.type == MessageTypes.Invocation)
        {
#if !UNITY_WEBGL
            Debug.Log(
                $"msg is {msg.target} {JsonConvert.SerializeObject(msg, Formatting.Indented)}");
#else
            Debug.Log($"msg is {JsonUtility.ToJson(msg)}");
#endif
            HandleInvocationMessage(msg).Forget();
            return false;
        }

        return true;
    }
    private void OnConnected(HubConnection obj)
    {
        Connected = true;

        Debug.Log("connected to server");

        SignInPanel.DestroyModule();
        LangSelector.DestroyModule();

        Destroy(FindObjectOfType<GuestView>()?.gameObject);
    }
    private void OnClosed(HubConnection obj)
    {
        //don't restart game here because this is called only when the connection
        //is gracefully closed
        Debug.Log("OnClosed");
    }
    private void OnError(HubConnection arg1, string arg2)
    {
        RestartGame();
        Debug.Log($"OnError: {arg2}");
    }
    private void OnReconnecting(HubConnection arg1, string arg2)
    {
        //todo
        //if the client can recover from reconnection, we are okay
        //if it can't and we on error is not called when reconnecting is called, then we have to restart the game here
        //if on error is called when this is called, we can remove this callback
        Debug.Log("reconnecting");
    }

    [ContextMenu("restart")]
    public void RestartGame()
    {
        UniTask.Create(async () =>
        {
            try
            {
                await hubConnection.CloseAsync();
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            await SceneManager.LoadSceneAsync(0);
        }).Forget(e => throw e);
    }

    #region rpc with reflections

    private readonly Dictionary<string, (MethodInfo info, Type[] types)> rpcInfos = new();

    private void FetchRpcInfos()
    {
        //you need instance of each object of the fun when server calls
        //get the type of each one and pass the right object

        var namespaceTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && t.GetCustomAttribute<RpcAttribute>() != null);

        foreach (var type in namespaceTypes)
        {
            foreach (var method in type.GetMethods())
            {
                var attr = method.GetCustomAttribute<RpcAttribute>();
                if (attr == null) continue;
                rpcInfos.Add(attr.RpcName ?? method.Name, (method, method.GetParameterTypes()));
            }
        }
    }

    private readonly Dictionary<Type, object> rpcContainers = new();

    public void AddRpcContainer(object container)
    {
        var t = container.GetType();

        if (rpcContainers.ContainsKey(t))
            rpcContainers[t] = container;
        else
            rpcContainers.Add(t, container);
    }

    private readonly List<Message> pendingInvocations = new();

    private int messageIndex;

    private bool rpcCalling;

    private async UniTaskVoid HandleInvocationMessage(Message message)
    {
        if (message.target != Controller.I.InitGameName
            && (int)message.arguments[0] != messageIndex)
        {
            pendingInvocations.Add(message);
            return;
        }

        await UniTask.WaitUntil(() => !rpcCalling);

        rpcCalling = true;

        var method = rpcInfos[message.target];

        var realArgs = hubConnection.Protocol.GetRealArguments(method.types,
            message.arguments.Skip(1).ToArray());

        var container = method.info.IsStatic ? null : rpcContainers[method.info.DeclaringType!];

        if (method.info.ReturnType == typeof(UniTask))
            await method.info.InvokeAsync(container, realArgs);
        else
            method.info.Invoke(container, realArgs);

        messageIndex++;

        rpcCalling = false;

        if (pendingInvocations.Any(m => (int)m.arguments[0] == messageIndex))
            HandleInvocationMessage(pendingInvocations
                    .First(m => (int)m.arguments[0] == messageIndex))
                .Forget();
    }

    #endregion

    public void UpStreamCharTest()
    {
        var controller = hubConnection.GetUpStreamController<string, char>("UpStreamCharTest");
        UniTask.Create(async () =>
        {
            while (true)
            {
                controller.UploadParam((char)Random.Range(0, 200));
                await UniTask.Delay(500);
            }
        });
    }
}

public class ServerRequestException : Exception
{
    public ServerRequestException(string msg) : base(msg)
    {
    }

    public ServerRequestException() : base()
    {
    }
}