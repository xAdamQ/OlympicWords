using UnityEngine;

public class FriendsView : MonoModule<FriendsView>
{
    /// <summary>
    /// this is legal because they are the same unit
    /// </summary>
    [SerializeField] private Transform container;

    public void ShowFriendList(bool followings)
    {
        var list = followings
            ? Repository.I.PersonalFullInfo.Followings
            : Repository.I.PersonalFullInfo.Followers;

        foreach (Transform go in container)
            Destroy(go.gameObject);
        foreach (var info in list)
            MinUserView.Create(info, container);
    }

    private void Start()
    {
        ShowFriendList(true);
    }
}