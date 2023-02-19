using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public abstract class ThreadEnv : RootEnv
{
    public override Vector3 GetCharPozAt(int charIndex, int playerIndex)
    {
        var chr = GetCharObjectAt(charIndex, playerIndex);
        var minX = chr.GetComponent<MeshRenderer>().bounds.min.x;
        var center = chr.GetComponent<MeshRenderer>().bounds.center;
        return new Vector3(minX, center.y, center.z);
    }
    public override Vector3 GetCharRotAt(int charIndex, int playerIndex)
    {
        return allChars[playerIndex][charIndex].transform.eulerAngles;
    }
    public override GameObject GetCharObjectAt(int charIndex, int playerIndex)
    {
        return allChars[playerIndex][charIndex];
    }
    public override IEnumerable<GameObject> GetWordObjects(int wordIndex, int playerIndex)
    {
        return allChars[playerIndex][WordMap[wordIndex]..WordMap[wordIndex + 1]];
    }


    [SerializeField] private Transform threadInitialTransformsContainer;
    private readonly List<Transform> threadInitialTransforms = new();

    private GameObject[][] allChars;

    protected override void Awake()
    {
        base.Awake();

        foreach (Transform child in threadInitialTransformsContainer)
            threadInitialTransforms.Add(child);
    }

    protected override void SetPlayersInitialPoz()
    {
        for (var i = 0; i < Players.Count; i++)
        {
            Players[i].transform.position = GetCharPozAt(0, i);
            Players[i].transform.eulerAngles = GetCharRotAt(0, i);
        }
    }

    protected const float SPACING = .15f;

    protected override void GenerateDigits(Random random)
    {
        //I have a character buffer that get filled by time, so I have to notify the env with my current progress
        //the amount of chars is not that great, so I will do optimizations later, because it's needed for city env
        //as well

        var oppoCounter = 1;

        allChars = new GameObject[Players.Count][];

        for (var p = 0; p < Players.Count; p++)
        {
            var charObjects = new GameObject[Text.Length];
            var pointer = threadInitialTransforms[p == MyTurn ? 0 : oppoCounter++].position;

            for (var i = 0; i < Text.Length; i++)
            {
                var charObj = Instantiate(digitPrefab, pointer, Quaternion.identity, transform);

                var c = Text[i];

                var mesh = EnvShared.I.GetDigitMesh(c);
                charObj.GetComponent<MeshFilter>().mesh = mesh;
                var charWidth = mesh == null ? SPACE_DISTANCE : (mesh.bounds.size.x + SPACING);
                pointer += Vector3.right * charWidth;
                charObjects[i] = charObj;
                pointer += Vector3.right * SPACE_DISTANCE;
            }

            allChars[p] = charObjects;
        }
    }
    protected override void ColorFillers(List<(int index, int player)> fillerWords)
    {
        foreach (var (index, player) in fillerWords)
            for (var p = 0; p < Players.Count; p++)
                foreach (var wordObject in GetWordObjects(index, p))
                    wordObject.GetComponent<Renderer>().material = PlayerMats[player];
    }
}