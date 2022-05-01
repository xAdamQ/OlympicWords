using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Basra.Common;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

[Rpc]
public class RoomController : MonoModule<RoomController>
{
    public void Init(int betChoice, int capacityChoice, List<FullUserInfo> userInfos, int myTurn)
    {
        BetChoice = betChoice;
        CapacityChoice = capacityChoice;
        UserInfos = userInfos;
        MyTurn = myTurn;

        Canvas = Instantiate(Controller.I.canvasPrefab, transform).GetComponent<Transform>();

        Instantiate(gameplayPrefab, transform);

        PrizeView.Create();

        new RoomUserView.Manager();

        //dependent on RoomSettings
        //this will make registering requires order, so no circular dependencies possible

        RoomUserView.Manager.I.Init();

        Gameplay.I.CreatePlayers();

        Repository.I.PersonalFullInfo.Money -= BetMoneyToPay();

        Background.I.SetForRoom(UserInfos);

        Controller.I.SendAsync("Ready").Forget(e => throw e);
    }

    public int MyTurn;
    public List<FullUserInfo> UserInfos;
    public int BetChoice;
    public int CapacityChoice;

    public int Bet => Bets[BetChoice];
    public static int[] Bets => new[] {55, 110, 220, 550, 1100, 5500};

    public int Capacity => Capacities[CapacityChoice];
    public static readonly int[] Capacities = {2, 3, 4};

    public static int MinBet => Bets[0];

    public int TotalPrize => Mathf.RoundToInt(Bet / 1.1f) * Capacity;

    public int BetMoneyToPay()
    {
        return Bet;
    }

    [SerializeField] private Gameplay gameplayPrefab, finalizerPrefab;
    public Transform Canvas;

    protected override void Awake()
    {
        base.Awake();

        Controller.I.AddRpcContainer(this);
    }

    private void Start()
    {
    }

    [Rpc]
    public void FinalizeRoom(FinalizeResult finalizeResult)
    {
        UniTask.Create(async () =>
        {
            //wait for the last throw operation, this can be done better
            await UniTask.Delay(1200);

            RoomUserView.Manager.I.RoomUserViews.ForEach(ruv => Object.Destroy(ruv.gameObject));
            Object.FindObjectsOfType<PlayerBase>().ForEach(obj => Object.Destroy(obj.gameObject));
            Object.Destroy(PrizeView.I.gameObject);

            //immmm this will cause issues on the running funs like decreaseMoneyAimTime and events
            //change indie values instead of rewrite the whole object
            finalizeResult.PersonalFullUserInfo.Followers =
                Repository.I.PersonalFullInfo.Followers;
            finalizeResult.PersonalFullUserInfo.Followings =
                Repository.I.PersonalFullInfo.Followings;

            Repository.I.PersonalFullInfo = finalizeResult.PersonalFullUserInfo;
            Repository.I.PersonalFullInfo.DecreaseMoneyAimTimeLeft().Forget();
            //todo you MUST edit each value on it's own now?, this is about replacing the whole
            //data object, but it seems fine

            Instantiate(finalizerPrefab).GetComponent<Finalizer>().Init(finalizeResult);
        });
    }
    //this function breaks my controller pattern because it has functionality 

    [Rpc]
    public void StartRoomRpc()
    {
        Gameplay.I.BeginGame();

        BlockingPanel.Hide();
    }

    public void DestroyModuleGroup()
    {
        //killing non mb
        Gameplay.I = null;
        RoomUserView.Manager.I = null;

        I = null;

        Destroy(gameObject);
    }
}