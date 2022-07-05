using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public abstract class PlayerBase : MonoBehaviour
{
    public float StartTime;

    public void Init(int index)
    {
        //todo change characters this way
        // await Extensions.LoadnoAndReleaseAsset<Sprite>(((CardbackType) selectedBackIndex).ToString(),
        // sprite => BackSprite = sprite);

        Index = index;
        transform.position = GetRelativeStairPoz(CurrentStair.transform);
        transform.eulerAngles = GetRelativeStairAngle(CurrentStair.transform);

        //todo if you changed the start to 3 2 1 go, then don't call this here
        StartTime = Time.time;
    }

    private Vector3 GetRelativeStairPoz(Transform stairTransform)
    {
        return stairTransform.position + Vector3.up * .1f
               - stairTransform.right * stairTransform.localScale.x * .5f
               + stairTransform.forward * stairTransform.localScale.z * .25f;
    }

    private Vector3 GetRelativeStairAngle(Transform stairTransform)
    {
        return new Vector3(0, stairTransform.transform.eulerAngles.y + 90, 0);
    }


    private int Index;
    public int CurrentWordIndex;
    private Stair CurrentStair => Gameplay.I.stairs[Index][CurrentWordIndex];

    [SerializeField] protected Animator animator;
    private static readonly int Jump = Animator.StringToHash("jump");

    protected int WordDigitIndex;
    public int DoneDigitsCount = 1;

    protected bool IsLastDigit()
    {
        return CurrentWordIndex == RoomController.I.Words.Length - 1 &&
               WordDigitIndex == CurrentStair.Word.Length;
    }

    private char CurrentDigit => WordDigitIndex < CurrentStair.Word.Length
        ? CurrentStair.Word[WordDigitIndex]
        : ' ';

    public event System.Action OnMovingDigit;
    //current digit index, total digits count    

    protected void TakeDigit(char digit)
    {
        if (digit != CurrentDigit) return;

        lastMoveTween.SkipTween();
        lastRotateTween.SkipTween();
        stepMoveTween.SkipTween();

        animator.SetTrigger(Jump);

        if (CurrentDigit == ' ')
        {
            JumpStair();
            return;
        }

        OnMovingDigit?.Invoke();

        var targetPoz = transform.position + transform.forward;
        var middlePoz = Vector3.Lerp(transform.position, targetPoz, .5f) + Vector3.up * 1f;
        var path = new[] { transform.position, middlePoz, targetPoz };

        lastMoveTween = transform.DOPath(path, .2F, PathType.CatmullRom);

        var lowStep = Gameplay.I.spacing.y / CurrentStair.Word.Length;

        if (Gameplay.I.moveSteps)
            stepMoveTween = CurrentStair.transform.DOBlendableMoveBy(Vector3.down * lowStep, .2f)
                .OnUpdate(() => transform.position = new Vector3(transform.position.x,
                    CurrentStair.transform.position.y,
                    transform.position.z));

        WordDigitIndex++;
        DoneDigitsCount++;
    }

    protected TweenerCore<Vector3, Path, PathOptions> lastMoveTween;
    private TweenerCore<Quaternion, Vector3, QuaternionOptions> lastRotateTween;
    private Tweener stepMoveTween;

    protected virtual void JumpStair()
    {
        CurrentStair.GetComponent<Renderer>().material
            .DOFade(Gameplay.I.fadeStepValue, Gameplay.I.fadeStepTime);
        foreach (Transform digit in CurrentStair.transform)
            digit.GetComponent<Renderer>().material.DOFade(Gameplay.I.fadeStepValue, Gameplay.I.fadeStepTime);

        CurrentWordIndex++;
        WordDigitIndex = 0;

        var targetPoz = GetRelativeStairPoz(CurrentStair.transform);
        var middlePoz = Vector3.Lerp(transform.position, targetPoz, .5f) + Vector3.up * 1f;
        var path = new[] { transform.position, middlePoz, targetPoz };

        lastRotateTween = transform.DORotate(GetRelativeStairAngle(CurrentStair.transform), .2f);
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

    public abstract void TakeInput(char chr);
}