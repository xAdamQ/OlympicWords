using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BestHTTP.SignalRCore;
using BestHTTP.SignalRCore.Encoders;
using BestHTTP.SignalRCore.Messages;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;
public class RoomNet : MonoModule<RoomNet>, IRoomHub
{
    private readonly IProtocol protocol = new JsonProtocol(new LitJsonEncoder());
    private readonly MyReconnectPolicy myReconnectPolicy = new();
    private UpStreamItemController<string> upStreamItemController;
    public bool IsConnected;
    private HubConnection hubConnection;

    public async Task<object> SendAsync(string method, params object[] args)
    {
        return await hubConnection.SendAsync(method, args);
    }
    protected override void Awake()
    {
        base.Awake();
        FetchRpcInfos();
    }
    private void DownStream()
    {
        downStreamController = hubConnection.GetDownStreamController<string[]>("DownStreamCharBuffer");
        downStreamController.OnError(f =>
        {
            Debug.LogError(f.Message);
            NetManager.I.RestartGame();
        });

        downStreamController.OnItem(DigitsReceived)
            .OnSuccess(_ => Debug.Log("Streaming finished!"));

        Closed += () => downStreamController.Cancel();

        // A stream request can be cancelled any time by calling the controller's Cancel method
        // controller.Cancel();
    }
    private void DigitsReceived(string[] playerBuffers)
    {
        for (var p = 0; p < playerBuffers.Length; p++)
            if (p != RootEnv.I.MyTurn)
                foreach (var digit in playerBuffers[p])
                    RootEnv.I.Players[p].TakeInput(digit);
    }
    private void UpStream()
    {
        upStreamItemController = hubConnection.GetUpStreamController<string, char>("UpStreamChar");

        upStreamItemController.OnError(f =>
        {
            Debug.LogError(f.Message);
            NetManager.I.RestartGame();
        });

        Closed += () => upStreamItemController.Cancel();

        // upStreamItemController.UploadParam((char)Random.Range(0, 200));
        // upStreamItemController.UploadParam((char)Random.Range(0, 200));
        // upStreamItemController.UploadParam((char)Random.Range(0, 200));
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

    private void Start()
    {
        StartRandomRoom(RoomRequester.LastRequest.betChoice, RoomRequester.LastRequest.env);
    }

    private void StartRandomRoom(int betChoice, string env)
    {
        if (GuestView.I)
            NetManager.I.SelectedAddress = GuestView.I.GetServerAddress();

        Debug.Log("connecting to server");

        var query = NetManager.I.GetAuthQuery();
        query["betChoice"] = betChoice.ToString();
        query["env"] = env;
        var uriBuilder = new UriBuilder(Extensions.UriCombine(NetManager.I.SelectedAddress, "/connect"))
        {
            Query = query.ToString()
        };

        Debug.Log($"connecting with url {uriBuilder}");

        // HTTPManager.Logger.Level = BestHTTP.Logger.Loglevels.All;
        var hubOptions = new HubOptions
        {
            SkipNegotiation = false,
            PreferedTransport = TransportTypes.WebSocket,
        };
        hubConnection = new HubConnection(uriBuilder.Uri, protocol, hubOptions)
        {
            ReconnectPolicy = myReconnectPolicy,
            //I authenticate with a token as a query param
            // AuthenticationProvider = null,
        };

        //I don't have this term "authentication" despite I make token authentication
        hubConnection.OnConnected += OnConnected;
        hubConnection.OnError += OnError;
        hubConnection.OnClosed += OnClosed;
        hubConnection.OnMessage += OnMessage;
        hubConnection.OnReconnecting += OnReconnecting;

        BlockingOperationManager.Forget(hubConnection.ConnectAsync());
    }

    public event Action Connected, Closed;

    private bool OnMessage(HubConnection arg1, Message msg)
    {
        if (msg.type != MessageTypes.Invocation) return true;
        Debug.Log($"msg is {JsonConvert.SerializeObject(msg)}");
        HandleInvocationMessage(msg).Forget();
        return false;
    }
    private void OnConnected(HubConnection _)
    {
        IsConnected = true;
        Debug.Log("connected to server");
        Connected?.Invoke();
    }
    private void OnClosed(HubConnection obj)
    {
        //don't restart game here because this is called only when the connection
        //is gracefully closed
        Debug.Log("OnClosed");
        Closed?.Invoke();
    }
    private void OnError(HubConnection arg1, string arg2)
    {
        NetManager.I.RestartGame();
        Debug.Log($"OnError: {arg2}");
    }
    private void OnReconnecting(HubConnection arg1, string arg2)
    {
        //if the client can recover from reconnection, we are okay
        //if it can't and we on error is not called when reconnecting is called, then we have to restart the game here
        //if on error is called when this is called, we can remove this callback
        Debug.Log("reconnecting");
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
                if (method.GetBaseDefinition() != method) continue;
                //ignore overriden methods, to use base only
                rpcInfos.Add(attr.RpcName ?? method.Name, (method, method.GetParameterTypes()));
            }
        }
    }
    private readonly Dictionary<Type, object> rpcContainers = new();
    public void AddRpcContainer(object container)
    {
        var type = container.GetType();
        if (rpcContainers.ContainsKey(type))
            rpcContainers[type] = container;
        else
            rpcContainers.Add(type, container);
    }
    public void AddRpcContainer(object container, Type type)
    {
        if (rpcContainers.ContainsKey(type))
            rpcContainers[type] = container;
        else
            rpcContainers.Add(type, container);
    }

    // private readonly List<Message> pendingInvocations = new();
    // private int messageIndex;
    // private bool rpcCalling;

    private static HashSet<RoomNet> nets = new();
    private DownStreamItemController<string[]> downStreamController;
    private async UniTaskVoid HandleInvocationMessage(Message message)
    {
        nets.Add(this);

        #region queued messages
        // if ((int)message.arguments[0] != messageIndex)
        // {
        //     pendingInvocations.Add(message);
        //     return;
        // }
        //
        // await UniTask.WaitUntil(() => !rpcCalling);

        // rpcCalling = true;
        #endregion

        var method = rpcInfos[message.target];
        var realArgs = hubConnection.Protocol.GetRealArguments(method.types,
            message.arguments.Skip(1).ToArray());
        var container = method.info.IsStatic ? null : rpcContainers[method.info.DeclaringType!];
        if (method.info.ReturnType == typeof(UniTask))
            await method.info.InvokeAsync(container, realArgs);
        else
            method.info.Invoke(container, realArgs);

        #region queued messages
        // messageIndex++;

        // rpcCalling = false;

        // if (pendingInvocations.Any(m => (int)m.arguments[0] == messageIndex))
        //     HandleInvocationMessage(pendingInvocations
        //             .First(m => (int)m.arguments[0] == messageIndex))
        //         .Forget();
        #endregion
    }
    #endregion

    public Task CloseConnection()
    {
        return hubConnection.CloseAsync();
    }

    #region hub
    public Task Ready()
    {
        return SendAsync(nameof(Ready));
    }
    public Task Surrender()
    {
        return SendAsync(nameof(Surrender));
    }

    public Task LeaveFinishedRoom()
    {
        return SendAsync(nameof(LeaveFinishedRoom));
    }

    public Task SetPowerUp(int powerUp)
    {
        return SendAsync(nameof(SetPowerUp), powerUp);
    }

    public Task<string> UpStreamChar(IAsyncEnumerable<char> stream)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<string[]> DownStreamCharBuffer(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<int> DownStreamTest(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    #endregion
}