using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestController : MonoModule<TestController>
{
    public bool UseTest;

    public Vector3 spacing;
    public bool moveSteps;
    public float fadeStepValue, fadeStepTime;
    public bool useConnected, circular, useRepetition;
    
    public Vector3 cameraPlayerOffset;

    protected override void Awake()
    {
        base.Awake();

        DontDestroyOnLoad(this);
    }
}