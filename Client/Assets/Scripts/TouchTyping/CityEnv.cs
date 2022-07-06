using System.Collections.Generic;
using PlasticPipe.PlasticProtocol.Client;
using UnityEngine;

public class CityEnv : EnvBase
{
    private List<int> Path;
    [SerializeField] private GraphData CityGraph;

    protected override void Start()
    {
        Path = GraphManager.GetRandomPath(CityGraph);
    }

    public override Vector3 GetDigitPozAt(int wordIndex, int digitIndex)
    {
        throw new System.NotImplementedException();
    }

    public override Vector3 GetDigitRotAt(int wordIndex, int digitIndex)
    {
        throw new System.NotImplementedException();
    }

    private const float DigitDistance = 1f;

    private GameObject[][] WordObjects;

    protected override void GenerateDigitsModels(string word, Stair stair)
    {
        WordObjects = new GameObject[word.Length][];

        const int edgeIndex = 0;

        foreach (var currentWord in words)
        {
            var currentEdge = Path[edgeIndex];

            var currentWordObject = new GameObject[currentWord.Length];
            // var edgeDir = currentEdge.End - currentEdge.Start; //todo make standard start/end

            for (var i = 0; i < currentWord.Length; i++)
            {
                // var currentDigit = currentWord[i];
                // var currentDigitObject = Instantiate(digitPrefab, currentEdge.Start + edgeDir * (i * DigitDistance), Quaternion.identity);
                // currentDigitObject.transform.localScale = new Vector3(1, 1, 1);
                // currentDigitObject.transform.localRotation = Quaternion.identity;
                // currentDigitObject.transform.localPosition = new Vector3(0, 0, 0);
                // currentDigitObject.transform.parent = currentEdge.transform;
                // currentDigitObject.name = currentDigit.ToString();
                // currentWordObject[i] = currentDigitObject;
            }
        }

        throw new System.NotImplementedException();
    }
}