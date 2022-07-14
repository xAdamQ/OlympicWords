using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class MyPlayerBase : PlayerBase
{
    private void Awake()
    {
        if (TestController.I.UseTest)
            offset = TestController.I.cameraPlayerOffset;

        mainCamera = Camera.main.transform;
    }

    private void Start()
    {
        EnvBase.I.OnGameFinished += onGameFinished;
        Keyboard.current.onTextInput += onTextInput;
        
        MovePozWithLinearY = transform.position;
    }
    

    private void onTextInput(char c)
    {
        NetManager.I.StreamChar(c);
        
        TakeInput(c);

        if (IsLastDigit())
            EnvBase.I.FinishGame();
    }


    private void OnDestroy()
    {
        Keyboard.current.onTextInput -= onTextInput;
    }

    private void onGameFinished()
    {
        Keyboard.current.onTextInput -= onTextInput;
    }


    #region camera follow

    [SerializeField] private Vector3 offset;

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
                            transform.right * offset.z + 
                            transform.up * offset.y +
                            transform.forward * offset.x +
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
