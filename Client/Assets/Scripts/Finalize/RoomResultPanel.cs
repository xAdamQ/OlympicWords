using Basra.Common;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;


/// <summary>
/// this not a preloaded module like core modules
/// this is loaded and destroyed
/// referenced by the module group (room)
/// </summary>
public class RoomResultPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text
        competetionScoreText,
        basraScoreText,
        bigBasraScoreText,
        greatEatScoreText,
        eatenCards,
        basras,
        superBasras,
        winRatioChange,
        earnedMoney,
        winStreak,
        competitionStateText;


    public static async UniTaskVoid Instantiate(Transform parent, RoomXpReport roomXpReport,
        PersonalFullUserInfo personalFullUserInfo,
        UserRoomStatus userRoomStatus)
    {
        var obj = await Addressables.InstantiateAsync("myRoomResultView", parent);
        obj.GetComponent<RoomResultPanel>()
            .Construct(roomXpReport, personalFullUserInfo, userRoomStatus);
    }

    private void Construct(RoomXpReport roomXpReport, PersonalFullUserInfo personalFullUserInfo,
        UserRoomStatus userRoomStatus)
    {
        if (roomXpReport.Competition == 0)
            competetionScoreText.transform.parent.gameObject.SetActive(false);
        if (roomXpReport.Basra == 0) basraScoreText.transform.parent.gameObject.SetActive(false);
        if (roomXpReport.BigBasra == 0)
            bigBasraScoreText.transform.parent.gameObject.SetActive(false);
        if (roomXpReport.GreatEat == 0)
            greatEatScoreText.transform.parent.gameObject.SetActive(false);

        competetionScoreText.text = $"+{roomXpReport.Competition}xp";
        basraScoreText.text = $"+{roomXpReport.Basra}xp";
        bigBasraScoreText.text = $"+{roomXpReport.BigBasra}xp";
        greatEatScoreText.text = $"+{roomXpReport.GreatEat}xp";

        eatenCards.text = userRoomStatus.EatenCards.ToString();
        basras.text = userRoomStatus.Basras.ToString();
        superBasras.text = userRoomStatus.BigBasras.ToString();
        earnedMoney.text = userRoomStatus.WinMoney.ToString();

        winStreak.text = personalFullUserInfo.WinStreak.ToString();

        if (userRoomStatus.WinMoney == 0)
        {
            competitionStateText.text = "خسرت";
            competitionStateText.color = Color.red;
        }
        else if (userRoomStatus.WinMoney < RoomController.I.Bet) //because the ticket is taken
        {
            competitionStateText.text = "تعادل";
            competitionStateText.color = Color.grey;
        }
        else
        {
            competitionStateText.text = "كسبت";
            competitionStateText.color = Color.green;
        }

        //eatenCards.text = (personalFullUserInfo.EatenCardsCount - oldInfo.EatenCardsCount).ToString();
        //basras.text = (personalFullUserInfo.BasraCount - oldInfo.BasraCount).ToString();
        //superBasras.text = (personalFullUserInfo.BigBasraCount - oldInfo.BigBasraCount).ToString();
        //winRatioChange.text = (personalFullUserInfo.WinRatio - oldInfo.WinRatio).ToString("p2");


        //know win or loose by this
        //var moneyChange = (personalFullUserInfo.Money - oldInfo.Money);

        //var bet = RoomController.Bets[betChoice];

        //if (moneyChange < 0) earnedMoney.color = Color.red;
        //else earnedMoney.text = ((bet - (bet * .1f)) * 2).ToString("f0");
    }

    /// <summary>
    /// uses roomController, lobbyFac
    /// </summary>
    public void ToLobby()
    {
        RoomController.I.DestroyModuleGroup();
        new LobbyController();
    }
}