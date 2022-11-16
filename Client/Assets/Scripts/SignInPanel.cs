using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

public class SignInPanel : MonoModule<SignInPanel>
{
    [SerializeField] private GameObject guestLoginButton, havingTroubleButton;

    // ReSharper disable once Unity.IncorrectMethodSignature
    private async UniTaskVoid Start()
    {
        if (!FbManager.IsLoggedInBefore())
        {
            FbManager.ShowButton();
            ShowGuest();
            return;
        }

        if (!await FbManager.IsTokenValid())
        {
            FbManager.ShowButton();
            ShowHavingTrouble();
            return;
        }

        try
        {
            FbManager.CachedLogin();
        }
        catch (Exception e)
        {
            Debug.LogError("failed to auto login fb due to: " + e);
            FbManager.ShowButton();
            ShowHavingTrouble();
        }
    }

    private void ShowGuest()
    {
        guestLoginButton.SetActive(true);
    }

    private void ShowHavingTrouble()
    {
        havingTroubleButton.SetActive(true);
    }

    //called from javascript
    [UsedImplicitly]
    public void FbLogin(string responseStr)
    {
        FbManager.Login(responseStr);
    }

    public void LoginAsGuest()
    {
        var guid = new Guid().ToString();
        NetManager.I.Connected += cacheGuestGuid;
        NetManager.I.ConnectToServer(guid, "guest");

        void cacheGuestGuid()
        {
            PlayerPrefs.SetString("GuestGuid", guid);
        }
    }
}