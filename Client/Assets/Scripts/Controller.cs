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
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
#if !UNITY_WEBGL
using Newtonsoft.Json;
#endif

[Rpc]
public class Controller : MonoModule<Controller>
{
    public GameObject canvasPrefab;
    [HideInInspector] public Transform canvas;

    protected override void Awake()
    {
        base.Awake();

        DontDestroyOnLoad(this);

        new BlockingOperationManager();

        HTTPManager.Logger = new MyBestHttpLogger();

        AddRpcContainer(this);

        FetchRpcInfos();
    }

    [SerializeField] private GameObject adPlaceholder;

    public void Start()
    {
        canvas = Instantiate(canvasPrefab, transform).GetComponent<Transform>();
        canvas.GetComponent<Canvas>().sortingOrder = 10;

        Background.Create().Forget();
        Toast.Create().Forget();
    }

    public void TstStartClient(string id)
    {
        ConnectToServer(fbigToken: id, name: "guest", demo: true);
    }

    [Rpc]
    public void InitGame(PersonalFullUserInfo myFullUserInfo, int messageIndex)
    {
        Debug.Log("InitGame is being called");

        new Repository(myFullUserInfo, null, null);

        Repository.I.PersonalFullInfo.DecreaseMoneyAimTimeLeft().Forget();

        this.messageIndex = messageIndex;

        SceneManager.LoadScene("Lobby");
    }


    #region hub

    private HubConnection hubConnection;

    [SerializeField] private int selectedAddress;
    [SerializeField] private string[] addresses;

    private string getAddress()
    {
        return addresses[selectedAddress] + "/connect";
    }

    private readonly IProtocol protocol = new JsonProtocol(new LitJsonEncoder());
    private readonly MyReconnectPolicy myReconnectPolicy = new MyReconnectPolicy();

    //I use event functions because awaiting returns hubconn and this is useless
    public void ConnectToServer(string fbigToken = null, string huaweiAuthCode = null,
        string facebookAccToken = null, string name = null, string pictureUrl = null,
        bool demo = false)
    {
        Debug.Log("connecting to server");

        var query = HttpUtility.ParseQueryString(string.Empty);

        if (facebookAccToken != null)
            query["fb_access_token"] = facebookAccToken;

        if (fbigToken != null)
            query["access_token"] = fbigToken;

        if (huaweiAuthCode != null)
            query["huaweiAuthCode"] = huaweiAuthCode;

        if (name != null)
            query["name"] = name;
        if (pictureUrl != null)
            query["pictureUrl"] = pictureUrl;

        if (demo)
            query["demo"] = "1";


        var uriBuilder = new UriBuilder(getAddress())
        {
            Query = query.ToString()
        };

        Debug.Log($"connecting with url {uriBuilder.ToString()}");

        var hubOptions = new HubOptions()
        {
            SkipNegotiation = true,
            PreferedTransport = TransportTypes.WebSocket,
        };

        hubConnection = new HubConnection(uriBuilder.Uri, protocol, hubOptions)
        {
            ReconnectPolicy = myReconnectPolicy,
        };

        // AssignGeneralRpcs();

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

    #endregion

    #region out rpcs

    public void ToggleFollow(string targetId)
    {
        SendAsync("ToggleFollow", targetId).Forget();
    }

    public async UniTask<bool> IsFollowing(string targetId)
    {
        return await InvokeAsync<bool>("IsFollowing", targetId);
    }

    public async UniTask<FullUserInfo> GetPublicFullUserInfo(string userId)
    {
        return await hubConnection.InvokeAsync<FullUserInfo>("GetUserData", userId);
    }

    public async UniTask NotifyTurnMiss()
    {
        await hubConnection.SendAsync("MissTurn");
    }

    public void ThrowCard(int cardId)
    {
        hubConnection.Send("Throw", cardId).OnError(e => throw e);
    }

    public async UniTask SelectItem(ItemType itemType, int id)
    {
        await hubConnection.SendAsync(
            itemType == ItemType.Cardback ? "SelectCardback" : "SelectBackground", id);
    }

    public async UniTask BuyItem(ItemType itemType, int id)
    {
        await hubConnection.SendAsync(
            itemType == ItemType.Cardback ? "BuyCardback" : "BuyBackground", id);
    }

    public async UniTask RequestRandomRoom(int betChoice, int capacityChoice)
    {
        await hubConnection.SendAsync("RequestRandomRoom", betChoice, capacityChoice);
    }


    public async UniTask<object> SendAsync(string method, params object[] args)
    {
        return await hubConnection.SendAsync(method, args);
    }

    public void Send(string method, params object[] args)
    {
        hubConnection.Send(method, args);
    }

    public async UniTask<T> InvokeAsync<T>(string method, params object[] args)
    {
        return await hubConnection.InvokeAsync<T>(method, args);
    }

    #endregion

    #region rpc with reflections

    private readonly Dictionary<string, (MethodInfo info, Type[] types)> rpcInfos =
        new Dictionary<string, (MethodInfo info, Type[] types)>();

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

    private readonly Dictionary<Type, object> rpcContainers = new Dictionary<Type, object>();

    public void AddRpcContainer(object container)
    {
        var t = container.GetType();

        if (rpcContainers.ContainsKey(t))
            rpcContainers[t] = container;
        else
            rpcContainers.Add(t, container);
    }

    private readonly List<Message> pendingInvocations = new List<Message>();

    private int messageIndex;

    private bool rpcCalling;
    [SerializeField] private GameObject lobbyControllerPrefab;

    private async UniTaskVoid HandleInvocationMessage(Message message)
    {
        if (message.target != nameof(InitGame)
            && (int) message.arguments[0] != messageIndex)
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

        if (pendingInvocations.Any(m => (int) m.arguments[0] == messageIndex))
            HandleInvocationMessage(pendingInvocations
                    .First(m => (int) m.arguments[0] == messageIndex))
                .Forget();
    }

    #endregion
}