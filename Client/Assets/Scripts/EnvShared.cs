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
    private Dictionary<char, Mesh> specialModelsDict;

    protected override void Awake()
    {
        base.Awake();

        specialModelsDict = SpecialModels.ToDictionary(x => x.Key, x => x.Value);
    }

    public Mesh GetLetterMesh(char letter)
    {
        return letter switch
        {
            >= 'a' and <= 'z' => AlphabetModels[letter - 'a'],
            >= 'A' and <= 'Z' => throw new NotImplementedException(),
            _ => specialModelsDict[letter]
        };
    }
}