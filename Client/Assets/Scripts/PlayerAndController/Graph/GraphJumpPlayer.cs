using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

/// <summary>
/// if you want to use it, remove the abstract and complete the functionality
/// </summary>
public abstract class GraphPlayer : Player
{
    protected override void Awake()
    {
        base.Awake();

        RootEnv.I.GamePrepared += () => { name = (GetComponent<PlayerController>() ? "me" : "oppo") + Index; };
    }
}

public class GraphJumpPlayer : GraphPlayer
{
    //this prevents overwriting the original values
    private float automationSpeedUp => Config.AutomationSpeedUp;
    private float jetJumpSlowDown => Config.JetJumpSlowDown;
    private float originalJumpTime => Config.JumpTime;
    private float jumpTime;

    private static readonly int jump = Animator.StringToHash("jump");

    public JumpControllerConfig JumpControllerConfig;
    private JumpPlayerMapper jumpMapper;

    protected override void Awake()
    {
        base.Awake();

        jumpMapper = GetComponent<JumpPlayerMapper>();

        jumpTime = originalJumpTime;

        AssignEvents();
    }

    public override Type GetControllerType()
    {
        return typeof(GraphJumpController);
    }

    private void AssignEvents()
    {
        WordSkipping += () => jumpTime /= automationSpeedUp;
        WordSkipped += () => jumpTime = originalJumpTime;


        PowerSkipping += _ =>
        {
            jumpTime *= jetJumpSlowDown;

            SkipTweens();

            jumpMapper.jetpack.SetActive(true);
            HideJetpack();
        };

        RootEnv.I.GamePrepared += () =>
        {
            lastTargetPoz = transform.position;
            lastTargetRot = transform.rotation;
        };

        PowerSkipped += _ =>
        {
            jumpTime = originalJumpTime;

            TextPointer--;
            JumpToCurrent();
            TextPointer++;
        };

        DoingLetter += _ => JumpToCurrent();
    }

    private void JumpToCurrent()
    {
        SkipTweens();

        Mapper.Animator.SetTrigger(jump);
        JumpMovement();

        lastMoveTween = JumpMovement();
        lastRotateTween = JumpRotation();
    }


    private void HideJetpack()
    {
        UniTask.Create(async () =>
        {
            await UniTask.Delay(TimeSpan.FromSeconds(Config.JetpackTime));
            jumpMapper.jetpack.SetActive(false);
        });
    }

    private Tween lastMoveTween;
    private Tween lastRotateTween;
    private Vector3 lastTargetPoz;
    private Quaternion lastTargetRot;
    private Tween JumpMovement()
    {
        var targetPoz = GraphEnv.I.GetCharPozAt(TextPointer, Index);
        var upVector = Vector3.up * (Vector3.Distance(transform.position, targetPoz) * .5f);
        var middlePoint = Vector3.Lerp(transform.position, targetPoz, .5f);

        var middlePoz = middlePoint + upVector;

        var path = new[] { transform.position, middlePoz, targetPoz };

        TargetPos = path[^1];

        lastTargetPoz = targetPoz;

        return transform.DOPath(path, jumpTime, PathType.CatmullRom);
    }
    private Tween JumpRotation()
    {
        lastTargetRot = RootEnv.I.GetCharRotAt(TextPointer, Index);
        return transform.DORotateQuaternion(lastTargetRot, .2f);
    }
    private void SkipTweens()
    {
        lastMoveTween.Kill();
        lastRotateTween.Kill();
        transform.position = lastTargetPoz;
        transform.rotation = lastTargetRot;
    }
}