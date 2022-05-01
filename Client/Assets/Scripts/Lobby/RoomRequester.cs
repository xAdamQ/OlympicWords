using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class RoomRequester : MonoBehaviour
{
    public static async UniTask Create()
    {
        await Addressables.InstantiateAsync("roomRequester", LobbyController.I.Canvas);
    }

    [SerializeField] private ChoiceButton capacityChoiceButton;
    [SerializeField] private TMP_Text betText, ticketText;

    private void Awake()
    {
        var bet = RoomController.Bets[transform.GetSiblingIndex()];

        betText.text = bet.ToString();
        ticketText.text = (bet / 11f).ToString();
    }

    public async void RequestRandomRoom(int betChoice)
    {
        if (Repository.I.PersonalFullInfo.Money < RoomController.Bets[betChoice])
        {
            Toast.I.Show(Translatable.GetText("no_money"));
            return;
        }

        await BlockingOperationManager.I.Start(
            Controller.I.RequestRandomRoom(betChoice, capacityChoiceButton.CurrentChoice));

        BlockingPanel.Show("finding players")
            .Forget(e => throw e);
        //this is shown even if the room is started, it's removed before game start directly
    }
}