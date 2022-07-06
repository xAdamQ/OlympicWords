using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class MyPlayerBase : PlayerBase
{
    private void Start()
    {
        EnvBase.I.OnGameFinished += onGameFinished;
        Keyboard.current.onTextInput += onTextInput;
    }

    private void onTextInput(char c)
    {
        NetManager.I.StreamChar(c);
        TakeInput(c);

        if (IsLastDigit())
            EnvBase.I.FinishGame();

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

    protected override void JumpWord()
    {
        base.JumpWord();

        var camera = Camera.main;

        if (StairEnv.I.useConnected)
            lastMoveTween.OnUpdate(() => camera!.transform.LookAt(transform));

        lastMoveTween.OnComplete(() =>
        {
            camera!.GetComponent<CameraFollow>().Follow();
            if (StairEnv.I.useConnected)
                camera!.transform.LookAt(transform);
        });
    }

    public override void TakeInput(char chr)
    {
        //you can add second player shadow shows the current server state
    }
}