using System;
using System.Collections;
using System.Web;
using BestHTTP.SignalRCore;
using UnityEngine;
using BestHTTP.SignalRCore.Encoders;
using BestHTTP.SignalRCore.Messages;

public class NetManager : MonoModule<NetManager>
{
    private readonly IProtocol protocol = new JsonProtocol(new LitJsonEncoder());
    private readonly MyReconnectPolicy myReconnectPolicy = new MyReconnectPolicy();
    private HubConnection hubConnection;
    private UpStreamItemController<string> upStreamItemController;

    private void Start()
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        var uriBuilder = new UriBuilder("http://localhost:5112/connect")
        {
            Query = query.ToString()
        };

        var hubOptions = new HubOptions()
        {
            SkipNegotiation = true,
            PreferedTransport = TransportTypes.WebSocket,
        };

        hubConnection = new HubConnection(uriBuilder.Uri, protocol, hubOptions)
        {
            ReconnectPolicy = myReconnectPolicy,
        };

        hubConnection.OnConnected += _ =>
        {
            FromServer();
            ToServer();
        };
        hubConnection.OnError += (_, msg) => Debug.Log(msg);

        hubConnection.StartConnect();
    }

    private void FromServer()
    {
        var controller = hubConnection.GetDownStreamController<int>("Counter", 10, 1000);

        controller.OnItem(result => Debug.Log("New item arrived: " + result.ToString()))
            .OnSuccess(_ => Debug.Log("Streaming finished!"))
            .OnError(error => Debug.Log("Error: " + error));

        // A stream request can be cancelled any time by calling the controller's Cancel method
        // controller.Cancel();
    }

    private void ToServer()
    {
        upStreamItemController = hubConnection.GetUpStreamController<string, char>("StreamChar");
        upStreamItemController.OnSuccess(result => { Debug.Log($"Upload finished: {result}"); });
    }

    public void StreamChar(char chr)
    {
        upStreamItemController.UploadParam(chr);
    }
}