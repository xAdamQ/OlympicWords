using Basra.Common;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FinalMuv : MinUserView
{
    private Player player;

    [SerializeField] protected TMP_Text
        earnedMoneyText,
        WpmText,
        scoreText,
        finalPositionText;

    [SerializeField] private Button followButton;
    [SerializeField] private GameObject myPlayerSign;

    private FullUserInfo fullUserInfo;

    public static FinalMuv Create(FullUserInfo fullUserInfo, Player player, Transform parent)
    {
        var finalMuv = Instantiate(Finalizer.I.FinalMuvPrefab, parent).GetComponent<FinalMuv>();
        finalMuv.Init(fullUserInfo, player);
        return finalMuv;
    }

    private void Init(FullUserInfo fullUserInfo, Player player)
    {
        this.player = player;
        this.fullUserInfo = fullUserInfo;

        myPlayerSign.SetActive(player.IsMine);

        base.Init(fullUserInfo);
        UpdateFriendshipView();
    }

    public bool Finished;

    public void SetFinal(UserRoomStatus userRoomStatus)
    {
        Finished = true;

        earnedMoneyText.text = userRoomStatus.EarnedMoney.ToString();
        WpmText.text = userRoomStatus.Wpm.ToString("f2");
        scoreText.text = userRoomStatus.Score.ToString();
        finalPositionText.text = (userRoomStatus.FinalPosition + 1).ToString();
        transform.parent.SetSiblingIndex(userRoomStatus.FinalPosition);
    }

    public void SetTemporalStatus()
    {
        player.LetterDone += _ => UpdateWpm();
    }


    private void UpdateWpm()
    {
        var timeInterval = (Time.time - player.StartTime) / 60f;
        WpmText.text = (player.WordIndex / timeInterval).ToString("f2");
    }

    public override void ShowFullInfo()
    {
        followButton.interactable = false;

        var fullInfo = Id == Repository.I.PersonalFullInfo.Id
            ? Repository.I.PersonalFullInfo
            : fullUserInfo;

        FullUserView.Show(fullInfo);
    }

    public void ToggleFollow()
    {
        UniTask.Create(async () =>
        {
            await Controllers.User.ToggleFollow(Id);

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

    private void UpdateFriendshipView()
    {
        //follower and not friend means you're not following back
        followButton.interactable = fullUserInfo.Friendship is (int)FriendShip.Follower or (int)FriendShip.None;
    }
}