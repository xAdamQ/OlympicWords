using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// these are referenced items that can't reside on the env base because each environment will have to assign and
/// change them by its own despite they're shared
/// </summary>
public class EnvShared : MonoModule<EnvShared>
{
    [SerializeField] private Mesh[] AlphabetModels;
    [SerializeField] private List<Kvp<char, Mesh>> SpecialModels;

    public Mesh GetDigitMesh(char digit)
    {
        return digit switch
        {
            >= 'a' and <= 'z' => AlphabetModels[digit - 'a'],
            >= 'A' and <= 'Z' => throw new NotImplementedException(),
            _ => SpecialModels.First(c => c.Key == digit).Value
        };
    }
}