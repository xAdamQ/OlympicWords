using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Scripting;

// [Preserve]
public class MinUserInfo
{
    [Preserve]
    public MinUserInfo()
    {
        PictureLoaded += OnPictureLoaded;
    }

    private void OnPictureLoaded(Texture2D texture2D)
    {
        IsPictureLoaded = true;
        if (texture2D != null)
            PictureSprite = Sprite.Create(texture2D,
                new Rect(0, 0, texture2D.width, texture2D.height), new Vector2(.5f, .5f));
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

    public Sprite PictureSprite { get; set; }

    public event Action<Texture2D> PictureLoaded;
    public bool IsPictureLoaded;

    private string PictureAddress { get; } =
        Extensions.UriCombine(NetManager.I.GetServerAddress(), "Picture", "GetUserPicture");

    public IEnumerator DownloadPicture()
    {
        if (string.IsNullOrEmpty(PictureAddress)) yield break;

        var query = NetManager.I.GetAuthQuery();
        query["userId"] = Id;

        var uriBuilder = new UriBuilder
            (PictureAddress)
            {
                Query = query.ToString(),
            };

        yield return Extensions.GetRemoteTexture(uriBuilder.Uri.ToString(), PictureLoaded);
    }
}