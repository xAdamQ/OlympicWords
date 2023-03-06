using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player))]
public abstract class PlayerController : MonoModule<PlayerController>
{
    private bool canWrite;

    protected const float MOVE_TIME = .2f;

    //don't set this here, should be set by a concrete type
    protected Player Player { get; set; }

    #region config
    public ControllerConfig Config;

    protected float JumpZoomCoefficient => Config.JumpZoomCoefficient;
    protected Material WordHighlightMat => Config.WordHighlightMat;
    protected Material FadeMaterial => Config.FadeMaterial;
    protected Vector3 CameraOffset => Config.CameraOffset;
    protected float CameraMoveSmoothing => Config.CameraMoveSmoothing;
    protected float CameraLookSmoothing => Config.CameraLookSmoothing;
    #endregion

    protected override void Awake()
    {
        base.Awake();

        Config = Player.ControllerConfig;

        mainCamera = Camera.main!.transform;

        Player.WordSkipping += () =>
        {
            canWrite = false;
            Keyboard.current.onTextInput -= OnTextInput;
        };
        Player.WordSkipped += () =>
        {
            canWrite = true;
            Keyboard.current.onTextInput += OnTextInput;
        };
    }

    protected virtual void Start()
    {
        RootEnv.I.GameFinished += OnGameFinished;
        RootEnv.I.GameStarted += OnGameStarted;
    }

    private void OnGameStarted()
    {
        canWrite = true;
        Keyboard.current.onTextInput += OnTextInput;

        switch (Player.ChosenPowerUp)
        {
            case PowerUp.SmallJet:
                RemainingJets.I.ValueText.text = RootEnv.SMALL_JETS_COUNT.ToString();
                break;
            case PowerUp.MegaJet:
                RemainingJets.I.ValueText.text = RootEnv.MEGA_JETS_COUNT.ToString();
                break;
            default:
                RemainingJets.DestroyModule();
                break;
        }
    }

    private void OnGameFinished()
    {
        canWrite = false;
        Keyboard.current.onTextInput -= OnTextInput;
    }

    private void OnTextInput(char c)
    {
        RoomNet.I.StreamChar(c);

        Player.TakeInput(c);

        if (Player.IsFinished())
            RootEnv.I.FinishGame();
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current.enterKey.wasPressedThisFrame && canWrite)
            OnTextInput('\r');
    }
#endif


    private void OnDestroy()
    {
        OnGameFinished();
    }

    #region camera follow
    private Vector3 lastLookAtPoz;
    private Transform mainCamera;
    private Coroutine followRoutine;

    public void SetCameraFollow()
    {
        InstantCameraFollow();
        CameraFollow();
    }

    private void CameraFollow()
    {
        if (followRoutine != null) StopCoroutine(followRoutine);

        followRoutine = StartCoroutine(FollowIEnumerator());
    }

    private void InstantCameraFollow()
    {
        mainCamera.position = GetTargetPoz();
        mainCamera.LookAt(transform);
    }

    protected abstract Vector3 GetTargetPoz();
    protected abstract Vector3 TargetLookAt { get; }

    private IEnumerator FollowIEnumerator()
    {
        while (true)
        {
            mainCamera.position = Vector3.Lerp(mainCamera.transform.position, GetTargetPoz(), CameraMoveSmoothing);

            lastLookAtPoz = Vector3.Lerp(lastLookAtPoz, TargetLookAt, CameraLookSmoothing);
            mainCamera.LookAt(lastLookAtPoz);

            yield return new WaitForFixedUpdate();
        }
        // ReSharper disable once IteratorNeverReturns
    }
    #endregion
}

public abstract class PlayerController<TPlayer> : PlayerController where TPlayer : Player
{
    protected new TPlayer Player
    {
        get => player;
        set => base.Player = player = value;
    }
    private TPlayer player;

    protected override void Awake()
    {
        Player = GetComponent<TPlayer>();
        base.Awake();
    }
}