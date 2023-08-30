using System;
using System.Collections;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

public class MinUserInfo
{
    [Preserve]
    public MinUserInfo()
    {
    }

    public int CalcLevel()
    {
        return GetLevelFromXp(Xp);
    }

    private const int MAX_LEVEL = 999;
    private const float EXPO = .55f, DIVIDER = 10;

    private static int GetLevelFromXp(int xp)
    {
        var level = (int)(Mathf.Pow(xp, EXPO) / DIVIDER);
        return level < MAX_LEVEL ? level : MAX_LEVEL;
    }

    //transferred model
    public string Id { get; set; }
    public virtual int Xp { get; set; }
    public int SelectedTitleId { get; set; }
    private int selectedTitleId;
    public string Name { get; set; }
    public float AverageWpm { get; set; }

    private string PictureAddress { get; } =
        Extensions.UriCombine(NetManager.I.SelectedAddress, "Picture", "GetUserPicture");

    /// <summary>
    /// try to get from cache, if not, get from server
    /// </summary>
    public async UniTask<bool> GetPic()
    {
        if (CacheManager.I.TryGetPic(Id, out _)) return true;

        Debug.Log($"pic for {Id} was not found in the cache");

        var (successful, pic) = await GetPicFromServer();
        if (successful)
        {
            CacheManager.I.AddPlayerPic(Id, pic);
            return true;
        }

        return false;
    }
    private async Task<(bool successful, Sprite sprite)> GetPicFromServer()
    {
        var query = NetManager.I.GetAuthQuery();
        query["userId"] = Id;

        var uriBuilder = new UriBuilder
            (PictureAddress)
            {
                Query = query.ToString(),
            };

        Texture2D texture2D;
        try
        {
            texture2D = await Extensions.GetRemoteTexture(uriBuilder.Uri.ToString());
        }
        catch (Exception)
        {
            return (false, null);
        }

        var pic = Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(.5f, .5f));
        return (true, pic);
    }
}