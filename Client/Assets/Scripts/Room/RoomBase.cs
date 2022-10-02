using System;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[Rpc]
public class RoomBase : MonoModule<RoomBase>
{
    #region props

    [HideInInspector] public int MyTurn, BetChoice, CapacityChoice;
    public List<FullUserInfo> UserInfos;

    public int Bet => Bets[BetChoice];
    public static int[] Bets { get; } = { 55, 110, 220, 550, 1100, 5500 };
    public static int MinBet => Bets[0];
    //bet

    public int Capacity => Capacities[CapacityChoice];
    public static int[] Capacities { get; } = { 2, 3, 4 };
    //capacity

    public string[] Words;
    public string Text;
    //text


    public Transform Canvas;

    #endregion

    protected override void Awake()
    {
        base.Awake();

        NetManager.I.AddRpcContainer(this);
    }

    private void Start()
    {
        (BetChoice, CapacityChoice, UserInfos, MyTurn, Text)
            = ((int, int, List<FullUserInfo>, int, string))
            Controller.I.TakeTransitionData("roomArgs");
        //take room args from other lobby scene

        Words = Text.Split(' ').Select(w => " " + w).ToArray(); //each word has space before it

        Repository.I.PersonalFullInfo.Money -= Bet;
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

    public event Action Destroyed;

    private void OnDestroy()
    {
        Destroyed?.Invoke();
    }
}