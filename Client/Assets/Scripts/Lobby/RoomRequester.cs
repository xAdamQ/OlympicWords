using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class RoomRequester : MonoBehaviour
{
    [SerializeField] private ChoiceButton capacityChoiceButton;
    [SerializeField] private TMP_Text betText, ticketText;

    private void Awake()
    {
        var bet = RoomBase.Bets[transform.GetSiblingIndex()];

        betText.text = bet.ToString();
        ticketText.text = (bet / 11f).ToString();
    }

    public async void RequestRandomRoom(int betChoice)
    {
        if (betChoice > 0)
        {
            var handle = Addressables.DownloadDependenciesAsync("Env" + betChoice);
            await BlockingOperationManager.I.Start(handle, "the level is downloading");
        }

        if (Repository.I.PersonalFullInfo.Money < RoomBase.Bets[betChoice])
        {
            Toast.I.Show(Translatable.GetText("no_money"));
            return;
        }

        var operation = MasterHub.I.RequestRandomRoom
            (betChoice, capacityChoiceButton.CurrentChoice);
        await BlockingOperationManager.I.Start(operation);

        LastRequest = (capacityChoiceButton.CurrentChoice, betChoice);

        BlockingPanel.Show("finding players")
            .Forget(e => throw e);
        //this is shown even if the room is started, it's removed before game start directly
    }

    public static (int capacityChoice, int betChoice) LastRequest;
}