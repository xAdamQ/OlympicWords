using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public abstract class PlayerBase : MonoBehaviour
{
    public float StartTime;
    public int totalDigitsCount = 1;

    protected EnvBase Env;

    public void Init(int index)
    {
        //todo change characters this way
        // await Extensions.LoadnoAndReleaseAsset<Sprite>(((CardbackType) selectedBackIndex).ToString(),
        // sprite => BackSprite = sprite);

        Index = index;

        //todo if you changed the start to 3 2 1 go, then don't call this here
        StartTime = Time.time;
    }


    protected int Index;
    public int CurrentWordIndex;

    [SerializeField] protected Animator animator;
    private static readonly int Jump = Animator.StringToHash("jump");

    protected int WordDigitIndex;

    protected bool IsLastDigit()
    {
        return CurrentWordIndex == RoomController.I.Words.Length - 1 && CurrentDigit == ' ';
    }

    private char CurrentDigit => Env.GetDigitAt(CurrentWordIndex, WordDigitIndex);
    private Vector3 CurrentDigitPosition => Env.GetDigitPozAt(CurrentWordIndex, WordDigitIndex);
    private Vector3 CurrentDigitRotation => Env.GetDigitRotAt(CurrentWordIndex, WordDigitIndex);

    public event System.Action OnMovingDigit;
    //current digit index, total digits count    

    public virtual void TakeInput(char digit)
    {
        if (digit != CurrentDigit) return;

        lastMoveTween.SkipTween();
        lastRotateTween.SkipTween();
        stepMoveTween.SkipTween();

        animator.SetTrigger(Jump);

        if (CurrentDigit == ' ')
        {
            JumpWord();
            return;
        }

        OnMovingDigit?.Invoke();

        MoveADigit();

        WordDigitIndex++;
        totalDigitsCount++;
    }

    //target pos is always known in the env
    //you need to pass a function to resolve the next pos
    //you just need digit positions
    protected virtual void MoveADigit()
    {
        // var targetPoz = transform.position + transform.forward;
        var targetPoz = Env.GetDigitPozAt(CurrentWordIndex, WordDigitIndex);
        var middlePoz = Vector3.Lerp(transform.position, targetPoz, .5f) + Vector3.up * 1f;
        var path = new[] { transform.position, middlePoz, targetPoz };

        lastMoveTween = transform.DOPath(path, .2F, PathType.CatmullRom);
    }


    protected TweenerCore<Vector3, Path, PathOptions> lastMoveTween;
    protected TweenerCore<Quaternion, Vector3, QuaternionOptions> lastRotateTween;
    protected Tweener stepMoveTween;

    protected virtual void JumpWord()
    {
        CurrentWordIndex++;
        WordDigitIndex = 0;

        var targetPoz = CurrentDigitPosition;
        var middlePoz = Vector3.Lerp(transform.position, targetPoz, .5f) + Vector3.up * 1f;
        var path = new[] { transform.position, middlePoz, targetPoz };

        lastRotateTween = transform.DORotate(CurrentDigitRotation, .2f);
        lastMoveTween = transform.DOPath(path, .2F, PathType.CatmullRom);
    }

    public static string[] Titles =
    {
        "Basra Player", //all take this because the feature is not implemented yet
        "piece of skill",
        "holy hanaka",
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