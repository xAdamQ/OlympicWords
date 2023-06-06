using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MinUserView : MonoBehaviour
{
    [SerializeField] protected TMP_Text
        displayName,
        level,
        title;

    [SerializeField] protected Image picture;

    public MinUserInfo MinUserInfo { get; private set; }

    public static void Create(MinUserInfo info, Transform parent)
    {
        var muv = Instantiate(Coordinator.I.References.MuvPrefab, parent).GetComponent<MinUserView>();
        muv.Init(info);
    }

    protected void Init(MinUserInfo minUserInfo)
    {
        MinUserInfo = minUserInfo;

        Id = minUserInfo.Id;
        Level = minUserInfo.CalcLevel();
        DisplayName = minUserInfo.Name;

        SetPicture(minUserInfo).Forget();
    }

    private async UniTaskVoid SetPicture(MinUserInfo info)
    {
        //if I didn't assign the pic, it's not used
        //if I didn't get a pic, the user doesn't have one
        if (picture == null || !await info.GetPic()) return;

        //this go can be destroyed before the pic is awaited
        if (!picture) return;

        CacheManager.I.TryGetPic(Id, out var pic);
        picture.sprite = pic;
    }


    /// <summary>
    /// personal, room and final overrides this 
    /// </summary>
    public virtual void ShowFullInfo()
    {
        UniTask.Create(async () =>
        {
            var fullInfo = await BlockingOperationManager.Start(Controllers.User.Public(Id));
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