using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using UnityEngine;

public class Player : PlayerBase
{
    void Update()
    {
        if (Input.inputString == currentDigit)
        {
            MoveADigit();
            NetManager.I.StreamChar(Input.inputString[0]);
        }
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
}