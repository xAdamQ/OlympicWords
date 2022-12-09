using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

[Serializable]
public class Kvp<TKey, TValue>
{
    public TKey Key;
    public TValue Value;
}

public enum PowerUp
{
    SmallJet,
    MegaJet,
    Filler
}

[Rpc]
public abstract class EnvBase : MonoModule<EnvBase>
{
    [SerializeField] protected GameObject
        myPlayerPrefab,
        oppoPlayerPrefab,
        digitPrefab;

    public const int SMALL_JETS_COUNT = 2, MEGA_JETS_COUNT = 1;

    #region props

    [HideInInspector] public int MyTurn, BetChoice, CapacityChoice;
    [HideInInspector] protected List<string> Words;
    public List<FullUserInfo> UserInfos;

    public int Bet => Bets[BetChoice];
    public static int[] Bets { get; } = { 55, 110, 220, 550, 1100, 5500 };
    public static int MinBet => Bets[0];
    //bet

    public int Capacity => Capacities[CapacityChoice];
    public static int[] Capacities { get; } = { 2, 3, 4 };
    //capacity

    public string Text;
    //text

    public Transform Canvas;

    #endregion


    /////////////// SERIALIZED FIELDS
    public Material BaseMaterial;

    [SerializeField] private Mesh[] AlphabetModels;
    [SerializeField] private List<Kvp<char, Mesh>> SpecialModels;

    protected Mesh GetDigitMesh(char digit)
    {
        if (digit is >= 'a' and <= 'z')
            return AlphabetModels[digit - 'a'];

        return SpecialModels.First(c => c.Key == digit).Value;
    }

    public int GetWordLengthAt(int wordIndex) => Words[wordIndex].Length;

    public abstract Vector3 GetCharPozAt(int charIndex);
    public abstract Vector3 GetCharRotAt(int charIndex);
    public abstract GameObject GetCharObjectAt(int charIndex);

    public abstract IEnumerable<GameObject> GetWordObjects(int wordIndex);

    public abstract int WordsCount { get; }

    protected override void Awake()
    {
        base.Awake();
        NetManager.I.AddRpcContainer(this, typeof(EnvBase));
        Initiated?.Invoke();
        //the type is sent manually, otherwise it will be sent with the type of the chile
    }

    public void Surrender()
    {
        UniTask.Create(async () =>
        {
            await MasterHub.I.Surrender();
            SceneManager.LoadScene("Lobby");
        }).Forget(e => throw e);
    }

    [Rpc]
    public void StartRoomRpc()
    {
        Destroy(RoomBaseAdapter.I.PowerUpPanel);

        StartCoroutine(ReadyGo());

        GameStarted += OnGameStarted;
    }

    /// <summary>
    /// each number represents the start index of the word at index
    /// the length of the word is arr[i+1]-arr[i]
    /// </summary>
    public int[] WordMap;

    [Rpc]
    public virtual void PrepareRequestedRoomRpc
    (List<FullUserInfo> userInfos, int myTurn, string text, int randomSeed,
        List<(int index, int player)> fillerWords, List<int> chosenPowerUps)
    {
        BetChoice = RoomRequester.LastRequest.betChoice;
        CapacityChoice = RoomRequester.LastRequest.capacityChoice;
        UserInfos = userInfos;
        MyTurn = myTurn;
        Text = text;

        var trimmedWords = Text.Split(' ');
        Words = trimmedWords.Select(w => ' ' + w).ToList(); //each word has space before it
        Words[0] = Words[0][1..]; //remove initial space

        WordMap = new int[trimmedWords.Length + 1];
        for (var i = 1; i <= Words.Count; i++)
            WordMap[i] = WordMap[i - 1] + Words[i - 1].Length;

        UserInfos.ForEach(info => info.DownloadPicture().Forget(e => throw e));

        Repository.I.PersonalFullInfo.Money -= Bet;

        MakePlayersColorPalette();

        CreatePlayers(fillerWords, chosenPowerUps, userInfos);

        var random = new System.Random(randomSeed);
        GenerateDigits(random);

        ColorFillers(fillerWords);

        SetPlayersStartPoz();

        SetCameraFollow();

        GamePrepared?.Invoke();

        MasterHub.I.Ready();
    }

