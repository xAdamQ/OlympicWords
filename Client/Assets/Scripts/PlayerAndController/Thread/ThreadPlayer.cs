using System.Collections;
using DG.Tweening;
using UnityEngine;
public class ThreadPlayer : Player
{
    private readonly Vector3 spacing = new(.3f, 0f, 0f);
    private Animator animator;
    private new ParticleSystem particleSystem;

    protected override void Awake()
    {
        base.Awake();

        particleSystem = GetComponentInChildren<ParticleSystem>();
        animator = Mapper.Graphic.GetComponent<Animator>();
    }

    protected override void Start()
    {
        base.Start();

        MovedADigit += SliceAnim;
    }

    protected override Tween JumpMovement()
    {
        var bounds = RootEnv.I.GetCharObjectAt(TextPointer, Index).GetComponent<MeshRenderer>().bounds;

        var targetPoz = new Vector3(bounds.max.x, bounds.center.y, bounds.center.z) + spacing;

        MovePath = (transform.position, targetPoz);

        return transform.DOMove(targetPoz, .3f).SetEase(Ease.Linear);
    }

    private void SliceAnim()
    {
        // DOTween.Sequence()
        //     .Append(Graphic.transform.DORotate(new Vector3(45f, 0f, 0f), .1f))
        //     .Append(Graphic.transform.DORotate(new Vector3(70f, 0f, -44f), .1f))
        //     .Append(Graphic.transform.DORotate(new Vector3(70f, -45f, -200f), .1f))
        //     .Append(Graphic.transform.DORotate(new Vector3(7f, 70f, -55f), .05f))
        //     .Append(Graphic.transform.DORotate(new Vector3(0f, 0f, 0f), .05f));

        animator.SetBool(Attacking, true);

        if (idleRoutine != null)
            StopCoroutine(idleRoutine);

        idleRoutine = StartCoroutine(TryIdle());

        particleSystem.Play();

        StartCoroutine(SliceLater(RootEnv.I.GetCharObjectAt(TextPointer, Index)));
    }

    private IEnumerator SliceLater(GameObject c)
    {
        yield return new WaitForSeconds(.15f);
        AutoSlicer.Slice(c, 5f);
    }

    private Coroutine idleRoutine;
    private static readonly int Attacking = Animator.StringToHash("attacking");

    private IEnumerator TryIdle()
    {
        yield return new WaitForSeconds(.4f);
        animator.SetBool(Attacking, false);
    }

    protected override Tween JumpRotation()
    {
        return transform.DORotate(RootEnv.I.GetCharRotAt(TextPointer, Index), .2f);
    }
}