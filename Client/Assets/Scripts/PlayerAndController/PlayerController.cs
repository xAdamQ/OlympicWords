using System;
using System.Collections;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Player))]
public abstract class PlayerController : MonoModule<PlayerController>
{
    private bool canWrite;

    protected const float MOVE_TIME = .2f;

    //don't set this here, should be set by a concrete type
    public Player Player { get; protected set; }
    protected abstract Transform CameraTarget { get; }


    #region config
    public ControllerConfig Config;

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

        AssignEvents();
    }

    private void AssignEvents()
    {
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

        Player.PowerSkipped += _ =>
        {
            var remainingJets = int.Parse(RemainingJets.I.ValueText.text) - 1;
            RemainingJets.I.ValueText.text = remainingJets.ToString();
            if (remainingJets == 0)
                RemainingJets.I.gameObject.SetActive(false);
        };
    }

    protected virtual void Start()
    {
        Player.GameFinished += OnGameFinished;
        RootEnv.I.GameStarted += OnGameStarted;

        lazyYPosition = transform.position;
        StartCoroutine(SetLazyY());
    }

    private void OnGameStarted()
    {
        canWrite = true;
        Keyboard.current.onTextInput += OnTextInput;

        RemainingJets.I.GetComponent<CanvasGroup>().alpha = 1;
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

        if (Player.CurrentChar != c)
            KeyboardHint.I.ShowHint(Player.CurrentChar);
        else
            KeyboardHint.I.HideHint();

        Player.TakeInput(c);
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
    private Vector3 lazyYPosition;

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

    private IEnumerator SetLazyY()
    {
        // var framesCount = MOVE_TIME / Time.fixedDeltaTime;
        // var part = 1 / framesCount;
        // for (var i = 0; i < framesCount; i++)
        while (!Player.IsFinished)
        {
            // var lazyY = Mathf.Lerp(Player.MovePath.start.y, Player.MovePath.end.y, i * part);
            var lazyY = Mathf.Lerp(CameraTarget.position.y, Player.TargetPos.y, .1f);
            lazyYPosition = new Vector3(CameraTarget.position.x, lazyY, CameraTarget.position.z);
            //the look at in the y makes the camera less shaky
            yield return new WaitForFixedUpdate();
        }
    }

    private Vector3 GetTargetPoz()
    {
        return
            CameraTarget.right * CameraOffset.z +
            CameraTarget.up * CameraOffset.y +
            CameraTarget.forward * CameraOffset.x +
            lazyYPosition;
    }

    private IEnumerator FollowIEnumerator()
    {
        while (!Player.IsFinished)
        {
            mainCamera.position = Vector3.Lerp(mainCamera.transform.position, GetTargetPoz(), CameraMoveSmoothing);

            lastLookAtPoz = Vector3.Lerp(lastLookAtPoz, lazyYPosition, CameraLookSmoothing);
            mainCamera.LookAt(lastLookAtPoz);

            yield return new WaitForFixedUpdate();
        }
        // ReSharper disable once IteratorNeverReturns
    }
    #endregion
}

public abstract class PlayerController<TPlayer> : PlayerController where TPlayer : Player
{
    public new TPlayer Player
    {
        get => player;
        private set => base.Player = player = value;
    }
    private TPlayer player;

    protected override void Awake()
    {
        Player = GetComponent<TPlayer>();
        base.Awake();
    }
}