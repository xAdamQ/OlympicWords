using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Random = System.Random;

/*
 graph
 graph jump
 graph jump city
 */

[UsedImplicitly]
public class RoomPrepareResponse
{
    public List<FullUserInfo> TurnSortedUsersInfo { get; set; }
    public List<string> SelectedItemPlayers { get; set; }
    public int TurnIndex { get; set; }
    public string Text { get; set; }
    public int Seed { get; set; }
    public List<(int index, int player)> FillerWords { get; set; }
    public List<int> ChosenPowerUps { get; set; }
}

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

public abstract class EnvObject : MonoBehaviour
{
    protected abstract Type GenericEnvType { get; }
    protected string GenericEnvName => RootEnv.EnvIds[GenericEnvType];
}

[Rpc]
public abstract class RootEnv : MonoBehaviour
{
    public static RootEnv I;

    //env specific
    [SerializeField] protected GameObject digitPrefab;

    protected const float SPACE_DISTANCE = .5f;

    protected List<string> Words;
    public int WordsCount => Words.Count;

    public static readonly Type[] OrderedEnvs =
    {
        typeof(GraphJumpCityEnv),
    };

    public static int GetEnvIndex(Type envType)
    {
        return Array.IndexOf(OrderedEnvs, envType);
    }

    public static IEnumerable<Type> GetAllTypes()
    {
        return Assembly.GetAssembly(typeof(RootEnv)).GetTypes()
            .Where(t => t == typeof(RootEnv) || t.IsSubclassOf(typeof(RootEnv)));
    }

    public static string Name => EnvIds[typeof(RootEnv)];

    public static Dictionary<Type, string> EnvIds { get; } =
        new(GetAllTypes().ToDictionary(t => t, GetAbstractName));
    public static Dictionary<string, Type> EnvTypes { get; } =
        new(GetAllTypes().ToDictionary(GetAbstractName, t => t));

    public static string GetAbstractName(Type envType)
    {
        var envIndex = envType.ToString().IndexOf("Env", StringComparison.Ordinal);
        return envType.ToString()[..envIndex];
    }

    public static List<ClientEnvironment> GetEnvironments()
    {
        var allEnvTypes = GetAllTypes();

        Environments = new Dictionary<string, ClientEnvironment>();
        getOrCreateEnv(EnvIds[typeof(RootEnv)]);

        foreach (var envType in allEnvTypes)
        {
            if (envType == typeof(RootEnv))
                continue;

            if (envType.BaseType == null)
                throw new Exception("BaseType cannot be null because we inherit from EnvBase at least");

            var child = getOrCreateEnv(EnvIds[envType]);
            var parent = getOrCreateEnv(EnvIds[envType.BaseType]);

            parent.Children.Add(child);
            child.Parent = parent;

            // if (incudeItems)
            //     await Addressables.LoadAssetsAsync<GameObject>(child.Name, o =>
            //     {
            //         if (o.TryGetComponent(typeof(global::ItemPlayer), out var i) && !o.name.ToLower().Contains("base"))
            //             envs[envType].Items.Add((global::ItemPlayer)i);
            //     });
        }

        return Environments.Values.ToList();

        ClientEnvironment getOrCreateEnv(string envName)
        {
            if (Environments.TryGetValue(envName, out var env)) return env;

            env = new()
            {
                Name = envName,
            };
            Environments.Add(envName, env);

            return env;
        }
    }
    //
    // public static async UniTask HierarchicalEnvLoop(Action<ClientEnvironment> action)
    // {
    //     var envs = await GetEnvironments();
    //     var envQueue = new Queue<ClientEnvironment>();
    //     envQueue.Enqueue(envs.Single(e => e.Name == "Base"));
    //     var visited = new List<ClientEnvironment>();
    //
    //     while (envQueue.Count > 0)
    //     {
    //         var env = envQueue.Dequeue();
    //         visited.Add(env);
    //
    //         foreach (var child in env.Children.Where(e => !visited.Contains(e)))
    //             envQueue.Enqueue(child);
    //
    //         action(env);
    //     }
    // }

    /// <summary>
    /// each number represents the start index of the word at index
    /// the length of the word is arr[i+1]-arr[i]
    /// </summary>
    [HideInInspector] public int[] WordMap;
    private void CreateWordArray()
    {
        var trimmedWords = Text.Split(' ');
        Words = trimmedWords.Select(w => ' ' + w).ToList(); //each word has space before it
        Words[0] = Words[0][1..]; //remove initial space

        WordMap = new int[trimmedWords.Length + 1];
        for (var i = 1; i <= Words.Count; i++)
            WordMap[i] = WordMap[i - 1] + Words[i - 1].Length;
    }

