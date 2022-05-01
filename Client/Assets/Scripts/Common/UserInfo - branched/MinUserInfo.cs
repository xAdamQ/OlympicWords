using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;

// [Preserve]
public class MinUserInfo
{
    [Preserve]
    public MinUserInfo()
    {
        UniTask.Create(async () =>
        {
            await UniTask.DelayFrame(1); //to get data from object inititalizer >> {abdc = value}
            DownloadPicture().Forget();
        });
    }

    public int CalcLevel()
    {
        return GetLevelFromXp(Xp);
    }

    private const int MaxLevel = 999;
    private const float Expo = .55f, Divi = 10;
    private static int GetLevelFromXp(int xp)
    {
        var level = (int)(Mathf.Pow(xp, Expo) / Divi);
        return level < MaxLevel ? level : MaxLevel;
    }

    //transferred model
    public string Id { get; set; }
    public virtual int Xp { get; set; }
    public int SelectedTitleId { get; set; }
    private int selectedTitleId;
    public string Name { get; set; }
    public string PictureUrl { get; set; }

    public Texture2D Picture { get; set; }

    public event Action<Texture2D> PictureLoaded;
    public bool IsPictureLoaded;

    private async UniTaskVoid DownloadPicture()
    {
        if (string.IsNullOrEmpty(PictureUrl)) return;

        Picture = await GetRemoteTexture(PictureUrl);
        PictureLoaded?.Invoke(Picture);
        IsPictureLoaded = true;
    }

    public static async UniTask<Texture2D> GetRemoteTexture(string url)
    {
        using (var www = UnityWebRequestTexture.GetTexture(url))
        {
            // begin request:
            var asyncOp = www.SendWebRequest();

            // await until it's done: 
            try
            {
                await asyncOp;
            }
            catch (Exception)
            {
                Debug.Log($"{www.error}, URL:{www.url}");
            }

            // read results:
            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                // log error:
#if DEBUG
                Debug.Log($"{www.error}, URL:{www.url}");
#endif

                // nothing to return on error:
                return null;
            }

            // return valid results:
            Debug.Log($"img from url {url} downloaded");
            return DownloadHandlerTexture.GetContent(www);
        }
    }
}