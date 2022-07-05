using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class MyPlayer : PlayerBase
{
    void Update()
    {
        // animator.SetTrigger("jump");
    }

    private void Start()
    {
        Gameplay.I.OnGameFinished += onGameFinished;
        Keyboard.current.onTextInput += onTextInput;
    }

    private void onTextInput(char c)
    {
        NetManager.I.StreamChar(c);
        TakeDigit(c);

        if (IsLastDigit())
            Gameplay.I.FinishGame();

        Debug.Log($"{WordDigitIndex}");
    }


    private void OnDestroy()
    {
        Keyboard.current.onTextInput -= onTextInput;
    }

    private void onGameFinished()
    {
        Keyboard.current.onTextInput -= onTextInput;
    }

    protected override void JumpStair()
    {
        base.JumpStair();

        var camera = Camera.main;

        if (Gameplay.I.useConnected)
            lastMoveTween.OnUpdate(() => camera!.transform.LookAt(transform));

        lastMoveTween.OnComplete(() =>
        {
            camera!.GetComponent<CameraFollow>().Follow();
            if (Gameplay.I.useConnected)
                camera!.transform.LookAt(transform);
        });
    }

    public override void TakeInput(char chr)
    {
        //you can add second player shadow shows the current server state
    }
}