using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class MinUserView : MonoBehaviour
{
    [SerializeField] private TMP_Text
        displayName,
        level,
        title;

    [SerializeField] private Image picture;

    public MinUserInfo MinUserInfo { get; set; }

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
    /// personal and room view overrides this 
    /// </summary>
    public virtual void ShowFullInfo()
    {
        BlockingOperationManager.I.Forget(MasterHub.I.GetUserData(Id), FullUserView.Show);
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

    public static async UniTask<MinUserView> Create(MinUserInfo info, Transform parent)
    {
        var muv = (await Addressables.InstantiateAsync("minUserView", parent))
            .GetComponent<MinUserView>();

        muv.Init(info);

        return muv;
    }
}