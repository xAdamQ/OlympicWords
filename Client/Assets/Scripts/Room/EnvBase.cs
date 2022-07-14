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
        digitPrefab;

    public Material wordHighlightMat, digitHighlightMat;
    
    /////////////// SERIALIZED FIELDS
    protected List<string> words;
    protected int capacity;
    public Material BaseMaterial;

    [SerializeField] private Mesh[] DigitModels;

    protected Mesh GetDigitMesh(char digit) => DigitModels[digit - 'a'];

    public char GetDigitAt(int wordIndex, int digitIndex)
    {
        var word = words[wordIndex];
        return digitIndex >= word.Length ? ' ' : word[digitIndex];
    }

    public abstract Vector3 GetDigitPozAt(int wordIndex, int digitIndex);
    public abstract Vector3 GetDigitRotAt(int wordIndex, int digitIndex);
    public abstract GameObject[] GetWordObjects(int wordIndex);
    // public abstract void MyPlayerMoveADigit(int wordIndex, int digitIndex);
    
    protected virtual void Awake()
    {
        I = this;
    }

    protected virtual void Start()
    {
        NetManager.I.AddRpcContainer(this);

        capacity = RoomController.I.Capacity;
        words = RoomController.I.Words.Select(w=>w.ToLower()).ToList();

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
        Players.ForEach(p=>p.transform.position = GetDigitPozAt(0, 0));
        Players.ForEach(p=>p.transform.eulerAngles = GetDigitRotAt(0, 0));
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
                MyPlayer.Init(RoomController.I.MyTurn);

                Players.Add(MyPlayer);


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