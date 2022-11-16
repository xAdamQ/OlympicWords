using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;

public abstract class PlayerBase : MonoBehaviour
{
    [HideInInspector] public EnvBase Env;

    [HideInInspector] public float startTime;

    [HideInInspector] public int
        Index,
        WordIndex,
        CharIndex,
        textPointer;

    [SerializeField] private TMP_Text nameText;

    private Animator animator;
    private static readonly int jump = Animator.StringToHash("jump");

    protected char CurrentChar => EnvBase.I.Text[textPointer];

    public PowerUp ChosenPowerUp;
    private int usedJets;

    private List<int> fillerWords;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
    }

    protected virtual void Start()
    {
        EnvBase.I.GameFinished += OnGameFinished;
        EnvBase.I.GameStarted += OnGameStarted;
    }

    protected virtual void OnGameStarted()
    {
        startTime = Time.time;
    }
    protected virtual void OnGameFinished()
    {
    }


    public void Init(int index, EnvBase env, int powerUp, List<int> myFillers, string name)
    {
        //todo change characters this way
        // await Extensions.LoadAndReleaseAsset<Sprite>(((CardbackType) selectedBackIndex).ToString(),
        // sprite => BackSprite = sprite);
        Env = env;
        Index = index;
        ChosenPowerUp = (PowerUp)powerUp;
        nameText.text = name;
        fillerWords = myFillers;
    }

    protected bool IsTextFinished()
    {
        //do we exceed it sometime?
        // if (globalCharIndex > EnvBase.I.Text.Length)
        // Debug.LogWarning
        // ($"MY WARN:::: the globalCharIndex: {globalCharIndex} exceeds the text length: {EnvBase.I.Text.Length}");

        return textPointer >= EnvBase.I.Text.Length;

        // return
        //     (WordIndex == EnvBase.I.Words.Count - 1 && CharIndex >= EnvBase.I.GetWordLengthAt(WordIndex))
        //     ||
        //     WordIndex > EnvBase.I.Words.Count - 1;
    }

    public event Action MovedADigit;
    public event Action<int> MovedAWord;

    public void TakeInput(char digit)
    {
        //supposed we won't have \r naturally in the text
        if (digit == '\r')
        {
            if (ChosenPowerUp == PowerUp.MegaJet && usedJets < 1)
                JetJump(4);
            else if (ChosenPowerUp == PowerUp.SmallJet && usedJets < 2)
                JetJump(1);
        }
        else if (digit == CurrentChar)
        {
            CharJump();
        }
    }

    private void CharJump()
    {
        PrepareNewJump();
        MoveADigit();

        //don't jump to the next word if there are no more words
        if (IsTextFinished())
            return;

        if (CharIndex == EnvBase.I.GetWordLengthAt(WordIndex))
            JumpWord();
    }

    private void PrepareNewJump()
    {
        lastMoveTween.SkipTween();
        lastRotateTween.SkipTween();
        stepMoveTween.SkipTween();

        animator.SetTrigger(jump);
    }

    protected Vector3 MovePozWithLinearY;
    private Vector3[] currentPath;

    private const float MOVE_TIME = .2f;

    private void MoveADigit()
    {
        JumpToTarget();


        MovedADigit?.Invoke();
        CharIndex++;
        textPointer++;
    }

    protected void JumpToTarget()
    {
        try
        {
            var targetPoz = EnvBase.I.GetDigitPozAt(WordIndex, CharIndex);
            var upVector = Vector3.up * (Vector3.Distance(transform.position, targetPoz) * .5f);
            var middlePoint = Vector3.Lerp(transform.position, targetPoz, .5f);

            var middlePoz = middlePoint + upVector;

            currentPath = new[] { transform.position, middlePoz, targetPoz };

            lastMoveTween = transform.DOPath(currentPath, .2F, PathType.CatmullRom)
                .OnUpdate(() => StartCoroutine(SetLinearY()));
            lastRotateTween = transform.DORotate(EnvBase.I.GetDigitRotAt(WordIndex, CharIndex), .2f);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private IEnumerator SetLinearY()
    {
        var framesCount = MOVE_TIME / Time.fixedDeltaTime;
        var part = 1 / framesCount;
        var startPoint = currentPath[0];
        var endPoint = currentPath[^1];
        for (var i = 0; i < framesCount; i++)
        {
            MovePozWithLinearY = Vector3.Lerp(startPoint, endPoint, i * part);
            yield return new WaitForFixedUpdate();
        }
    }

    private TweenerCore<Vector3, Path, PathOptions> lastMoveTween;
    private TweenerCore<Quaternion, Vector3, QuaternionOptions> lastRotateTween;
    private Tweener stepMoveTween;

    protected virtual void JumpWord()
    {
        WordIndex++;
        CharIndex = 0;

        if (fillerWords is { Count: > 0 })
            Debug.Log($"current word: {WordIndex}, and current filler {fillerWords[0]}");

        //we have fillers, and the coming is at least a filler
        if (fillerWords is { Count: > 0 } && WordIndex == fillerWords[0])
        {
            var toSkip = 0;
            while (fillerWords.Count > 0 && WordIndex + toSkip == fillerWords[0])
            {
                toSkip++;
                fillerWords.RemoveAt(0);
            }

            JetJump(toSkip);
        }
        else
        {
            MovedAWord?.Invoke(WordIndex);
        }
    }

    protected virtual void JetJump(int count)
    {
        PrepareNewJump();

        var consumedWords = 0;

        if (CurrentChar == ' ') textPointer++;
        //in case we are in the start of a word

        for (; consumedWords < count && !IsTextFinished(); textPointer++)
        {
            if (CurrentChar == ' ')
            {
                consumedWords++;
                WordIndex++;
            }
        }

        if (IsTextFinished())
        {
            WordIndex = EnvBase.I.WordsCount - 1;
            CharIndex = EnvBase.I.GetWordLengthAt(WordIndex) - 1;

            //just jump to the last digit
            JumpToTarget();

            //finish directly because time is critical, the player will see
            //the animation through finalize panel anyway
            EnvBase.I.FinishGame();
        }
        else
        {
            //jump to the initial space then skip it
            CharIndex = 0;
            JumpToTarget();

            CharIndex = 1;
            //it is guaranteed you will have an additional word
        }

        usedJets++;
    }


    public static string[] Titles =
    {
        "Basra Player", //all take this because the feature is not implemented yet
        "piece of skill",
        "holy son",
        "basra grandmaster",
        "top eater",
        "top eater",
        "top eater",
        "top eater",
        "top eater",
        "top eater",
        "top eater",
    };
}