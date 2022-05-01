using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public interface ILobbyController
{
    void PrepareRequestedRoomRpc(int betChoice, int capacityChoice, List<FullUserInfo> userInfos,
        int myTurn);

    event Action Destroyed;
}

[Rpc]
public class LobbyController : MonoModule<LobbyController>
{
    public GameObject roomControllerPrefab;
    public Transform Canvas;

    private async UniTaskVoid Initialize()
    {
        Controller.I.AddRpcContainer(this);

        await UniTask.DelayFrame(1);

        //await FriendsView.Create();

        //SoundButton.Create();

        //await PersonalActiveUserView.Create();

        //await RoomRequester.Create();

        Background.I.SetForLobby();
    }

    [Rpc]
    public void AddMoney(int amount)
    {
        AddMoneyPopup.Show(amount)
            .Forget(e => throw e);
    }

    [Rpc]
    public void PrepareRequestedRoomRpc(int betChoice, int capacityChoice,
        List<FullUserInfo> userInfos, int myTurn)
    {
        DestroyLobby();

        Instantiate(roomControllerPrefab).GetComponent<RoomController>()
            .Init(betChoice, capacityChoice, userInfos, myTurn);
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