using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;

public abstract class PlayerBase : MonoBehaviour
{
    public float startTime;

    public void Init(int index)
    {
        //todo change characters this way
        // await Extensions.LoadnoAndReleaseAsset<Sprite>(((CardbackType) selectedBackIndex).ToString(),
        // sprite => BackSprite = sprite);

        Index = index;

        nameText.text = "player " + index;

        //todo if you changed the start to 3 2 1 go, then don't call this here
        startTime = Time.time;
    }


    protected int Index;
    public int wordIndex;

    [SerializeField] private TMP_Text nameText;

    [SerializeField] protected Animator animator;
    private static readonly int Jump = Animator.StringToHash("jump");

    protected int DigitIndex;

    protected bool IsLastDigit()
    {
        return wordIndex == RoomController.I.Words.Length - 1 && CurrentDigit == ' ';
    }

    protected char CurrentDigit => EnvBase.I.GetDigitAt(wordIndex, DigitIndex);

    public event System.Action<int, int> MovedADigit;
    public event System.Action<int> MovedAWord;

    public void TakeInput(char digit)
    {
        if (char.ToLower(digit) != CurrentDigit) return;

        LastMoveTween.SkipTween();
        LastRotateTween.SkipTween();
        StepMoveTween.SkipTween();

        animator.SetTrigger(Jump);

        if (CurrentDigit == ' ')
        {
            JumpWord();
            return;
        }
        
        MoveADigit();

        MovedADigit?.Invoke(wordIndex, DigitIndex);
    }

    protected Vector3 MovePozWithLinearY;
    private Vector3[] currentPath;

    private const float MOVE_TIME = .2f;

    private void JumpToTarget()
    {
        var targetPoz = EnvBase.I.GetDigitPozAt(wordIndex, DigitIndex);
        
        var upVector = Vector3.up * Vector3.Distance(transform.position, targetPoz) * .5f;
        var middlePoint = Vector3.Lerp(transform.position, targetPoz, .5f);
        
        var middlePoz = middlePoint + upVector;
                        
        currentPath = new[] { transform.position, middlePoz, targetPoz };
        
        LastMoveTween = transform.DOPath(currentPath, .2F, PathType.CatmullRom)
            .OnUpdate(()=>StartCoroutine(SetLinearY()));
        LastRotateTween = transform.DORotate(EnvBase.I.GetDigitRotAt(wordIndex, DigitIndex), .2f);
    }
    
    //target pos is always known in the env
    //you need to pass a function to resolve the next pos
    //you just need digit positions
    private void MoveADigit()
    {
        JumpToTarget();
        DigitIndex++;
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


    protected TweenerCore<Vector3, Path, PathOptions> LastMoveTween;
    protected TweenerCore<Quaternion, Vector3, QuaternionOptions> LastRotateTween;
    protected Tweener StepMoveTween;

    protected virtual void JumpWord()
    {
        wordIndex++;
        DigitIndex = 0;
        
        JumpToTarget();
        
        MovedAWord?.Invoke(wordIndex);
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