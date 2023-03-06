using DG.Tweening;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class GraphJumpPlayer : Player
{
    private static readonly int jump = Animator.StringToHash("jump");

    protected override void Awake()
    {
        base.Awake();
        Jumping += OnJumping;
    }

    private void OnJumping() => animator.SetTrigger(jump);

    protected override Tween JumpMovement()
    {
        var targetPoz = GraphEnv.I.GetCharPozAt(TextPointer, Index);
        var upVector = Vector3.up * (Vector3.Distance(transform.position, targetPoz) * .5f);
        var middlePoint = Vector3.Lerp(transform.position, targetPoz, .5f);

        var middlePoz = middlePoint + upVector;

        var path = new[] { transform.position, middlePoz, targetPoz };

        MovePath = (path[0], path[^1]);

        return transform.DOPath(path, JumpTime, PathType.CatmullRom);
    }

    protected override Tween JumpRotation()
    {
        return transform.DORotate(RootEnv.I.GetCharRotAt(TextPointer, Index), .2f);
    }
}