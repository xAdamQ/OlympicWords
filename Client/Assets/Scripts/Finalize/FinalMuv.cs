using Basra.Common;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class FinalMuv : MinUserView
{
    private PlayerBase player;

    [SerializeField] protected TMP_Text
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
        finalMuv.Init((MinUserInfo)fullUserInfo);
        finalMuv.Init(fullUserInfo);
        return finalMuv;
    }

    private void Init(FullUserInfo fullUserInfo)
    {
        this.fullUserInfo = fullUserInfo;
        UpdateFriendshipView();
    }

    public bool Finished;

    public void SetFinal(UserRoomStatus userRoomStatus)
    {
        Finished = true;

        earnedMoneyText.text = userRoomStatus.EarnedMoney.ToString();
        WpmText.text = userRoomStatus.Wpm.ToString("f2");
        scoreText.text = userRoomStatus.Score.ToString();
        finalPositionText.text = userRoomStatus.FinalPosition.ToString();
    }

    public void SetTemporalStatus()
    {
        player.MovedADigit += UpdateWpm;
    }


    private void UpdateWpm()
    {
        var timeInterval = (Time.time - player.startTime) / 60f;
        WpmText.text = (player.WordIndex / timeInterval).ToString("f2");
        Debug.Log("updating wpm");
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
            await MasterHub.I.ToggleFollow(Id);

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
        followButton.interactable = fullUserInfo.Friendship is (int)FriendShip.Follower or (int)FriendShip.None;
    }
}