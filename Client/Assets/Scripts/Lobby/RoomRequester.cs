using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public abstract class RoomRequester : EnvObject
{
    [SerializeField] private ChoiceButton capacityChoiceButton;
    [SerializeField] private TMP_Text betText, ticketText;

    private void Awake()
    {
        var bet = RootEnv.Bets[transform.GetSiblingIndex()];

        betText.text = bet.ToString();
        ticketText.text = (bet / 11f).ToString(CultureInfo.InvariantCulture);
    }

    public void RequestRandomRoom()
    {
        RequestRandomRoomAsync().Forget();
    }

    private async UniTaskVoid RequestRandomRoomAsync()
    {
        var betChoice = RootEnv.GetEnvIndex(GenericEnvType);


#if ADDRESSABLES
        if (betChoice > 0)
        {
            var handle = Addressables.DownloadDependenciesAsync(EnvName);
            await BlockingOperationManager.Start(handle, "the level is downloading");
        }
#endif

        if (Repository.I.PersonalFullInfo.Money < RootEnv.Bets[betChoice])
        {
            Toast.I.Show(Translatable.GetText("no_money"));
            return;
        }

        LastRequest = (GenericEnvName, betChoice);
        await SwitchScene();
    }

    private async UniTask SwitchScene()
    {
        LobbyCoordinator.DestroyModule();

        BlockingPanel.Show("loading");

        await SceneManager.LoadSceneAsync("RoomBase");
        await SceneManager.LoadSceneAsync(GenericEnvName, LoadSceneMode.Additive);

        BlockingPanel.Hide();
    }

    public static (string env, int betChoice) LastRequest;

    public void OpenShop()
    {
        UniTask.Create(async () =>
        {
            var shop = await Addressables.InstantiateAsync(AddressManager.I.GetShop(GenericEnvName),
                GameObject.FindWithTag("WorldCanvas").transform);
            shop.GetComponent<Shop>().Init(GenericEnvName);
        });
    }
}

public abstract class RoomRequester<TEnv> : RoomRequester where TEnv : RootEnv
{
    protected override Type GenericEnvType { get; } = typeof(TEnv);
}