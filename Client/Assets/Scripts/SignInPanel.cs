using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

public class SignInPanel : MonoModule<SignInPanel>
{
    [SerializeField] private GameObject guestLoginButton, havingTroubleButton, linkAdvice;

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
        if (!string.IsNullOrEmpty(PlayerPrefs.GetString("GuestGuid")))
            linkAdvice.SetActive(true);

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
        var guestToken = PlayerPrefs.GetString("GuestGuid");

        if (string.IsNullOrEmpty(guestToken))
        {
            guestToken = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("GuestGuid", guestToken);
            // NetManager.I.Connected += cacheGuestGuid;
        }

        NetManager.I.ConnectToServer(guestToken, "Guest");
    }
}