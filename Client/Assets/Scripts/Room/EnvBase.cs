using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class EnvBase : MonoBehaviour
{
    public static EnvBase I;

    [SerializeField] protected GameObject
        myPlayerPrefab,
        oppoPlayerPrefab,
        endPlanPrefab,
        digitPrefab;

    /////////////// SERIALIZED FIELDS
    [HideInInspector] public List<string> words;
    protected int capacity;
    [SerializeField] private Material BaseMaterial;

    [SerializeField] private Mesh[] DigitModels;

    public char GetDigitAt(int wordIndex, int digitIndex)
    {
        var word = words[wordIndex];
        return digitIndex >= word.Length ? ' ' : word[digitIndex];
    }

    public abstract Vector3 GetDigitPozAt(int wordIndex, int digitIndex);
    public abstract Vector3 GetDigitRotAt(int wordIndex, int digitIndex);

    protected virtual void Awake()
    {
        I = this;
    }

    protected virtual void Start()
    {
        NetManager.I.AddRpcContainer(this);

        capacity = RoomController.I.Capacity;
        words = RoomController.I.Words.ToList();

        MakePlayersColorPalette();

        CreatePlayers();
    }

    protected abstract void GenerateDigitsModels(string word, Stair stair);

    public Material[] PlayerMats;

    private void MakePlayersColorPalette()
    {
        PlayerMats = new Material[capacity];
        for (var i = 0; i < capacity; i++)
        {
            distinctColors[i].a = .5f;

            PlayerMats[i] = new Material(BaseMaterial)
            {
                color = distinctColors[i]
            };
        }
    }

    private Color[] distinctColors =
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

    private void CreatePlayers()
    {
        var oppoPlaceCounter = 1;
        //oppo place starts at 1 to 3

        for (var i = 0; i < RoomController.I.Capacity; i++)
        {
            if (RoomController.I.MyTurn == i)
            {
                MyPlayer = Instantiate(myPlayerPrefab).GetComponent<MyPlayerBase>();
                MyPlayer.Init(RoomController.I.MyTurn);

                Players.Add(MyPlayer);

                var cameraFollow = Camera.main!.GetComponent<CameraFollow>();
                cameraFollow.target = MyPlayer.transform;
                cameraFollow.InstantFollow();
            }
            else
            {
                var oppo = Instantiate(oppoPlayerPrefab).GetComponent<OppoBase>();
                oppo.Init(oppoPlaceCounter++);

                Players.Add(oppo);
                Oppos.Add(oppo);
            }
        }
    }

    public List<PlayerBase> Players { get; } = new();
    public List<OppoBase> Oppos { get; } = new();
    public MyPlayerBase MyPlayer { get; set; }

    public void BeginGame()
    {
    }

    public event Action OnGameFinished;

    public void FinishGame()
    {
        OnGameFinished?.Invoke();
        Debug.Log("show finish particles here");
    }
}