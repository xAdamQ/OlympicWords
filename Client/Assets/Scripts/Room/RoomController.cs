using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Basra.Common;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[Rpc]
public class RoomController : MonoModule<RoomController>
{
    #region props

    [HideInInspector] public int MyTurn, BetChoice, CapacityChoice;
    [HideInInspector] public List<FullUserInfo> UserInfos;

    public int Bet => Bets[BetChoice];
    public static readonly int[] Bets = { 55, 110, 220, 550, 1100, 5500 };
    public static int MinBet => Bets[0];
    public int TotalPrize => Mathf.RoundToInt(Bet / 1.1f) * Capacity;

    public int Capacity => Capacities[CapacityChoice];
    public static readonly int[] Capacities = { 2, 3, 4 };

    public string[] Words;

    public int[] OppoIndicies;

    #endregion


    public int BetMoneyToPay()
    {
        return Bet;
    }

    //[SerializeField] private Gameplay gameplayPrefab, finalizerPrefab;
    public Transform Canvas;
    public string Text;

    protected override void Awake()
    {
        base.Awake();

        NetManager.I.AddRpcContainer(this);
    }

    private void Start()
    {
        var roomArgs = ((int betChoice, int capacityChoice, List<FullUserInfo> userInfos, int myTurn, string text))
            Controller.I.TakeTransitionData("roomArgs");

        BetChoice = roomArgs.betChoice;
        CapacityChoice = roomArgs.capacityChoice;
        UserInfos = roomArgs.userInfos;
        MyTurn = roomArgs.myTurn;
        Text = roomArgs.text;

        Words = Text.Split(' ');

        OppoIndicies = Enumerable.Range(0, Capacity).Where(n => n != MyTurn).ToArray();

        // new RoomUserView.Manager();

        //dependent on RoomSettings
        //this will make registering requires order, so no circular dependencies possible

        // RoomUserView.Manager.I.Init();

        Repository.I.PersonalFullInfo.Money -= BetMoneyToPay();

        NetManager.I.SendAsync("Ready").Forget(e => throw e);
    }


    public void Surrender()
    {
        UniTask.Create(async () =>
        {
            await NetManager.I.SendAsync("Surrender");
            SceneManager.LoadScene("Lobby");
        }).Forget(e => throw e);
    }

    [Rpc]
    public void StartRoomRpc()
    {
        Debug.Log("room should start");

        EnvBase.I.BeginGame();

        NetManager.I.StartStreaming();

        BlockingPanel.Hide();
    }
}