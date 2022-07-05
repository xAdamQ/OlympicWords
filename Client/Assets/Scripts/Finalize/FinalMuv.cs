using Basra.Common;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class FinalMuv : MonoBehaviour
{
    private string Id;

    private PlayerBase player;

    [SerializeField] private Image picture;

    [SerializeField] private TMP_Text
        earnedMoneyText,
        WpmText,
        scoreText,
        finalPositionText;

    [SerializeField] private Button followButton;

    private FullUserInfo fullUserInfo;

    public static async UniTask<FinalMuv> Create(FullUserInfo fullUserInfo, PlayerBase player, Transform parent)
    {
        var asset = await Addressables.InstantiateAsync("finalMuv", parent);
        var finalMuv = asset.GetComponent<FinalMuv>();
        finalMuv.player = player;
        finalMuv.Init(fullUserInfo);
        return finalMuv;
    }

    private void Init(FullUserInfo fullUserInfo)
    {
        this.fullUserInfo = fullUserInfo;

        Id = fullUserInfo.Id;

        UpdateFriendshipView();

        if (fullUserInfo.IsPictureLoaded)
            SetPicture(fullUserInfo.Picture);
        else
            fullUserInfo.PictureLoaded += pic => SetPicture(pic);
    }

    public bool Finished;

    public void SetFinal(UserRoomStatus userRoomStatus)
    {
        Finished = true;

        earnedMoneyText.text = userRoomStatus.EarnedMoney.ToString();
        WpmText.text = userRoomStatus.Wpm.ToString();
        scoreText.text = userRoomStatus.Score.ToString();
        finalPositionText.text = userRoomStatus.FinalPosition.ToString();
    }

    public void SetTemporalStatus()
    {
        Debug.Log("set temp");

        player.OnMovingDigit += UpdateWpm;
    }

    private void UpdateWpm()
    {
        var timeInterval = (Time.time - player.StartTime) / 60f;
        WpmText.text = (player.CurrentWordIndex / timeInterval).ToString("f2");
        Debug.Log("updating wpm");
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
            await NetManager.I.SendAsync("ToggleFollow", Id);

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

            UpdateFriendshipView();
        });
    }

    public void SetWpm(float wpm)
    {
        WpmText.text = wpm.ToString("F1");
    }

    public void SetFinished(int position)
    {
        transform.parent.SetSiblingIndex(position);
    }

    private void UpdateFriendshipView()
    {
        //follower and not friend means you're not following back
        followButton.interactable = fullUserInfo.Friendship == (int)FriendShip.Following ||
                                    fullUserInfo.Friendship == (int)FriendShip.None;
    }
}