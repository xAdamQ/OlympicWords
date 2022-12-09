using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
        TextPointer;

    [SerializeField] private TMP_Text nameText;

    [SerializeField] private GameObject Jetpack;

    private Animator animator;
    private static readonly int jump = Animator.StringToHash("jump");

    private char CurrentChar => EnvBase.I.Text[TextPointer];

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

        return TextPointer >= EnvBase.I.Text.Length;

        // return
        //     (WordIndex == EnvBase.I.Words.Count - 1 && CharIndex >= EnvBase.I.GetWordLengthAt(WordIndex))
        //     ||
        //     WordIndex > EnvBase.I.Words.Count - 1;
    }

    public event Action MovedADigit;
    public event Action<int> MovedAWord;

    public void TakeInput(char chr)
    {
        //supposed we won't have \r naturally in the text
        if (chr == '\r')
        {
            if (ChosenPowerUp == PowerUp.MegaJet && usedJets < 1)
                JetJump(4);
            else if (ChosenPowerUp == PowerUp.SmallJet && usedJets < 2)
                JetJump(1);
        }
        else if (chr == CurrentChar)
        {
            CharJump();
        }
    }

    private void CharJump()
    {
        PrepareNewJump();
        JumpToCurrent();
        //CharIndex is the coming character, 

        MovedADigit?.Invoke();
        TextPointer++;

        //don't jump to the next word if there are no more words
        if (IsTextFinished())
            return;

        // if (CharIndex == EnvBase.I.GetWordLengthAt(WordIndex))
        if (EnvBase.I.Text[TextPointer - 1] == ' ')
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
    protected Vector3[] currentPath;

    private const float MOVE_TIME = .2f;
    [SerializeField] private float moveSpeed, movePower, moveRoot, jumpTime = .3f, automationSpeedUp, jetJumpSlowDown;


    protected virtual void JumpToCurrent(Action onDone = null)
    {
        var targetPoz = EnvBase.I.GetCharPozAt(TextPointer);
        var upVector = Vector3.up * (Vector3.Distance(transform.position, targetPoz) * .5f);
        var middlePoint = Vector3.Lerp(transform.position, targetPoz, .5f);

        var middlePoz = middlePoint + upVector;

        currentPath = new[] { transform.position, middlePoz, targetPoz };

        // var time = Vector3.Distance(targetPoz, transform.position) / moveSpeed;

        // var normalizedTime = MathF.Min(MathF.Pow(time, movePower), MathF.Pow(time, 1f / moveRoot));
        lastMoveTween = transform.DOPath(currentPath, jumpTime, PathType.CatmullRom)
            .OnUpdate(() => StartCoroutine(SetLinearY()))
            // .OnKill(() => onDone?.Invoke())
            .OnComplete(() => onDone?.Invoke());

        lastRotateTween = transform.DORotate(EnvBase.I.GetCharRotAt(TextPointer), .2f);
    }

    private IEnumerator SetLinearY()
    {
        var framesCount = MOVE_TIME / Time.fixedDeltaTime;
        var part = 1 / framesCount;
        var startPoint = currentPath[0];
        var endPoint = currentPath[^1];
        for (var i = 0; i < framesCount; i++)
        {
            var lazyPoz = Vector3.Lerp(startPoint, endPoint, i * part);
            MovePozWithLinearY = new Vector3(transform.position.x, lazyPoz.y, transform.position.z);
            yield return new WaitForFixedUpdate();
        }
    }

    private TweenerCore<Vector3, Path, PathOptions> lastMoveTween;
    private TweenerCore<Quaternion, Vector3, QuaternionOptions> lastRotateTween;
    private Tweener stepMoveTween;


    protected virtual void JumpWord()
    {
        WordIndex++;

        if (fillerWords is { Count: > 0 })
            Debug.Log($"current word: {WordIndex}, and current filler {fillerWords[0]}");

        //we have fillers, and the coming is at least a filler
        if (fillerWords is { Count: > 0 } && WordIndex == fillerWords[0])
        {
            StartCoroutine(SkipWord());

            Debug.Log("text pointer after skip: " + TextPointer);
        }
        else
        {
            MovedAWord?.Invoke(WordIndex);
        }
    }

    protected Action WordSkipping, WordSkipped;
    private IEnumerator SkipWord()
    {
        WordSkipping?.Invoke();

        fillerWords.RemoveAt(0);

        var original = jumpTime;
        jumpTime /= automationSpeedUp;

        while (CurrentChar != ' ')
        {
            CharJump();
            yield return new WaitForSeconds(jumpTime);
        }

        jumpTime = original;
        WordSkipped?.Invoke();

        CharJump();
        //this can make recursive call to skip work, I put it at the end to make it sequential
    }

    protected virtual void JetJump(int count)
    {
        Jetpack.SetActive(true);

        jumpTime *= jetJumpSlowDown;

        PrepareNewJump();

        var consumedWords = 0;

        if (CurrentChar == ' ') TextPointer++;
        //in case we are in the start of a word

        for (; consumedWords < count && !IsTextFinished(); TextPointer++)
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

            jumpPreChar();

            //finish directly because time is critical, the player will see
            //the animation through finalize panel anyway
            EnvBase.I.FinishGame();
        }
        else
        {
            jumpPreChar();
        }

        void jumpPreChar()
        {
            TextPointer--;
            JumpToCurrent(HideJetpack);
            TextPointer++;
        }

        usedJets++;
    }

    private void HideJetpack()
    {
        UniTask.Create(async () =>
        {
            jumpTime /= jetJumpSlowDown;
            await UniTask.Delay(250);
            Jetpack.SetActive(false);
        });
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