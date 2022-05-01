using Basra.Common;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class FinalMuv : MonoBehaviour
{
    private string Id;

    [SerializeField] private Image picture;
    [SerializeField] private TMP_Text
        eatenCardsText,
        basrasText,
        bigBasrasText,
        winMoneyText;

    [SerializeField] private Button followButton;

    private FullUserInfo fullUserInfo;

    public void Init(FullUserInfo fullUserInfo, UserRoomStatus oppoRoomResult)
    {
        this.fullUserInfo = fullUserInfo;

        Id = fullUserInfo.Id;

        eatenCardsText.text = oppoRoomResult.EatenCards.ToString();
        basrasText.text = oppoRoomResult.Basras.ToString();
        bigBasrasText.text = oppoRoomResult.BigBasras.ToString();
        winMoneyText.text = oppoRoomResult.WinMoney.ToString();

        UpdateFriendShipView();

        if (fullUserInfo.IsPictureLoaded)
            SetPicture(fullUserInfo.Picture);
        else
            fullUserInfo.PictureLoaded += pic => SetPicture(pic);
    }

    public static async UniTaskVoid Create(FullUserInfo fullUserInfo, UserRoomStatus oppoRoomResult,
        Transform parent)
    {
        var asset = await Addressables.InstantiateAsync("finalMuv", parent);
        asset.GetComponent<FinalMuv>().Init(fullUserInfo, oppoRoomResult);
    }

    private void SetPicture(Texture2D texture2D)
    {
        if (texture2D != null)
            picture.sprite =
                Sprite.Create(texture2D, new Rect(0, 0, texture2D.width, texture2D.height),
                    new Vector2(.5f, .5f));
    }

    /// <summary>
    /// uses BlockingOperationManager, Controller, FullUserView
    /// </summary>
    public void ShowFullInfo()
    {
        followButton.interactable = false;

        FullUserView.Show(Id == Repository.I.PersonalFullInfo.Id
            ? Repository.I.PersonalFullInfo
            : fullUserInfo);
    }

    public void ToggleFollow()
    {
        UniTask.Create(async () =>
        {
            await Controller.I.SendAsync("ToggleFollow", Id);

            switch (fullUserInfo.Friendship)
            {
                case (int)FriendShip.Friend:
                    fullUserInfo.Friendship = (int)FriendShip.Following;
                    Repository.I.PersonalFullInfo.Followings
                        .RemoveAll(i => i.Id == Id);
                    break;
                case (int)FriendShip.Follower:
                    fullUserInfo.Friendship = (int)FriendShip.None;
                    Repository.I.PersonalFullInfo.Followings
                        .RemoveAll(i => i.Id == Id);
                    break;
                case (int)FriendShip.Following:
                    fullUserInfo.Friendship = (int)FriendShip.Friend;
                    Repository.I.PersonalFullInfo.Followings.Add(fullUserInfo);
                    break;
                case (int)FriendShip.None:
                    fullUserInfo.Friendship = (int)FriendShip.Follower;
                    Repository.I.PersonalFullInfo.Followings.Add(fullUserInfo);
                    break;
            }

            UpdateFriendShipView();
        });
    }
    private void UpdateFriendShipView()
    {
        //follower and not friend means you're not following back
        followButton.interactable = fullUserInfo.Friendship == (int)FriendShip.Following ||
                                    fullUserInfo.Friendship == (int)FriendShip.None;
    }
}