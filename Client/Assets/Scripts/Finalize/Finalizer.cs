using System.Collections.Generic;
using System.Linq;
using Basra.Common;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

[Rpc]
public class Finalizer : MonoModule<Finalizer>
{
    private FinalMuv[] FinalMuvs;
    private List<(int index, UserRoomStatus status)> FinishedUsersStatus = new();

    private bool finalized;

    [SerializeField] private GameObject view;
    [SerializeField] private Transform muvParent;

    public GameObject FinalMuvPrefab;

    protected override void Awake()
    {
        base.Awake();
        RoomNet.I.AddRpcContainer(this);
    }

    [Rpc]
    public void FinalizeRoom(UserRoomStatus myUserRoomStatus)
    {
        finalized = true;
        view.SetActive(true);

        // RoomUserView.Manager.I.RoomUserViews.ForEach(ruv => Destroy(ruv.gameObject));
        //todo destroy additional UI

        var info = Repository.I.PersonalFullInfo;
        info.Money += myUserRoomStatus.EarnedMoney;
        info.TotalEarnedMoney += myUserRoomStatus.EarnedMoney;
        info.Xp += myUserRoomStatus.Score;
        info.PlayedRoomsCount++;

        Repository.I.PersonalFullInfo.DecreaseMoneyAimTimeLeft().Forget();

        FinalMuvs = new FinalMuv[RootEnv.I.Capacity];

        for (var i = 0; i < RootEnv.I.UserInfos.Count; i++)
            FinalMuvs[i] = FinalMuv.Create(RootEnv.I.UserInfos[i], RootEnv.I.Players[i], muvParent);

        FinalMuvs[RootEnv.I.MyTurn].SetFinal(myUserRoomStatus);
        FinishedUsersStatus.ForEach(rs => FinalMuvs[rs.index].SetFinal(rs.status));

        foreach (var finalMuv in FinalMuvs.Where(fm => !fm.Finished))
            finalMuv.SetTemporalStatus();
    }

    [Rpc]
    public void TakeOppoUserRoomStatus(int userIndex, UserRoomStatus userRoomStatus)
    {
        Debug.Log($"taking status of: {userIndex} with {JsonConvert.SerializeObject(userRoomStatus)}");
        if (finalized)
            FinalMuvs[userIndex].SetFinal(userRoomStatus);
        else
            FinishedUsersStatus.Add((userIndex, userRoomStatus));
    }


    /// <summary>
    /// uses roomController, lobbyFac
    /// </summary>
    public void ToLobby()
    {
        UniTask.Create(async () =>
        {
            await RoomNet.I.LeaveFinishedRoom();
            SceneManager.LoadScene("Lobby");
        }).Forget(e => throw e);
    }
}