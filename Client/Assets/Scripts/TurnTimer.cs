using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using UnityEngine.XR;

public interface ITurnTimer
{
    bool IsPlaying { get; }

    event Action<int> Elapsed;
    event Action<float> Ticked;

    void Play(int currentTurn);
    void Stop();
}

//you can extract more generic form of this class for monobehaviour timer (move it outside room then)  /
//I merged both together and used coroutine
public class TurnTimer : MonoBehaviour, ITurnTimer
{
    private const int HandTime = 7; //the total interval

    public event Action<int> Elapsed;
    public event Action<float> Ticked;

    public bool IsPlaying { get; private set; }

    private Coroutine activeTimerCoroutine;

    private int currentTurn;

    public void Play(int currentTurn)
    {
        if (IsPlaying) Stop();
        this.currentTurn = currentTurn;
        activeTimerCoroutine = StartCoroutine(PlayEnumerator());
    }

    private IEnumerator PlayEnumerator()
    {
        IsPlaying = true;
        var targetTime = Time.realtimeSinceStartup + HandTime;

        while (Time.realtimeSinceStartup <= targetTime)
        {
            var timeLeft = targetTime - Time.realtimeSinceStartup;
            var progress = timeLeft / HandTime;

            Ticked?.Invoke(progress);

            yield return new WaitForFixedUpdate();
        }

        Ticked?.Invoke(0);

        //in the new design you don't reach here if cancelled
        Elapsed?.Invoke(currentTurn);

        IsPlaying = false;
    }

    // private IEnumerator PlayEnumerator()
    // {
    //     IsPlaying = true;
    //
    //     var ticksCount = HandTime / Time.fixedDeltaTime;
    //     for (var i = 0; i < ticksCount; i++)
    //     {
    //         var progress = (float) i / ticksCount;
    //         Ticked?.Invoke(progress);
    //
    //         yield return new WaitForFixedUpdate();
    //     }
    //
    //     Ticked?.Invoke(1);
    //
    //     //in the new design you don't reach here if cancelled
    //     Elapsed?.Invoke();
    //
    //     IsPlaying = false;
    // }

    public void Stop()
    {
        if (activeTimerCoroutine != null)
            StopCoroutine(activeTimerCoroutine);

        IsPlaying = false;
    }
}