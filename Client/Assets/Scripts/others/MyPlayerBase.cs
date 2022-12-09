using System;
using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class MyPlayerBase : PlayerBase
{
    protected override void Awake()
    {
        base.Awake();

        RoomBaseAdapter.I.PowerUpPanel.SetActive(true);

        if (TestController.I.UseTest)
            cameraOffset = TestController.I.cameraPlayerOffset;

        mainCamera = Camera.main!.transform;

        WordSkipping += () =>
        {
            canWrite = false;
            Keyboard.current.onTextInput -= OnTextInput;
        };
        WordSkipped += () =>
        {
            canWrite = true;
            Keyboard.current.onTextInput += OnTextInput;
        };
    }

    private bool canWrite;

    protected override void Start()
    {
        base.Start();

        MovePozWithLinearY = transform.position;
    }

    [SerializeField] private float jumpZoomCoefficient;
    protected override void JumpToCurrent(Action onDone = null)
    {
        onDone += () => UniTask.Delay(300).ContinueWith(() => addedOffset = Vector3.zero).Forget();
        base.JumpToCurrent(onDone);
        addedOffset = Vector3.Distance(currentPath[0], currentPath[^1]) * jumpZoomCoefficient * Vector3.one;
    }

    protected override void OnGameStarted()
    {
        canWrite = true;
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
        canWrite = false;
        Keyboard.current.onTextInput -= OnTextInput;
    }

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

    private void Update()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (Keyboard.current.enterKey.wasPressedThisFrame && canWrite)
            OnTextInput('\r');
#endif
    }

    #region camera follow

    [SerializeField] private Vector3 cameraOffset;
    private Vector3 addedOffset;

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
        var offset = cameraOffset + addedOffset;
        var targetPosition =
            transform.right * offset.z +
            transform.up * offset.y +
            transform.forward * offset.x +
            MovePozWithLinearY;

        return targetPosition;
    }

    [SerializeField] private float cameraMoveSmoothing, cameraLookSmoothing;
    private IEnumerator FollowIEnumerator()
    {
        while (true)
        {
            mainCamera.position = Vector3.Lerp(mainCamera.transform.position, GetTargetPoz(), cameraMoveSmoothing);

            lastLookAtPoz = Vector3.Lerp(lastLookAtPoz, MovePozWithLinearY, cameraLookSmoothing);
            mainCamera.LookAt(lastLookAtPoz);

            yield return new WaitForFixedUpdate();
        }
        // ReSharper disable once IteratorNeverReturns
    }

    #endregion
}