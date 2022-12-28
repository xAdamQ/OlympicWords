using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Shared;
using UnityEngine;

public class SignInPanel : MonoModule<SignInPanel>
{
    [SerializeField] private GameObject guestLoginButton, havingTroubleButton, linkAdvice;

    // ReSharper disable once Unity.IncorrectMethodSignature
    // ReSharper disable once UnusedMember.Local
    private async UniTaskVoid Start()
    {
        var (p, t) = NetManager.I.GetActiveAuth();

        if (string.IsNullOrEmpty(t))
        {
            FbManager.ShowButton();
            ShowGuest();
        }
        else if (p == ProviderType.Facebook)
        {
            if (!await FbManager.IsTokenValid())
            {
                FbManager.ShowButton();
                ShowHavingTrouble();
            }
            else
            {
                try
                {
                    CachedLogin(p, t);
                }
                catch (Exception e)
                {
                    Debug.LogError("failed to auto login fb due to: " + e);
                    FbManager.ShowButton();
                    ShowHavingTrouble();
                }
            }
        }
        else if (p == ProviderType.Guest)
        {
            CachedLogin(p, t);
        }
    }
    private void CachedLogin(ProviderType p, string t)
    {
        BlockingOperationManager.Forget(NetManager.I.Login(t, p));
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
        var guestToken = NetManager.I.GetToken(ProviderType.Guest);

        if (string.IsNullOrEmpty(guestToken))
            guestToken = Guid.NewGuid().ToString();

        NetManager.I.Login(guestToken, ProviderType.Guest).Forget(e => throw e);
    }
}