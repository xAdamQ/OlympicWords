using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class RoomRequester : MonoBehaviour
{
    [SerializeField] private ChoiceButton capacityChoiceButton;
    [SerializeField] private TMP_Text betText, ticketText;

    private void Awake()
    {
        var bet = EnvBase.Bets[transform.GetSiblingIndex()];

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

        if (Repository.I.PersonalFullInfo.Money < EnvBase.Bets[betChoice])
        {
            Toast.I.Show(Translatable.GetText("no_money"));
            return;
        }

        UniTask.Create(async () =>
        {
            await NetManager.I.StartRandomRoom(betChoice, capacityChoiceButton.CurrentChoice);

            LastRequest = (capacityChoiceButton.CurrentChoice, betChoice);

            await SwitchScene(betChoice);
        });

        // BlockingPanel.Show("finding players")
        //     .Forget(e => throw e);
        //this is shown even if the room is started, it's removed before game start directly
    }

    private async UniTask SwitchScene(int betChoice)
    {
        LobbyController.DestroyModule();

        BlockingPanel.Show("loading");
        await SceneManager.LoadSceneAsync("RoomBase");
        var envName = "Env" + betChoice;
        await SceneManager.LoadSceneAsync(envName, LoadSceneMode.Additive);
        BlockingPanel.Hide();
    }

    public static (int capacityChoice, int betChoice) LastRequest;
}