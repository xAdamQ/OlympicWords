using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Oppo : Player
{
    private IEnumerator Start()
    {
        while (currentStairIndex < Gameplay.I.Stairs[Index].Count)
        {
            MoveADigit();
            yield return new WaitForSeconds(Random.Range(0f, .5f));
        }
    }
}