using DG.Tweening;
using UnityEngine;

public class MyStairPlayer : MyPlayerBase
{
    private Stair CurrentStair => StairEnv.I.stairs[Index][CurrentWordIndex];

    public void Start()
    {
        transform.position = StairPlayerCommon.GetRelativeStairPoz(CurrentStair.transform);
        transform.eulerAngles = StairPlayerCommon.GetRelativeStairAngle(CurrentStair.transform);
    }

    private void MoveStep()
    {
        var lowStep = StairEnv.I.spacing.y / CurrentStair.Word.Length;

        if (StairEnv.I.moveSteps)
            stepMoveTween = CurrentStair.transform.DOBlendableMoveBy(Vector3.down * lowStep, .2f)
                .OnUpdate(() => transform.position = new Vector3(transform.position.x, CurrentStair.transform.position.y, transform.position.z));
    }

    protected override void JumpWord()
    {
        base.JumpWord();

        CurrentStair.GetComponent<Renderer>().material.DOFade(StairEnv.I.fadeStepValue, StairEnv.I.fadeStepTime);
        foreach (Transform digit in CurrentStair.transform)
            digit.GetComponent<Renderer>().material.DOFade(StairEnv.I.fadeStepValue, StairEnv.I.fadeStepTime);
    }

    // protected virtual void JumpWord()
    // {
    //     CurrentWordIndex++;
    //     WordDigitIndex = 0;
    //
    //     var targetPoz =
    //         StairPlayerCommon.GetRelativeStairPoz(CurrentStair.transform);
    //     var middlePoz = Vector3.Lerp(transform.position, targetPoz, .5f) + Vector3.up * 1f;
    //     var path = new[] { transform.position, middlePoz, targetPoz };
    //
    //     lastRotateTween = transform.DORotate(StairPlayerCommon.GetRelativeStairAngle(CurrentStair.transform), .2f);
    //     lastMoveTween = transform.DOPath(path, .2F, PathType.CatmullRom);
    // }
}

public static class StairPlayerCommon
{
    public static Vector3 GetRelativeStairPoz(Transform stairTransform)
    {
        return stairTransform.position + Vector3.up * .1f
               - stairTransform.right * stairTransform.localScale.x * .5f
               + stairTransform.forward * stairTransform.localScale.z * .25f;
    }

    public static Vector3 GetRelativeStairAngle(Transform stairTransform)
    {
        return new Vector3(0, stairTransform.transform.eulerAngles.y + 90, 0);
    }
}