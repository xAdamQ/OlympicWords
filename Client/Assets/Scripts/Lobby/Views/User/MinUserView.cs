using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class MinUserView : MonoBehaviour
{
    [SerializeField] protected TMP_Text
        displayName,
        level,
        title;

    [SerializeField] protected Image picture;

    public MinUserInfo MinUserInfo { get; set; }

    public static MinUserView Create(MinUserInfo info, Transform parent)
    {
        var muv = Instantiate(Controller.I.References.MuvPrefab, parent).GetComponent<MinUserView>();
        muv.Init(info);
        return muv;
    }

    public void Init(MinUserInfo minUserInfo)
    {
        MinUserInfo = minUserInfo;

        Id = minUserInfo.Id;
        Level = minUserInfo.CalcLevel();
        DisplayName = minUserInfo.Name;
        Title = PlayerBase.Titles[minUserInfo.SelectedTitleId];

        SetPicture(minUserInfo).Forget();
    }


    private async UniTaskVoid SetPicture(MinUserInfo info)
    {
        if (info.IsPictureLoaded)
        {
            if (destroyed || info.PictureSprite is null)
                return;
        }
        else
        {
            await info.DownloadPicture();
        }

        picture.sprite = info.PictureSprite;
    }

    private bool destroyed;

    private void OnDestroy()
    {
        destroyed = true;
    }

    /// <summary>
    /// personal, room and final overrides this 
    /// </summary>
    public virtual void ShowFullInfo()
    {
        UniTask.Create(async () =>
        {
            var fullInfo = await BlockingOperationManager.I.Start(Controllers.User.Public(Id));
            fullInfo.PictureSprite = MinUserInfo.PictureSprite;
            FullUserView.Show(fullInfo);
        });
    }

    protected string Id;

    public int Level
    {
        set
        {
            if (level)
                level.text = value.ToString();
        }
    }

    public string DisplayName
    {
        set
        {
            if (displayName)
                displayName.text = value;
        }
    }

    public string Title
    {
        set
        {
            if (title)
                title.text = value;
        }
    }
}