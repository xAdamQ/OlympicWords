using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Kvp<TKey, TValue>
{
    public TKey Key;
    public TValue Value;
}

public abstract class EnvBase : MonoModule<EnvBase>
{
    [SerializeField] protected GameObject
        myPlayerPrefab,
        oppoPlayerPrefab,
        digitPrefab;


    /////////////// SERIALIZED FIELDS
    protected List<string> words;
    protected int capacity;
    public Material BaseMaterial;

    [SerializeField] private Mesh[] AlphabetModels;
    [SerializeField] private List<Kvp<char, Mesh>> SpecialModels;

    [HideInInspector] public int TotalTextLength;

    protected Mesh GetDigitMesh(char digit)
    {
        if (digit is >= 'a' and <= 'z')
            return AlphabetModels[digit - 'a'];

        return SpecialModels.First(c => c.Key == digit).Value;
    }

    public char GetDigitAt(int wordIndex, int digitIndex)
    {
        var word = words[wordIndex];
        return word[digitIndex]; //spaces are not treated synthetically now
        // return digitIndex >= word.Length ? ' ' : word[digitIndex];
    }

    public int GetWordLengthAt(int wordIndex) => words[wordIndex].Length;

    public abstract Vector3 GetDigitPozAt(int wordIndex, int digitIndex);
    public abstract Vector3 GetDigitRotAt(int wordIndex, int digitIndex);

    public abstract GameObject[] GetWordObjects(int wordIndex);

    public abstract int WordsCount { get; }
    
    protected virtual void Start()
    {
        NetManager.I.AddRpcContainer(this);

        capacity = RoomController.I.Capacity;
        words = RoomController.I.Words.Select(w => w.ToLower()).ToList();

        TotalTextLength = RoomController.I.Text.Length + 1; //1 for the initial added space

        MakePlayersColorPalette();

        CreatePlayers();

        GenerateDigits();

        SetPlayersStartPoz();

        SetCameraFollow();
    }

    private void SetCameraFollow()
    {
        MyPlayer.InstantCameraFollow();
        MyPlayer.CameraFollow();
    }

    private void SetPlayersStartPoz()
    {
        Players.ForEach(p => p.transform.position = GetDigitPozAt(0, 0));
        Players.ForEach(p => p.transform.eulerAngles = GetDigitRotAt(0, 0));
    }

    protected abstract void GenerateDigits();

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
                MyPlayer.Init(RoomController.I.MyTurn, this);

                Players.Add(MyPlayer);
            }
            else
            {
                var oppo = Instantiate(oppoPlayerPrefab).GetComponent<OppoBase>();
                oppo.Init(oppoPlaceCounter++, this);

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