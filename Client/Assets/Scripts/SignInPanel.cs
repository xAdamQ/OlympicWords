using System;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Shared;
using UnityEngine;

public class SignInPanel : MonoModule<SignInPanel>
{
    [SerializeField] private GameObject guestLoginButton, havingTroubleButton, linkAdvice;

    private void Start()
    {
        ShowSuitableLogin().Forget();
    }

    private int retryCount;
    private readonly int[] retryPolicy = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

    private async UniTaskVoid ShowSuitableLogin()
    {
        if (retryCount > 0)
        {
            var waitIndex = Math.Min(retryCount - 1, retryPolicy.Length - 1);
            var waitSeconds = retryPolicy[waitIndex];
            await UniTask.Delay(TimeSpan.FromSeconds(waitSeconds));
            Toast.I.Show($"retry in {waitSeconds}");
        }

        retryCount++;

        var (p, t) = NetManager.I.GetActiveAuth();

        if (string.IsNullOrEmpty(t))
        {
            FbManager.ShowButton();
            ShowGuest();
            return;
        }

        switch (p)
        {
            case ProviderType.Facebook when !await FbManager.IsTokenValid():
                FbManager.ShowButton();
                ShowHavingTrouble();
                break;
            case ProviderType.Facebook:
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
                break;
            case ProviderType.Guest:
                CachedLogin(p, t);
                break;
            case ProviderType.Huawei:
                throw new NotImplementedException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void CachedLogin(ProviderType p, string t)
    {
        UniTask.Create(async () =>
        {
            try
            {
                await NetManager.I.BlockingLoginAwaitable(t, p);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                HideAll();
                ShowSuitableLogin().Forget();
            }
        });
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

        NetManager.I.BlockingLogin(guestToken, ProviderType.Guest);
    }

    private void HideAll()
    {
        guestLoginButton.SetActive(false);
#if UNITY_WEBGL && !UNITY_EDITOR
            JsManager.HideFbButton();
#endif
    }
}