using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using BestHTTP;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Shared;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class NetManager : MonoModule<NetManager>
{
    private const int MAX_DEBUG_LENGTH = 200;

    private readonly JsonSerializerSettings serializationSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };

    protected override void Awake()
    {
        if (I) Destroy(I.gameObject);
        DontDestroyOnLoad(this);

        base.Awake();

#if !UNITY_EDITOR
        dev = false;
#endif

        SelectedAddress = dev ? "https://localhost:5001" : "https://wordwar3.azurewebsites.net";
    }

    public bool dev;
    public string SelectedAddress;

    public async Task<T> GetAsync<T>(string uri, List<(string key, string value)> queryParams = null,
        string json = null, bool auth = true)
    {
        if (auth)
        {
            var activeAuth = GetActiveAuth();
            queryParams ??= new();
            queryParams.Add(("provider", activeAuth.provider.ToString()));
            queryParams.Add(("access_token", activeAuth.token));
        }

        var uriBuilder = new UriBuilder(uri);

        if (queryParams?.Count > 0)
        {
            for (var i = 0; i < queryParams.Count - 1; i++)
                uriBuilder.Query += $"{queryParams[i].key}={queryParams[i].value}&";

            uriBuilder.Query += $"{queryParams.Last().key}={queryParams.Last().value}&";
        }

        var request = new HTTPRequest(uriBuilder.Uri);

        // request.AddHeader("Content-Type", "application/json");
        request.AddHeader("Accept", "application/json");

        if (json != null)
            request.AddField("data", json);

        Debug.Log($"Sending to: {uriBuilder.Uri} \n" +
                  $"with data: {json}, and queries: {JsonConvert.SerializeObject(queryParams)}");

        var response = await request.GetHTTPResponseAsync();

        if (request.Exception is not null)
            throw request.Exception;

        if (!response.IsSuccess)
            throw new ServerRequestException(
                $"request didn't end successfully, " +
                $"request is {JsonConvert.SerializeObject(request, serializationSettings)[..MAX_DEBUG_LENGTH]} \n" +
                $"full response is {JsonConvert.SerializeObject(response, serializationSettings)[..MAX_DEBUG_LENGTH]}");

        response.Headers.TryGetValue("Content-Type", out var contentTypes);
        if (contentTypes == null)
            response.Headers.TryGetValue("content-type", out contentTypes);

        if (contentTypes == null)
            throw new Exception("the response doesn't have a content type header");

        return JsonConvert.DeserializeObject<T>(response.DataAsText);
    }

    public async Task<(HTTPRequest, HTTPResponse)> SendAsyncHTTP(string uri,
        List<(string key, string value)> queryParams = null,
        string json = null, bool auth = true, HTTPMethods method = HTTPMethods.Get)
    {
        if (auth)
        {
            var activeAuth = GetActiveAuth();
            queryParams ??= new();
            queryParams.Add(("provider", activeAuth.provider.ToString()));
            queryParams.Add(("access_token", activeAuth.token));
        }

        var uriBuilder = new UriBuilder(uri);

        if (queryParams is not null && queryParams.Count > 0)
        {
            for (var i = 0; i < queryParams.Count - 1; i++)
                uriBuilder.Query += $"{queryParams[i].key}={queryParams[i].value}&";

            uriBuilder.Query += $"{queryParams.Last().key}={queryParams.Last().value}&";
        }

        var request = new HTTPRequest(uriBuilder.Uri, method);

        // request.AddHeader("Content-Type", "application/json");
        // request.AddHeader("Accept", "application/json");


        if (json != null)
        {
            request.RawData = System.Text.Encoding.UTF8.GetBytes(json);
            // request.FormUsage = HTTPFormUsage.UrlEncoded;
            // request.AddField("data", json);
        }

        var response = await request.GetHTTPResponseAsync();

        if (request.Exception is not null)
            throw request.Exception;

        if (!response.IsSuccess)
            throw new ServerRequestException(
                $"request didn't end successfully, " +
                $"request is {JsonConvert.SerializeObject(request, serializationSettings)[..MAX_DEBUG_LENGTH]} \n" +
                $"full response is {JsonConvert.SerializeObject(response, serializationSettings)[..MAX_DEBUG_LENGTH]}");

        return (request, response);
    }

    public NameValueCollection GetAuthQuery()
    {
        var query = HttpUtility.ParseQueryString(string.Empty);

        var (p, t) = GetActiveAuth();
        (query["provider"], query["access_token"]) = (p.ToString(), t);

        return query;
    }

    public void Logout()
    {
        var (provider, _) = GetActiveAuth();

        PlayerPrefs.DeleteKey("activeToken");
        PlayerPrefs.DeleteKey("activeProvider");
        PlayerPrefs.DeleteKey(provider + "Token");

        PlayerPrefs.Save();
    }

    private const string devServerWarn =
        "the server is hosted on a dev/free plan, so the first time you login the server gets initiated for you and takes time to warm up so expect the game to fail on the first 3 attempts and you may need to refresh the page.";
    public void BlockingLogin(string token, ProviderType provider)
    {
        BlockingOperationManager.Forget(Login(token, provider), msg: devServerWarn);
    }
    public UniTask BlockingLoginAwaitable(string token, ProviderType provider)
    {
        return BlockingOperationManager.Start(Login(token, provider), msg: devServerWarn);
    }

    public async UniTask Login(string token, ProviderType provider)
    {
        // if (!ClonesManager.IsClone())
        // {
        PlayerPrefs.SetString(provider + "Token", token);

        PlayerPrefs.SetString("activeToken", token);
        PlayerPrefs.SetString("activeProvider", provider.ToString());

        PlayerPrefs.Save();
        // }

        Repository.I.PersonalFullInfo = await Controllers.User.Personal();
        //current auth is then used to fetch the personal data

        // if (!ClonesManager.IsClone())
        // {
        SetToken(provider, token);
        // }

#if UNITY_WEBGL && !UNITY_EDITOR
        JsManager.HideFbButton();
#endif

        await Addressables.LoadSceneAsync("Lobby");
    }

    public (ProviderType provider, string token) GetActiveAuth()
    {
        // if (ClonesManager.IsClone())
        // {
        //     var customArgument = ClonesManager.GetArgument();
        //     Debug.Log("The custom argument of this clone project is: " + customArgument);
        //     return (ProviderType.Guest, customArgument);
        // }

        var t = PlayerPrefs.GetString("activeToken");
        var pStr = PlayerPrefs.GetString("activeProvider");
        var providerParsed = Enum.TryParse<ProviderType>(pStr, out var provider);

        return string.IsNullOrEmpty(t) || !providerParsed ? (default, null) : (provider, t);
    }

    public string GetToken(ProviderType provider)
    {
        return PlayerPrefs.GetString(provider + "Token");
    }

    private void SetToken(ProviderType provider, string token)
    {
        PlayerPrefs.SetString(provider + "Token", token);
        PlayerPrefs.Save();
    }

    [ContextMenu("restart")]
    public void RestartGame()
    {
        UniTask.Create(async () =>
        {
            try
            {
                await RoomNet.I.CloseConnection();
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            finally
            {
                await Addressables.LoadSceneAsync("Startup");
            }
        }).Forget(e => throw e);
    }
}

public class ServerRequestException : Exception
{
    public ServerRequestException(string msg) : base(msg)
    {
    }

    public ServerRequestException() : base()
    {
    }
}