using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using BestHTTP;
using BestHTTP.SignalRCore;
using UnityEngine;
using BestHTTP.SignalRCore.Encoders;
using BestHTTP.SignalRCore.Messages;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Shared;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

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
    }

    public string SelectedAddress;

    // private (string token, string provider) currentAuth;

    public async Task<T> GetAsync<T>(string uri,
        (string key, string value)[] queryParams = null,
        string json = null)
    {
        var uriBuilder = new UriBuilder(uri);

        if (queryParams?.Length > 0)
        {
            for (var i = 0; i < queryParams.Length - 1; i++)
                uriBuilder.Query += $"{queryParams[i].key}={queryParams[i].value}&";

            uriBuilder.Query += $"{queryParams.Last().key}={queryParams.Last().value}&";
        }

        uriBuilder.Query += "access_token=531ecc3b-b99b-4aac-b364-680fda0afa8e&";
        uriBuilder.Query += "provider=Guest&";

        var request = new HTTPRequest(uriBuilder.Uri);

        // request.AddHeader("Content-Type", "application/json");
        // request.AddHeader("Accept", "application/json");


        if (json != null)
            request.AddField("data", json);

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

        // if (!contentTypes.Contains("application/json"))
        //     throw new Exception(
        //         $"the content types: {string.Join(", ", contentTypes)} for http requests is not supported");

        return JsonConvert.DeserializeObject<T>(response.DataAsText);
    }

    public async Task<(HTTPRequest, HTTPResponse)> SendAsyncHTTP(string uri,
        (string key, string value)[] queryParams = null,
        string json = null,
        HTTPMethods method = HTTPMethods.Get
    )
    {
        var uriBuilder = new UriBuilder(uri);

        if (queryParams is not null && queryParams.Length > 0)
        {
            for (var i = 0; i < queryParams.Length - 1; i++)
                uriBuilder.Query += $"{queryParams[i].key}={queryParams[i].value}&";

            uriBuilder.Query += $"{queryParams.Last().key}={queryParams.Last().value}&";
        }

        uriBuilder.Query += "access_token=531ecc3b-b99b-4aac-b364-680fda0afa8e&";
        uriBuilder.Query += "provider=Guest&";

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

    public async UniTask Login(string token, ProviderType provider)
    {
        PlayerPrefs.SetString(provider + "token", token);

        Repository.I.PersonalFullInfo = await Controllers.User.Personal();
        //current auth is then used to fetch the personal data

        PlayerPrefs.SetString("activeToken", token);
        PlayerPrefs.SetString("activeProvider", provider.ToString());

        SetToken(provider, token);

#if UNITY_WEBGL && !UNITY_EDITOR
        JsManager.HideFbButton();
#endif

        SceneManager.LoadScene("Lobby");
    }

    public (ProviderType, string) GetActiveAuth()
    {
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

            await SceneManager.LoadSceneAsync("Startup");
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