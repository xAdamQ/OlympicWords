using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class MyPlayerBase : PlayerBase
{
    public static MyPlayerBase I;

    protected override void Awake()
    {
        base.Awake();

        RoomBaseAdapter.I.PowerUpPanel.SetActive(true);

        I = this;

        if (TestController.I.UseTest)
            cameraOffset = TestController.I.cameraPlayerOffset;

        mainCamera = Camera.main!.transform;
    }

    protected virtual void Start()
    {
        EnvBase.I.GameFinished += OnGameFinished;
        EnvBase.I.GameStarted += OnGameStarted;

        MovePozWithLinearY = transform.position;
    }

    protected override void OnGameStarted()
    {
        Keyboard.current.onTextInput += OnTextInput;

        switch (ChosenPowerUp)
        {
            case PowerUp.SmallJet:
                RemainingJets.I.ValueText.text = EnvBase.SMALL_JETS_COUNT.ToString();
                break;
            case PowerUp.MegaJet:
                RemainingJets.I.ValueText.text = EnvBase.MEGA_JETS_COUNT.ToString();
                break;
            default:
                RemainingJets.DestroyModule();
                break;
        }
    }
    protected override void OnGameFinished()
    {
        Keyboard.current.onTextInput -= OnTextInput;
    }

    // private void Update()
    // {
    //     if (Keyboard.current.enterKey.wasPressedThisFrame)
    //     {
    //         if (ChosenPowerUp == PowerUp.MegaJet && usedJets < 1)
    //             MegaJetJump();
    //         else if (ChosenPowerUp == PowerUp.SmallJet && usedJets < 2)
    //             SmallJetJump();
    //     }
    // }

    private void OnTextInput(char c)
    {
        NetManager.I.StreamChar(c);

        Debug.Log("written: " + c);

        TakeInput(c);

        if (IsTextFinished())
            EnvBase.I.FinishGame();
    }

    private void OnDestroy()
    {
        OnGameFinished();
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