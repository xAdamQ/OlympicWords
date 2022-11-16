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

    public static async UniTask<MinUserView> Create(MinUserInfo info, Transform parent)
    {
        var muv = (await Addressables.InstantiateAsync("minUserView", parent))
            .GetComponent<MinUserView>();

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

        if (minUserInfo.IsPictureLoaded)
            SetPicture(minUserInfo.PictureSprite);
        else
        {
            minUserInfo.PictureLoaded += _ => SetPicture(minUserInfo.PictureSprite);
            StartCoroutine(minUserInfo.DownloadPicture());
        }
    }

    private void SetPicture(Sprite sprite)
    {
        if (destroyed || sprite is null) return;

        picture.sprite = sprite;
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
            var fullInfo = await BlockingOperationManager.I.Start(MasterHub.I.GetUserData(Id));
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