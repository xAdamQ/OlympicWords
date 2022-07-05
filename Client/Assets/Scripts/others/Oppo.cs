using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Oppo : PlayerBase
{
    public override void TakeInput(char chr)
    {
        TakeDigit(chr);
    }
}