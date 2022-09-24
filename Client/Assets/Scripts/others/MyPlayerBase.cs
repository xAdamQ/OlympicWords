using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class MyPlayerBase : PlayerBase
{
    protected override void Awake()
    {
        base.Awake();

        if (TestController.I.UseTest)
            cameraOffset = TestController.I.cameraPlayerOffset;

        mainCamera = Camera.main!.transform;
    }

    protected virtual void Start()
    {
        EnvBase.I.OnGameFinished += OnGameFinished;
        Keyboard.current.onTextInput += OnTextInput;

        MovePozWithLinearY = transform.position;
    }

    private void OnTextInput(char c)
    {
        NetManager.I.StreamChar(c);

        TakeInput(c);

        if (IsTextFinished())
            EnvBase.I.FinishGame();
    }


    private void OnDestroy()
    {
        Keyboard.current.onTextInput -= OnTextInput;
    }

    private void OnGameFinished()
    {
        Keyboard.current.onTextInput -= OnTextInput;
    }


    #region camera follow

    [SerializeField] private Vector3 cameraOffset;

    private Vector3 lastLookAtPoz;
    private Coroutine followCo;
    private Transform mainCamera;

    public void CameraFollow()
    {
        if (followCo != null) StopCoroutine(followCo);

        followCo = StartCoroutine(FollowIEnumerator());
    }

    public void InstantCameraFollow()
    {
        mainCamera.position = GetTargetPoz();
        mainCamera.LookAt(transform);
    }

    private Vector3 GetTargetPoz()
    {
        var targetPosition =
            transform.right * cameraOffset.z +
            transform.up * cameraOffset.y +
            transform.forward * cameraOffset.x +
            MovePozWithLinearY;

        return targetPosition;
    }

    private IEnumerator FollowIEnumerator()
    {
        // transform.DOMove(finalPosition, animTime).OnUpdate(() =>
        // {
        //     if (StairEnv.I.useConnected) transform.LookAt(target);
        // });

        while (true)
        {
            var targetPosition = GetTargetPoz();

            mainCamera.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, .05f);

            lastLookAtPoz = Vector3.Lerp(lastLookAtPoz, MovePozWithLinearY, .05f);
            mainCamera.LookAt(lastLookAtPoz);

            yield return new WaitForFixedUpdate();
        }
        // ReSharper disable once IteratorNeverReturns
    }

    #endregion
}