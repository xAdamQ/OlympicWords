using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

/// <summary>
/// it's dependent on player
/// </summary>
public class RoomUserView : MinUserView
{
    [SerializeField] private Image turnFillImage, turnFocusOutline;

    private static Color
        TurnFillStartColor = new Color(1, .815f, 0),
        TurnFillEndColor = new Color(1, 0, 0);

    public void TurnFocus(bool getFocus)
    {
        // turnFocusOutline.gameObject.SetActive(getFocus);
    }

    public void SetTurnFill(float progress)
    {
        turnFillImage.fillAmount = progress;
        turnFillImage.color = Color.Lerp(TurnFillEndColor, TurnFillStartColor, progress);
    }

    public override void ShowFullInfo()
    {
        if (Id == Repository.I.PersonalFullInfo.Id)
            FullUserView.Show(Repository.I.PersonalFullInfo);
        else
        {
            var oppoFullInfo = RoomController.I.UserInfos.FirstOrDefault(_ => _.Id == Id);
            FullUserView.Show(oppoFullInfo);
        }
    }

    public class Manager : Singleton<Manager>
    {
        private async UniTask<RoomUserView> Create(int place, MinUserInfo minUserInfo)
        {
            var view =
                (await Addressables.InstantiateAsync($"roomUserView{place}",
                    RoomController.I.Canvas)).GetComponent<RoomUserView>();

            view.Init(minUserInfo);

            return view;
        }

        public List<RoomUserView> RoomUserViews { get; set; }

        public async void Init()
        {
            RoomUserViews = new List<RoomUserView>();

            var oppoPlaceCounter = 1;

            for (int i = 0; i < RoomController.I.UserInfos.Count; i++)
            {
                var placeIndex = i == RoomController.I.MyTurn ? 0 : oppoPlaceCounter++;

                RoomUserViews.Add(await Create(placeIndex, RoomController.I.UserInfos[i]));
            }
        }
    }
}