    public const int SMALL_JETS_COUNT = 2, MEGA_JETS_COUNT = 1;

    #region props
    [HideInInspector] public int MyTurn, BetChoice;
    [HideInInspector] public string Env;
    public List<FullUserInfo> UserInfos;

    private int Bet => Bets[BetChoice];
    public static int[] Bets { get; } = { 55, 110, 220, 550, 1100, 5500 };

    public static int MinBet => Bets[0];
    //bet

    public int Capacity => 4;

    [HideInInspector] public string Text;
    //text

    public Transform Canvas;
    #endregion

    /////////////// SERIALIZED FIELDS
    public Material BaseMaterial;


    public abstract Vector3 GetCharPozAt(int charIndex, int playerIndex);
    public abstract Vector3 GetCharRotAt(int charIndex, int playerIndex);
    public abstract GameObject GetCharObjectAt(int charIndex, int playerIndex);
    public abstract IEnumerable<GameObject> GetWordObjects(int wordIndex, int playerIndex);

    protected virtual void Awake()
    {
        GetEnvironments();

        I = this;

        RoomNet.I.AddRpcContainer(this, typeof(RootEnv));
        //the type is sent manually, otherwise it will be sent with the type of the child

        RoomBaseAdapter.I.PowerUpPanel.SetActive(false);
        RoomBaseAdapter.I.WaitingPanel.SetActive(true);

        RoomNet.I.Connected += () => RoomBaseAdapter.I.PowerUpPanel.SetActive(true);

        Initiated?.Invoke();
    }

    public void Surrender()
    {
        UniTask.Create(async () =>
        {
            await RoomNet.I.Surrender();
            SceneManager.LoadScene("Lobby");
        }).Forget(e => throw e);
    }

    [Rpc]
    public void StartRoomRpc()
    {
        StartCoroutine(ReadyGo());

        GameStarted += OnGameStarted;
    }
    [Rpc]
    public virtual void PrepareRequestedRoomRpc(RoomPrepareResponse response)
    {
        BetChoice = RoomRequester.LastRequest.betChoice;
        Env = RoomRequester.LastRequest.env;
        UserInfos = response.TurnSortedUsersInfo;
        MyTurn = response.TurnIndex;
        Text = response.Text;

        Destroy(RoomBaseAdapter.I.PowerUpPanel);
        Destroy(RoomBaseAdapter.I.WaitingPanel);

        UserInfos.ForEach(info => info.DownloadPicture().Forget(e => throw e));

        Repository.I.PersonalFullInfo.Money -= Bet;

        MakePlayersColorPalette();

        var random = new Random(response.Seed);

        CreateWordArray();

        UniTask.Create(async () =>
        {
            await CreatePlayers(response.FillerWords, response.ChosenPowerUps, response.TurnSortedUsersInfo,
                response.SelectedItemPlayers);

            GenerateDigits(random);

            ColorFillers(response.FillerWords);

            SetPlayersInitialPoz();

            GamePrepared?.Invoke();

            await RoomNet.I.Ready();
        });
    }

    protected abstract void SetPlayersInitialPoz();

    protected abstract void GenerateDigits(Random random);

    [HideInInspector] public Material[] PlayerMats;

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
    public static Dictionary<string, ClientEnvironment> Environments;

    private async UniTask CreatePlayers(List<(int index, int player)> fillerWords, List<int> chosenPowerUps,
        List<FullUserInfo> fullUserInfos, List<string> selectedItemPlayers)
    {
        for (var i = 0; i < Capacity; i++)
        {
            var myFillers = fillerWords.Where(w => w.player == i).Select(w => w.index).ToList();
            if (myFillers.Count == 0) myFillers = null;

            var go = await Addressables.InstantiateAsync(AddressManager.I.GetPlayerLocation(selectedItemPlayers[i]));
            var player = go.GetComponent<Player>();

            player.Init(i, chosenPowerUps[i], myFillers, fullUserInfos[i].Name);
            Players.Add(player);

            if (MyTurn == i)
            {
                var controller = (PlayerController)player.gameObject.AddComponent(GetControllerType());
                controller.SetCameraFollow();
            }
        }
    }

    public abstract Type GetControllerType();

    public List<Player> Players { get; } = new();

    private void OnGameStarted()
    {
        RoomNet.I.StartStreaming();
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

    protected abstract void ColorFillers(List<(int index, int player)> fillerWords);


    public event Action GameFinished, GamePrepared, GameStarted;
    public static event Action Initiated;

    public void FinishGame()
    {
        GameFinished?.Invoke();
        Debug.Log("show finish particles here");
    }
}