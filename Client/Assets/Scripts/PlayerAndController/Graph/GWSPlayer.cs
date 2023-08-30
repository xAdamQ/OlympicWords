using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// graph walk shoot
/// </summary>
public class GWSPlayer : GraphPlayer
{
    [SerializeField] private WalkPlayerConfig walkConfig;
    [SerializeField] public ShootPlayerConfig shootConfig;

    private GameObject rotationSlave;
    private float instantSpeed;
    private static readonly int moveSpeedKey = Animator.StringToHash("moveSpeed");
    private float animMoveAmount;
    private float currentDistance;
    private (float dist, int node) moveEdgeCounter = (0, 1), lookEdgeCounter = (0, 1);
    private List<Vector3> pathPositions, pathNormals;
    private bool jumping;
    [SerializeField] public Transform gunMuzzle;
    public Vector3 offset;
    private Queue<(float start, float end)> jumpDistances;

    protected override void Awake()
    {
        base.Awake();

        rotationSlave = new GameObject("rotationSlave" + Index);
        rotationSlave.transform.SetParent(transform);
    }

    protected override void Start()
    {
        base.Start();

        color = Random.ColorHSV();

        pathPositions = GraphEnv.I.smoothPath.Select(n => n.position).ToList();
        pathNormals = GraphEnv.I.smoothPath.Select(n => n.normal).ToList();
        jumpDistances = new(GraphEnv.I.jumperDistances);

        Debug.Log("j d c: " + GraphEnv.I.jumperDistances.Count);

        GameFinished += () => Mapper.Animator.SetFloat(moveSpeedKey, 0f);
    }

    private void FixedUpdate()
    {
        LookAtTarget();
        MoveTowardsCurrent();
    }

    private void MoveTowardsCurrent()
    {
        if (jumping || IsFinished) return;

        if (jumpDistances.Count > 0 && jumpDistances.Peek().start <= currentDistance)
        {
            jumping = true;

            var endDistance = jumpDistances.Dequeue().end;
            var endPoz = GraphManager.GetPointOnPath(pathPositions, pathNormals, endDistance, ref moveEdgeCounter).position;
            var middlePoint = Vector3.Lerp(transform.position, endPoz, .5f);
            var upVector = Vector3.up * (Vector3.Distance(transform.position, endPoz) * .5f);
            var middlePoz = middlePoint + upVector;

            var path = new[] { transform.position, middlePoz, endPoz };

            TargetPos = path[^1];

            transform.DOPath(path, .3f, PathType.CatmullRom)
                .OnComplete(onDone)
                .OnKill(onDone);

            void onDone()
            {
                jumping = false;
                currentDistance = endDistance;
            }

            return;
        }

        var targetDistance = GraphEnv.I.letterDistances[TextPointer] - GraphEnv.I.PlayerWordSpacing - offset.z;
        var prevDistance = currentDistance;
        currentDistance = FloatLerp(currentDistance, targetDistance, walkConfig.MoveLerp);
        instantSpeed = currentDistance - prevDistance;

        var pathPoint = GraphManager.GetPointOnPath(pathPositions, pathNormals, currentDistance, ref moveEdgeCounter);

        if (Physics.Raycast(pathPoint.position + pathPoint.normal * .5f, -pathPoint.normal, out var hit, 10f))
        {
            pathPoint.position.y = FloatLerp(transform.position.y, hit.point.y, walkConfig.MoveLerp);
        }

        Debug.DrawRay(pathPoint.position + pathPoint.normal * .5f, -pathPoint.normal, Color.blue, .25f);

        var forward = (pathPositions[moveEdgeCounter.node] - pathPositions[moveEdgeCounter.node - 1]).normalized;
        var right = Vector3.Cross(forward, pathPoint.normal);
        pathPoint.position += right * offset.x;

        transform.position = pathPoint.position;

        if (instantSpeed < walkConfig.AnimRunThreshold)
        {
            animMoveAmount = 0;
        }
        else
        {
            var targetSpeed = Mathf.Clamp01(instantSpeed * walkConfig.AnimSpeedMultiplier);
            animMoveAmount = FloatLerp(animMoveAmount, targetSpeed, walkConfig.AnimRunLerp);
        }

        Mapper.Animator.SetFloat(moveSpeedKey, animMoveAmount);
    }

    private Vector3 collisionPoint;
    private Color color;
    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawCube(collisionPoint, Vector3.one * .2f);
        Debug.Log(collisionPoint);
    }

    private void LookAtTarget()
    {
        if (IsFinished) return;

        var (_, normal, dir) = GraphManager
            .GetPointOnPath(pathPositions, pathNormals, GraphEnv.I.letterDistances[TextPointer], ref lookEdgeCounter);

        var rot = Quaternion.LookRotation(dir, normal);


        // if ((transform.position - targetPoz).magnitude < walkConfig.LetterLookAtThreshold)
        // return;

        // rotationSlave.transform.LookAt(targetPoz);
        // transform.rotation = Quaternion.Lerp(transform.rotation, rotationSlave.transform.rotation, walkConfig.RotationLerp);
        transform.rotation = rot;
    }

    private static float FloatLerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    public override Type GetControllerType()
    {
        return typeof(ShootController);
    }
}