using Common.Lobby;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class FullUserView : MinUserView
{
    [SerializeField] private TMP_Text
        moneyText,
        playedRoomsText,
        wonRoomsText,
        eatenCardsText,
        winStreakText,
        maxWinStreakText,
        basrasText,
        bigBasrasText,
        winRatioText,
        totalEarnedMoney,
        followingBackText,
        followButtonText,
        openMatchesText;

    [SerializeField] private GameObject challengeButton;

    private static FullUserView activeInstance;
    public FullUserInfo FullUserInfo;

    [SerializeField] private Image openMatchesCheck;

    public static void Show(FullUserInfo fullUserInfo)
    {
        var fuvPrefab = fullUserInfo is PersonalFullUserInfo
            ? Coordinator.I.References.PersonalFuvPrefab
            : Coordinator.I.References.FuvPrefab;

        if (!activeInstance)
        {
            activeInstance = Instantiate(fuvPrefab, Coordinator.I.canvas).GetComponent<FullUserView>();
        }
        else if (activeInstance.FullUserInfo.GetType() != fullUserInfo.GetType())
        {
            activeInstance.Destroy();
            activeInstance = Instantiate(fuvPrefab, Coordinator.I.canvas).GetComponent<FullUserView>();
        }

        activeInstance.Init(fullUserInfo);
    }

    private void UpdateFriendShipView()
    {
        followingBackText.gameObject.SetActive(false);

        if (FullUserInfo.Friendship is (int)FriendShip.Follower or (int)FriendShip.Friend)
            followingBackText.gameObject.SetActive(true);

        followButtonText.text = FullUserInfo.Friendship is (int)FriendShip.Follower or (int)FriendShip.None
            ? "Follow"
            : "Unfollow";
    }

    private void Init(FullUserInfo fullUserInfo)
    {
        this.FullUserInfo = fullUserInfo;

        if (fullUserInfo is PersonalFullUserInfo)
        {
            UpdateOpenMatchesView();
        }
        else
        {
            UpdateFriendShipView();
            challengeButton.GetComponent<Button>().interactable = fullUserInfo
                .EnableOpenMatches && EnvBase.I == null;
        }

        base.Init(fullUserInfo);

        moneyText.text = fullUserInfo.Money.ToString();
        playedRoomsText.text = fullUserInfo.PlayedRoomsCount.ToString();
        wonRoomsText.text = fullUserInfo.WonRoomsCount.ToString();
        winStreakText.text = fullUserInfo.WinStreak.ToString();
        maxWinStreakText.text = fullUserInfo.MaxWinStreak.ToString();
        winRatioText.text = fullUserInfo.WinRatio.ToString("p2");
        totalEarnedMoney.text = fullUserInfo.TotalEarnedMoney.ToString();

        gameObject.SetActive(true);
    }

    public void Challenge()
    {
        if (Repository.I.PersonalFullInfo.Money < EnvBase.MinBet)
            Toast.I.Show(Translatable.GetText("no_money"));

        UniTask.Create(async () =>
        {
            var res = await Controllers.Lobby.RequestMatch(Id);

            if (res == MatchRequestResult.Available)
                BlockingPanel.Show("a challenge request is sent to the player",
                    () => Controllers.Lobby.CancelChallengeRequest(Id));
            else
                Toast.I.Show(res.ToString());
        });
    }

    public void ToggleFollow()
    {
        UniTask.Create(async () =>
        {
            await Controllers.User.ToggleFollow(Id);

            Debug.Log("was:");
            Debug.Log(FullUserInfo.Friendship.ToString());

            switch (FullUserInfo.Friendship)
            {
                case (int)FriendShip.Friend:
                    FullUserInfo.Friendship = (int)FriendShip.Following;
                    Repository.I.PersonalFullInfo.Followings
                        .RemoveAll(i => i.Id == Id);
                    break;
                case (int)FriendShip.Following:
                    FullUserInfo.Friendship = (int)FriendShip.None;
                    Repository.I.PersonalFullInfo.Followings
                        .RemoveAll(i => i.Id == Id);
                    break;
                case (int)FriendShip.Follower:
                    FullUserInfo.Friendship = (int)FriendShip.Friend;
                    Repository.I.PersonalFullInfo.Followings.Add(FullUserInfo);
                    break;
                case (int)FriendShip.None:
                    FullUserInfo.Friendship = (int)FriendShip.Following;
                    Repository.I.PersonalFullInfo.Followings.Add(FullUserInfo);
                    break;
            }

            Debug.Log(FullUserInfo.Friendship.ToString());

            UpdateFriendShipView();
        });
    }

    private void UpdateOpenMatchesView()
    {
        if (FullUserInfo.EnableOpenMatches)
        {
            openMatchesCheck.gameObject.SetActive(true);
            openMatchesText.text = "يمكن لاي شحص ان بتحداك للعب";
        }
        else
        {
            openMatchesCheck.gameObject.SetActive(false);
            openMatchesText.text = "يمكن للاصدقاء فقط تحديك للعب";
        }
    }

    public void ToggleOpenMatches()
    {
        UniTask.Create(async () =>
        {
            await Controllers.User.ToggleOpenMatches();
            FullUserInfo.EnableOpenMatches = !FullUserInfo.EnableOpenMatches;
            UpdateOpenMatchesView();
        });
    }

    public void Destroy()
    {
        Object.Destroy(gameObject);
    }
}