using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public abstract class PlayerBase : MonoBehaviour
{
    public void Init(int index)
    {
        //todo change characters this way
        // await Extensions.LoadAndReleaseAsset<Sprite>(((CardbackType) selectedBackIndex).ToString(),
        // sprite => BackSprite = sprite);

        Index = index;
        transform.position = GetRelativeStairPoz(currentStair.transform);
        transform.eulerAngles = GetRelativeStairAngle(currentStair.transform);
    }

    protected Vector3 GetRelativeStairPoz(Transform stairTransform)
    {
        return stairTransform.position + Vector3.up * .1f
               - stairTransform.right * stairTransform.localScale.x * .5f
               + stairTransform.forward * stairTransform.localScale.z * .25f;
    }

    protected Vector3 GetRelativeStairAngle(Transform stairTransform)
    {
        return new Vector3(0, stairTransform.transform.eulerAngles.y + 90, 0);
    }


    protected int Index;
    protected int currentStairIndex;
    protected Stair currentStair => Gameplay.I.Stairs[Index][currentStairIndex];

    protected Animator animator;
    protected static readonly int Jump = Animator.StringToHash("jump");

    protected int currentDigitIndex;

    protected string currentDigit => currentDigitIndex < currentStair.Word.Length
        ? currentStair.Word[currentDigitIndex].ToString()
        : " ";

    protected void Awake()
    {
        animator = GetComponent<Animator>();
    }

    protected void MoveADigit()
    {
        lastMoveTween.SkipTween();
        lastRotateTween.SkipTween();
        stepMoveTween.SkipTween();

        animator.SetTrigger(Jump);

        if (currentDigit == " ")
        {
            JumpStair();
            return;
        }

        var targetPoz = transform.position + transform.forward;
        var middlePoz = Vector3.Lerp(transform.position, targetPoz, .5f) + Vector3.up * 1f;
        var path = new[] {transform.position, middlePoz, targetPoz};

        lastMoveTween = transform.DOPath(path, .2F, PathType.CatmullRom);

        var lowStep = Gameplay.I.spacing.y / currentStair.Word.Length;

        if (Gameplay.I.moveSteps)
            stepMoveTween = currentStair.transform.DOBlendableMoveBy(Vector3.up * lowStep, .2f)
                .OnUpdate(() => transform.position = new Vector3(transform.position.x,
                    currentStair.transform.position.y,
                    transform.position.z));

        currentDigitIndex++;
    }

    protected TweenerCore<Vector3, Path, PathOptions> lastMoveTween;
    protected TweenerCore<Quaternion, Vector3, QuaternionOptions> lastRotateTween;
    protected Tweener stepMoveTween;

    protected virtual void JumpStair()
    {
        currentStair.GetComponent<Renderer>().material
            .DOFade(Gameplay.I.fadeStepValue, Gameplay.I.fadeStepTime);
        foreach (Transform digit in currentStair.transform)
            digit.GetComponent<Renderer>().material.DOFade(Gameplay.I.fadeStepValue, Gameplay.I.fadeStepTime);

        currentStairIndex++;
        currentDigitIndex = 0;

        var targetPoz = GetRelativeStairPoz(currentStair.transform);
        var middlePoz = Vector3.Lerp(transform.position, targetPoz, .5f) + Vector3.up * 1f;
        var path = new[] {transform.position, middlePoz, targetPoz};

        lastRotateTween = transform.DORotate(GetRelativeStairAngle(currentStair.transform), .2f);
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