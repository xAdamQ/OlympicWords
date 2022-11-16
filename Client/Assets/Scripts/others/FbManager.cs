using System;
using System.Web;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

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
        var token = PlayerPrefs.GetString("fbToken");

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

    public static bool IsLoggedInBefore()
    {
        var cachedToken = PlayerPrefs.GetString("fbToken");
        return !string.IsNullOrEmpty(cachedToken);
    }

    public static void ShowButton()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
            JsManager.ShowFbButton();
#endif
    }

    public static void CachedLogin()
    {
        var cachedToken = PlayerPrefs.GetString("fbToken");
        NetManager.I.ConnectToServer(cachedToken, "facebook");
    }

    public static void Login(string responseStr)
    {
        Debug.Log("fb login in unity called with data: " + responseStr);

        var response = JsonConvert.DeserializeObject<LoginStatus>(responseStr);
        if (response == null) throw new NullReferenceException("rb response is null");

        PlayerPrefs.SetString("fbToken", response.AuthResponse.AccessToken);

        NetManager.I.ConnectToServer(response.AuthResponse.AccessToken, "facebook");
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