using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class LobbyController : MonoModule<LobbyController>
{
    public Transform Canvas;

    //todo, how the money aid will work now!!! it  can be instant with limit!! makes sense
    public void AddMoney(int amount)
    {
        AddMoneyPopup.Show(amount)
            .Forget(e => throw e);
    }

    public void Logout()
    {
        if (!FbManager.Logout())
            if (PlayerPrefs.HasKey("GuestGuid"))
                PlayerPrefs.DeleteKey("GuestGuid");
        //if not logged in with fb, logout from guest

        NetManager.I.RestartGame();
    }
}