using System;
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
using Newtonsoft.Json;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class NetManager : MonoModule<NetManager>
{
    private readonly IProtocol protocol = new JsonProtocol(new LitJsonEncoder());
    private readonly MyReconnectPolicy myReconnectPolicy = new();
    private HubConnection hubConnection;
    private UpStreamItemController<string> upStreamItemController;
    private const int MAX_DEBUG_LENGTH = 200;

    private readonly JsonSerializerSettings serializationSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    protected override void Awake()
    {
        base.Awake();

        DontDestroyOnLoad(this);

        FetchRpcInfos();

        serverAddressChoice.ChoiceChanged += _ => chosenAddressText.text = GetServerAddress();
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

    private (string token, string provider) CurrentAuth;

    public async UniTask<T> GetAsync<T>(string methodName,
        (string key, string value)[] queryParams = null,
        string json = null)
    {
        var uriBuilder = new UriBuilder(Extensions.UriCombine(GetServerAddress(), methodName));

        if (queryParams?.Length > 0)
        {
            for (var i = 0; i < queryParams.Length - 1; i++)
                uriBuilder.Query += $"{queryParams[i].key}={queryParams[i].value}&";

            uriBuilder.Query += $"{queryParams.Last().key}={queryParams.Last().value}&";
        }

        var request = new HTTPRequest(uriBuilder.Uri);

        request.AddHeader("Content-Type", "application/json");
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

        if (contentTypes[0] == "application/json")
            return JsonConvert.DeserializeObject<T>(response.DataAsText);

        throw new Exception(
            $"the content type: {contentTypes[0]} for http requests is not supported");
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

        query["access_token"] = CurrentAuth.token;
        query["provider"] = CurrentAuth.provider;

        return query;
    }


    //I use event functions because awaiting returns hub conn and this is useless
    public void ConnectToServer(string accessToken, string provider)
    {
        CurrentAuth = (accessToken, provider);

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