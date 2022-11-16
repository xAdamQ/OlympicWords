// using Basra.Common;
// using Cysharp.Threading.Tasks;
// using TMPro;
// using UnityEngine;
// using UnityEngine.AddressableAssets;
// using UnityEngine.SceneManagement;
//
//
// /// <summary>
// /// this not a preloaded module like core modules
// /// this is loaded and destroyed
// /// referenced by the module group (room)
// /// </summary>
// public class RoomResultPanel : MonoBehaviour
// {
//     [SerializeField] private TMP_Text
//         eatenCards,
//         basras,
//         superBasras,
//         earnedMoney,
//         winStreak,
//         competitionStateText;
//
//
//     public static async UniTaskVoid Instantiate(Transform parent, RoomXpReport roomXpReport,
//         PersonalFullUserInfo personalFullUserInfo,
//         UserRoomStatus userRoomStatus)
//     {
//         var obj = await Addressables.InstantiateAsync("myRoomResultView", parent);
//         obj.GetComponent<RoomResultPanel>()
//             .Construct(roomXpReport, personalFullUserInfo, userRoomStatus);
//     }
//
//     private void Construct(RoomXpReport roomXpReport, PersonalFullUserInfo personalFullUserInfo,
//         UserRoomStatus userRoomStatus)
//     {
//         eatenCards.text = userRoomStatus.EatenCards.ToString();
//         basras.text = userRoomStatus.Basras.ToString();
//         superBasras.text = userRoomStatus.BigBasras.ToString();
//         earnedMoney.text = userRoomStatus.WinMoney.ToString();
//
//         winStreak.text = personalFullUserInfo.WinStreak.ToString();
//
//         if (userRoomStatus.EarnedMoney == 0)
//         {
//             competitionStateText.text = "خسرت";
//             competitionStateText.color = Color.red;
//         }
//         else if (userRoomStatus.EarnedMoney < EnvBase.I.Bet) //because the ticket is taken
//         {
//             competitionStateText.text = "تعادل";
//             competitionStateText.color = Color.grey;
//         }
//         else
//         {
//             competitionStateText.text = "كسبت";
//             competitionStateText.color = Color.green;
//         }
//     }
//
// }

