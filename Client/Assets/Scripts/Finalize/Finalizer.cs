using System.Collections.Generic;
using System.Linq;
using Basra.Common;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

[Rpc]
public class Finalizer : MonoModule<Finalizer>
{
    private FinalMuv[] FinalMuvs;
    private List<(int index, UserRoomStatus status)> FinishedUsersStatus = new();

    private bool finalized;

    [SerializeField] private GameObject view;

    protected override void Awake()
    {
        base.Awake();
        NetManager.I.AddRpcContainer(this);
    }

    [Rpc]
    public void FinalizeRoom(UserRoomStatus myUserRoomStatus)
    {
        finalized = true;

        // RoomUserView.Manager.I.RoomUserViews.ForEach(ruv => Destroy(ruv.gameObject));
        //todo destroy additional UI

        var info = Repository.I.PersonalFullInfo;
        info.Money += myUserRoomStatus.EarnedMoney;
        info.TotalEarnedMoney += myUserRoomStatus.EarnedMoney;
        info.Xp += myUserRoomStatus.Score;
        info.PlayedRoomsCount++;

        Repository.I.PersonalFullInfo.DecreaseMoneyAimTimeLeft().Forget();

        FinalMuvs = new FinalMuv[RoomBase.I.Capacity];

        UniTask.Create(async () =>
        {
            for (var i = 0; i < RoomBase.I.UserInfos.Count; i++)
                FinalMuvs[i] = await FinalMuv.Create(RoomBase.I.UserInfos[i], EnvBase.I.Players[i], transform);

            FinalMuvs[RoomBase.I.MyTurn].SetFinal(myUserRoomStatus);
            FinishedUsersStatus.ForEach(rs => FinalMuvs[rs.index].SetFinal(rs.status));

            foreach (var finalMuv in FinalMuvs.Where(fm => !fm.Finished))
                finalMuv.SetTemporalStatus();

            view.SetActive(true);
        });
    }

    [Rpc]
    public void TakeOppoUserRoomStatus(int userIndex, UserRoomStatus userRoomStatus)
    {
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
            await MasterHub.I.LeaveFinishedRoom();
            SceneManager.LoadScene("Lobby");
        }).Forget(e => throw e);
    }
}