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
        DigitIndex = 1,
        CurrentWordLength,
        globalCharIndex;

    [SerializeField] private TMP_Text nameText;

    private Animator animator;
    private static readonly int Jump = Animator.StringToHash("jump");

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Init(int index, EnvBase env)
    {
        //todo change characters this way
        // await Extensions.LoadAndReleaseAsset<Sprite>(((CardbackType) selectedBackIndex).ToString(),
        // sprite => BackSprite = sprite);
        Env = env;

        Index = index;

        nameText.text = "player " + index;

        //todo if you changed the start to 3 2 1 go, then don't call this here
        startTime = Time.time;

        CurrentWordLength = EnvBase.I.GetWordLengthAt(WordIndex);
    }

    protected bool IsTextFinished()
    {
        return globalCharIndex == RoomController.I.Text.Length;
        // return WordIndex == RoomController.I.Words.Length - 1 && CurrentDigit == ' ';
    }

    protected char CurrentDigit => EnvBase.I.GetDigitAt(WordIndex, DigitIndex);

    public event Action MovedADigit;
    public event Action<int> MovedAWord;

    public void TakeInput(char digit)
    {
        if (char.ToLower(digit) != CurrentDigit) return;

        lastMoveTween.SkipTween();
        lastRotateTween.SkipTween();
        stepMoveTween.SkipTween();

        animator.SetTrigger(Jump);

        MoveADigit();

        if (IsTextFinished()) return;

        if (DigitIndex == CurrentWordLength)
            JumpWord();
    }

    protected Vector3 MovePozWithLinearY;
    private Vector3[] currentPath;

    private const float MOVE_TIME = .2f;

    private void JumpToTarget()
    {
        var targetPoz = EnvBase.I.GetDigitPozAt(WordIndex, DigitIndex);

        var upVector = Vector3.up * Vector3.Distance(transform.position, targetPoz) * .5f;
        var middlePoint = Vector3.Lerp(transform.position, targetPoz, .5f);

        var middlePoz = middlePoint + upVector;

        currentPath = new[] { transform.position, middlePoz, targetPoz };

        lastMoveTween = transform.DOPath(currentPath, .2F, PathType.CatmullRom)
            .OnUpdate(() => StartCoroutine(SetLinearY()));
        lastRotateTween = transform.DORotate(EnvBase.I.GetDigitRotAt(WordIndex, DigitIndex), .2f);
    }

    //target pos is always known in the env
    //you need to pass a function to resolve the next pos
    //you just need digit positions
    private void MoveADigit()
    {
        JumpToTarget();
        MovedADigit?.Invoke();
        DigitIndex++;
        globalCharIndex++;
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
        DigitIndex = 0;
        CurrentWordLength = EnvBase.I.GetWordLengthAt(WordIndex);

        MovedAWord?.Invoke(WordIndex);
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