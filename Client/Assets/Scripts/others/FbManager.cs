using System;
using System.Collections.Generic;
using System.Web;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;
using Shared;

public static class FbManager
{
    private class LoginStatus
    {
        public FbAuthResponse AuthResponse { get; set; }
        public string Status { get; set; }

        [UsedImplicitly]
        public class FbAuthResponse
        {
            public string AccessToken { get; set; }
            public string ExpiresIn { get; set; }
            public string SignedRequest { get; set; }
            public string UserID { get; set; }
        }
    }

    [UsedImplicitly]
    private class ValidationResponse
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public ValidationData data { get; set; }

        [UsedImplicitly]
        public class ValidationData
        {
            // ReSharper disable once InconsistentNaming
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public bool is_valid { get; set; }
        }
    }

    public static async UniTask<bool> IsTokenValid()
    {
        var token = NetManager.I.GetToken(ProviderType.Facebook);

        const string clientToken = "468588098648394|CwbC4U-0WDoPAaeP79TTG7ELfD4";
        const string fbBaseAddress = "https://graph.facebook.com/v15.0/";

        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        queryParams.Add("input_token", token);
        queryParams.Add("access_token", clientToken);

        const string address = fbBaseAddress + "debug_token";

        var uri = new UriBuilder(address) { Query = queryParams.ToString()! }.ToString();

        try
        {
            var response = await NetManager.I.GetAsync<ValidationResponse>(uri);
            return response.data.is_valid;
        }
        catch (Exception e)
        {
            Console.WriteLine("couldn't validate the cached token due to the error: " + e);
            return false;
        }
    }

    public static void ShowButton()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
            JsManager.ShowFbButton();
#endif
    }

   

    private static LoginStatus LoginBase(string responseStr)
    {
        Debug.Log("fb login in unity called with data: " + responseStr);

        var response = JsonConvert.DeserializeObject<LoginStatus>(responseStr);
        if (response == null) throw new NullReferenceException("rb response is null");

        // NetManager.I.SetToken(ProviderType.Facebook, response.AuthResponse.AccessToken);

        return response;
    }

    public static void Login(string responseStr)
    {
        var response = LoginBase(responseStr);

        BlockingOperationManager.I.Forget
            (NetManager.I.Login(response.AuthResponse.AccessToken, ProviderType.Facebook));
    }

    public static void UpgradeGuestToFb(string responseStr)
    {
        var response = LoginBase(responseStr);

        const string message = "you will link your local progress to facebook, but in case facebook " +
                               "profile has progress do you want to overwrite it with local?";

        var choices = new List<(string, UniTask)>
        {
            ("YES", link(true)),
            ("NO", link(false)),
            ("login with facebook account separately", justLogin())
        };

        Popup.Show(message, choices);

        async UniTask link(bool overwrite)
        {
            var guestToken = NetManager.I.GetToken(ProviderType.Guest);
            //not that the last login token is now facebook because login base changed it

            var linkOp = Controllers.User.LinkTo(guestToken, "Guest", "Facebook",
                response.AuthResponse.AccessToken, overwrite);
            //in case something rather than 2xx ok is returned, the exception will be thrown so we don't continue 
            //to remove the guest guid

            await BlockingOperationManager.I.Start(linkOp);
        }

        UniTask justLogin()
        {
            var op = NetManager.I.Login(response.AuthResponse.AccessToken, ProviderType.Facebook);
            return BlockingOperationManager.I.Start(op);
        }
    }

    public static bool Logout()
    {
        if (PlayerPrefs.HasKey("fbToken"))
        {
            PlayerPrefs.DeleteKey("fbToken");
            return true;
        }

        return false;
    }
}