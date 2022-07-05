using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

[Rpc]
public class LobbyController : MonoModule<LobbyController>
{
    public Transform Canvas;

    protected override void Awake()
    {
        base.Awake();
        NetManager.I.AddRpcContainer(this);
    }

    [Rpc]
    public void AddMoney(int amount)
    {
        AddMoneyPopup.Show(amount)
            .Forget(e => throw e);
    }

    [Rpc]
    public void PrepareRequestedRoomRpc(List<FullUserInfo> userInfos, int myTurn, string text)
    {
        DestroyLobby();

        var roomArgs = (RoomRequester.LastRequest.betChoice, RoomRequester.LastRequest.capacityChoice, userInfos,
            myTurn, text);

        Controller.I.AddTransitionData(nameof(roomArgs), roomArgs);

        SceneManager.LoadScene("Room");
        SceneManager.LoadScene("Demo 1", LoadSceneMode.Additive);
    }

    [Rpc]
    public void ChallengeRequest(MinUserInfo senderInfo)
    {
        ChallengeResponsePanel.Show(senderInfo);
    }

    [Rpc]
    public void RespondChallenge(bool response)
    {
        if (!response)
        {
            BlockingPanel.Hide();
            Toast.I.Show(Translatable.GetText("player_rejected"));
        }
        else
        {
            BlockingPanel.HideDismiss();
            //panel is removed at start
            Toast.I.Show(Translatable.GetText("creating_room"));
        }
    }

    public event Action Destroyed;

    private void DestroyLobby()
    {
        I = null;

        Destroyed?.Invoke();

        Destroy(gameObject);
    }
}