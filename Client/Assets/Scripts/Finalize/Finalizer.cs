using Basra.Common;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;


public class Finalizer : MonoModule<Finalizer>
{
    public void Init(FinalizeResult finalizeResult)
    {
        //todo smash this and room result panel together
        // var finalMuvParent = (await Addressables.InstantiateAsync("finalMuvParent", RoomController.I.Canvas)).transform;
        //
        // for (int i = 0; i < RoomController.I.UserInfos.Count; i++)
        // {
        //     if (i == RoomController.I.MyTurn) continue;
        //
        //     FinalMuv.Create(RoomController.I.UserInfos[i], finalizeResult.UserRoomStatus[i], finalMuvParent).Forget();
        // }
        //
        // RoomResultPanel.Instantiate(RoomController.I.Canvas, finalizeResult.RoomXpReport,
        //     finalizeResult.PersonalFullUserInfo,
        //     finalizeResult.UserRoomStatus[RoomController.I.MyTurn]).Forget();
    }
}