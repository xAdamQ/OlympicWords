using System;
using System.Globalization;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(RoomRequesterMapper))]
public abstract class RoomRequester : EnvObject
{
    RoomRequesterMapper mapper;
    private void Awake()
    {
        mapper = GetComponent<RoomRequesterMapper>();
        var bet = RootEnv.Bets[transform.GetSiblingIndex()];

        mapper.betText.text = bet.ToString();
        mapper.ticketText.text = (bet / 11f).ToString(CultureInfo.InvariantCulture);
        mapper.playButton.onClick.AddListener(RequestRandomRoom);
        mapper.shopButton.onClick.AddListener(OpenShop);
    }

    private void RequestRandomRoom()
    {
        RequestRandomRoomAsync().Forget();
    }
    private void OpenShop()
    {
        UniTask.Create(async () =>
        {
            var shop = await Addressables.InstantiateAsync(AddressManager.I.GetShop(GenericEnvName),
                GameObject.FindWithTag("WorldCanvas").transform);
            shop.GetComponent<Shop>().Init(GenericEnvName);
        }).Forget(e => throw e);
    }

    private async UniTaskVoid RequestRandomRoomAsync()
    {
        var betChoice = RootEnv.GetEnvIndex(GenericEnvType);


#if ADDRESSABLES
        //I don't load the scene async directly because I don't want the player to ask for a room until his scene is ready
        var handle = Addressables.DownloadDependenciesAsync(GenericEnvName);
        Debug.Log("generic env name is: " + GenericEnvName);
        await BlockingOperationManager.Start(handle, "downloading level (first time only)");
        Debug.Log("level downloaded");
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

        await Addressables.LoadSceneAsync("RoomBase");
        await Addressables.DownloadDependenciesAsync(GenericEnvName);
        await Addressables.LoadSceneAsync(GenericEnvName, LoadSceneMode.Additive);
        // await SceneManager.LoadSceneAsync(GenericEnvName, LoadSceneMode.Additive);

        BlockingPanel.Hide();
    }

    public static (string env, int betChoice) LastRequest;
}

public abstract class RoomRequester<TEnv> : RoomRequester where TEnv : RootEnv
{
    protected override Type GenericEnvType { get; } = typeof(TEnv);
}