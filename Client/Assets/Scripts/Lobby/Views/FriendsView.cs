using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class FriendsView : MonoBehaviour
{
    public static FriendsView I;

    public static async UniTask Create()
    {
        I = (await Addressables.InstantiateAsync("friendsView", LobbyController.I.Canvas))
            .GetComponent<FriendsView>();
    }

    /// <summary>
    /// this is legal because they are the same unit
    /// </summary>
    [SerializeField] private Transform container;

    public async UniTask ShowFriendList(bool followings)
    {
        var list = followings
            ? Repository.I.PersonalFullInfo.Followings
            : Repository.I.PersonalFullInfo.Followers;

        foreach (Transform go in container)
            Destroy(go.gameObject);
        foreach (var info in list)
            await MinUserView.Create(info, container);
    }

    private async UniTaskVoid Start()
    {
        await ShowFriendList(true);
    }
}