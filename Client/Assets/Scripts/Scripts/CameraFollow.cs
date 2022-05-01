using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    [SerializeField] private Vector3 offset;

    private Coroutine FollowCo;

    public void Follow()
    {
        if (FollowCo != null)
            StopCoroutine(FollowCo);

        FollowCo = StartCoroutine(FollowIEnumerator(.3f));
    }

    public void InstantFollow()
    {
        var finalPosition =
            target.right * offset.z +
            target.up * offset.y +
            target.forward * offset.x +
            target.position;

        transform.position = finalPosition;
        transform.LookAt(target);
    }

    private IEnumerator FollowIEnumerator(float animTime)
    {
        var finalPosition =
            target.right * offset.z +
            target.up * offset.y +
            target.forward * offset.x +
            target.position;

        transform.DOMove(finalPosition, animTime).OnUpdate(() =>
        {
            if (Gameplay.I.useConnected) transform.LookAt(target);
        });

        yield return new WaitForSeconds(animTime);
        while (true)
        {
            //finalPosition = 
            //    target.right * offset.z +
            //    target.forward * offset.x +
            //    target.position;
            //
            //transform.position += (finalPosition - transform.position) * .05f;
            //transform.position = Vector3.Lerp (transform.position, finalPosition, .04f);

            var targetPosition = target.right * offset.z + target.forward * offset.x + target.position;
            targetPosition.y = transform.position.y;
            //practically this will affect the absolute x, z. the y will be 0
            transform.position = Vector3.Lerp(transform.position, targetPosition, .1f);
            //transform.position = finalForward;

            //transform.LookAt(target);


            yield return new WaitForFixedUpdate();
        }
    }
}