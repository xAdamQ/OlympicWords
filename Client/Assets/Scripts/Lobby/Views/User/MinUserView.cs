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
            SetPicture(minUserInfo.Picture);
        else
            minUserInfo.PictureLoaded += pic => SetPicture(pic);
    }

    private void SetPicture(Texture2D texture2D)
    {
        if (destroyed) return;

        if (texture2D != null)
            picture.sprite =
                Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height),
                    new Vector2(.5f, .5f));
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
        BlockingOperationManager.I.Forget(Controller.I.GetPublicFullUserInfo(Id),
            FullUserView.Show);
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