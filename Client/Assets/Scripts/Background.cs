#if ADDRESSABLES
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

vpublic class Background : MonoModule<Background>
{
    /// <summary>
    /// uses RoomController
    /// </summary>
    public void SetForRoom(List<FullUserInfo> userInfos)
    {
        var maxLevel = userInfos.Max(u => u.Xp);
        var bgId = userInfos.First(u => u.Xp == maxLevel).SelectedBackground;

        Extensions.LoadAndReleaseAsset<Sprite>(((BackgroundType)bgId).ToString(),
                sprite => GetComponent<Image>().sprite = sprite)
            .Forget(e => throw e);
    }

    public void SetForLobby()
    {
        Extensions.LoadAndReleaseAsset<Sprite>(BackgroundType.brownLeaf.ToString(),
                sprite => GetComponent<Image>().sprite = sprite)
            .Forget(e => throw e);
    }
}
#endif