using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerMapper))]
public abstract class Player : MonoBehaviour
{
    [HideInInspector] public float StartTime;

    [HideInInspector] public int
        Index,
        WordIndex,
        //text pointer point at the next character, not the current
        TextPointer;

    public ControllerConfig ControllerConfig;
    public PlayerConfig Config;

    [HideInInspector] public PlayerMapper Mapper;

    public char CurrentChar => RootEnv.I.Text[TextPointer];
    public Vector3 TargetPos { get; set; }
    public GameObject currentLetter => RootEnv.I.GetCharObjectAt(TextPointer, Index);

    [HideInInspector] public PowerUp ChosenPowerUp;
    private int usedJets;

    private List<int> fillerWords;
    protected float skipSpeed;

    protected virtual void Awake()
    {
        Mapper = GetComponent<PlayerMapper>();
        skipSpeed = Config.SkipSpeed;
    }

    protected virtual void Start()
    {
        RootEnv.I.GameStarted += OnGameStarted;
    }

    public void Init(int index, int powerUp, List<int> myFillers, string name)
    {
        Index = index;
        ChosenPowerUp = (PowerUp)powerUp;
        Mapper.nameText.text = name;
        fillerWords = myFillers;
    }

    private void OnGameStarted()
    {
        StartTime = Time.time;
    }

    public bool IsFinished => TextPointer >= RootEnv.I.Text.Length;

    public event Action<char> DoingLetter, LetterDone;
    public event Action<int> MovedAWord;

    public void TakeInput(char chr)
    {
        //supposed we won't have \r naturally in the text
        if (chr is '\r' or '\n')
        {
            if (ChosenPowerUp == PowerUp.MegaJet && usedJets < 1)
                PowerSkip(4);
            else if (ChosenPowerUp == PowerUp.SmallJet && usedJets < 2)
                PowerSkip(1);
        }
        else if (chr == CurrentChar)
        {
            DoLetter();
        }

        if (IsFinished) FinishGame();
    }

    private void DoLetter()
    {
        DoingLetter?.Invoke(CurrentChar);

        //you can add additional logic here

        LetterDone?.Invoke(CurrentChar);
        TextPointer++;

        //don't go to the next word if there are no more words
        if (IsFinished)
            return;

        if (RootEnv.I.Text[TextPointer - 1] == ' ')
            NextWord();
    }


    public Action GoingToNextWord, GoneToNextWord;
    private void NextWord()
    {
        GoingToNextWord?.Invoke();
        WordIndex++;

        //we have fillers, and the coming is at least a filler
        if (fillerWords is { Count: > 0 } && WordIndex == fillerWords[0])
            StartCoroutine(SkipWord());
        else
            MovedAWord?.Invoke(WordIndex);

        GoneToNextWord?.Invoke();
    }

    public Action WordSkipping, WordSkipped;
    /// <summary>
    /// skipping here is writing the word so fast, using the same do character logic
    /// </summary>
    private IEnumerator SkipWord()
    {
        WordSkipping?.Invoke();
        fillerWords.RemoveAt(0);

        while (CurrentChar != ' ')
        {
            DoLetter();
            yield return new WaitForSeconds(skipSpeed);
        }

        WordSkipped?.Invoke();

        DoLetter();
        //this can make recursive call to skip work, I put it at the end to make it sequential
    }

    public Action<int> PowerSkipping;
    public Action<int> PowerSkipped;
    private void PowerSkip(int count)
    {
        PowerSkipping?.Invoke(WordIndex);

        var lastWordIndex = WordIndex;

        if (CurrentChar == ' ') TextPointer++;
        //in case we are in the start of a word

        for (var consumedWords = 0; consumedWords < count && !IsFinished; TextPointer++)
        {
            if (CurrentChar != ' ') continue;
            consumedWords++;
            WordIndex++;
        }

        if (IsFinished)
            WordIndex = RootEnv.I.WordsCount - 1;

        usedJets++;

        PowerSkipped?.Invoke(lastWordIndex);
    }

    public abstract Type GetControllerType();

    public static readonly string[] Titles =
    {
        "Basra Player",
        "piece of skill",
        "holy son",
        "basra grandmaster",
        "top eater",
    };

    public event Action GameFinished;
    private void FinishGame()
    {
        GameFinished?.Invoke();
        Debug.Log("show finish particles here");
    }
}