    private void SetCameraFollow()
    {
        MyPlayer.InstantCameraFollow();
        MyPlayer.CameraFollow();
    }

    private void SetPlayersStartPoz()
    {
        var pozPointer = GetCharPozAt(0);
        var rot = GetCharRotAt(0);
        var charObj = GetCharObjectAt(0);
        for (int i = 0; i < Players.Count; i++)
        {
            Players[i].transform.position = pozPointer;
            Players[i].transform.eulerAngles = rot;

            pozPointer += -charObj.transform.forward * .5f;
        }

        // Players.ForEach(p => p.transform.position = GetCharPozAt(0));
        // Players.ForEach(p => p.transform.eulerAngles = GetCharRotAt(0));
    }

    protected abstract void GenerateDigits(Random random);

    public Material[] PlayerMats;

    private void MakePlayersColorPalette()
    {
        PlayerMats = new Material[Capacity];
        for (var i = 0; i < Capacity; i++)
        {
            distinctColors[i].a = .5f;

            PlayerMats[i] = new Material(BaseMaterial)
            {
                color = distinctColors[i]
            };
        }
    }

    private readonly Color[] distinctColors =
    {
        Color.red,
        Color.blue,
        Color.yellow,
        Color.green,
        Color.black,
        Color.magenta,
        Color.cyan,
        Color.Lerp(Color.red, Color.blue, .5f),
        Color.Lerp(Color.green, Color.blue, .5f),
        Color.Lerp(Color.green, Color.red, .5f),
    };

    private void CreatePlayers(List<(int index, int player)> fillerWords, List<int> chosenPowerUps,
        List<FullUserInfo> fullUserInfos)
    {
        var oppoPlaceCounter = 1;
        //oppo place starts at 1 to 3

        for (var i = 0; i < Capacity; i++)
        {
            var myFillers = fillerWords.Where(w => w.player == i).Select(w => w.index).ToList();
            if (myFillers.Count == 0) myFillers = null;

            if (MyTurn == i)
            {
                MyPlayer = Instantiate(myPlayerPrefab).GetComponent<MyPlayerBase>();
                MyPlayer.Init(MyTurn, this, chosenPowerUps[i], myFillers, fullUserInfos[i].Name);

                Players.Add(MyPlayer);
            }
            else
            {
                var oppo = Instantiate(oppoPlayerPrefab).GetComponent<OppoBase>();
                oppo.Init(oppoPlaceCounter++, this, chosenPowerUps[i], myFillers, fullUserInfos[i].Name);

                Players.Add(oppo);
                Oppos.Add(oppo);
            }
        }
    }

    public List<PlayerBase> Players { get; } = new();
    public List<OppoBase> Oppos { get; } = new();
    public MyPlayerBase MyPlayer { get; set; }

    private void OnGameStarted()
    {
        NetManager.I.StartStreaming();
    }

    private IEnumerator ThreeTwoOne()
    {
        var readyText = RoomBaseAdapter.I.ReadyText;

        readyText.gameObject.SetActive(true);

        for (var i = 3; i >= 1; i--)
        {
            readyText.text = i.ToString();
            readyText.transform.DOScale(0f, .3f).From();
            yield return new WaitForSeconds(1f);
        }

        GameStarted?.Invoke();

        readyText.text = "GO";
        readyText.transform.DOScale(0f, .2f).From();
        Destroy(readyText, .2f);
    }

    private IEnumerator ReadyGo()
    {
        var readyText = RoomBaseAdapter.I.ReadyText;

        readyText.gameObject.SetActive(true);

        readyText.text = "READY";
        readyText.transform.DOScale(0f, .3f).From();
        yield return new WaitForSeconds(1f);

        readyText.text = "GO";
        readyText.transform.DOScale(0f, .3f).From();
        yield return new WaitForSeconds(.5f);

        Destroy(readyText);
        GameStarted?.Invoke();
    }

    private void ColorFillers(List<(int index, int player)> fillerWords)
    {
        foreach (var (index, player) in fillerWords)
        foreach (var wordObject in GetWordObjects(index))
            wordObject.GetComponent<Renderer>().material = PlayerMats[player];
    }

    public event Action GameFinished, GamePrepared, GameStarted;
    public static event Action Initiated;

    public void FinishGame()
    {
        GameFinished?.Invoke();
        Debug.Log("show finish particles here");
    }
}