using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class NormalCharGraphEnv : EnvBase
{
    private GameObject[][] wordObjects;
    private readonly Vector3 digitAddedRotation = Vector3.zero;
    [SerializeField] private GraphData[] graphs;

    private GraphData graph => graphs[chosenGraphIndex];
    private int chosenGraphIndex;

    private const float DIGIT_SIZE = .7f,
        DIGIT_FILL_PERCENT = .8f,
        SPACE_DISTANCE = 1f,
        MAX_DIGIT_SIZE = 1f,
        DIGIT_Y_EXTEND = .13f,
        SPACING_Y = .1f;


    protected override void Awake()
    {
        base.Awake();
        chosenGraphIndex = Random.Range(0, graphs.Length);
    }

    private static Vector3 GetProjectedPoz(Vector3 at)
    {
        var res = at;
        var rayHit = Physics.Raycast(at + Vector3.up * 2, Vector3.down, out var hitInfo, 3, ~6);

        if (!rayHit) return res;

        res = hitInfo.point + (DIGIT_Y_EXTEND + SPACING_Y) * Vector3.up;

        return res;
    }

    public override Vector3 GetDigitPozAt(int wordIndex, int digitIndex)
    {
        var digit = wordObjects[wordIndex][digitIndex];
        var maxBound = digit.GetComponent<MeshRenderer>().bounds.max - new Vector3(.1f, 0, .1f);
        var minBound = digit.GetComponent<MeshRenderer>().bounds.min - new Vector3(.1f, 0, .1f);

        return new Vector3(
            Random.Range(minBound.x, maxBound.x),
            maxBound.y + .1f,
            Random.Range(minBound.z, maxBound.z));

        // return wordObjects[wordIndex][digitIndex].transform.position + Vector3.up * .3f;
    }

    public override Vector3 GetDigitRotAt(int wordIndex, int digitIndex)
    {
        return wordObjects[wordIndex][digitIndex].transform.eulerAngles - digitAddedRotation;
    }

    public override GameObject[] GetWordObjects(int wordIndex)
    {
        return wordObjects[wordIndex];
    }

    private static float GetEdgeLengths(List<Vector3> nodes)
    {
        var totalDistance = 0f;
        for (var i = 1; i < nodes.Count; i++)
            totalDistance += Vector3.Distance(nodes[i - 1], nodes[i]);

        return totalDistance;
    }

    public static List<Vector3> SmoothenAngles(List<Vector3> path)
    {
        if (path.Count <= 2) return path;

        const float cutRatio = .45f, cutValue = 1f;

        var res = new List<Vector3> { path[0] };
        for (var i = 1; i < path.Count - 1; i++)
        {
            var start = path[i - 1] - path[i];
            var end = path[i + 1] - path[i];

            //the cut value is relatively big
            var realStartCut = cutValue > start.magnitude * .5f ? start.magnitude * cutRatio : cutValue;
            var realEndCut = cutValue > end.magnitude * .5f ? end.magnitude * cutRatio : cutValue;

            var startCutPoint = start.normalized * realStartCut + path[i];
            var endCutPoint = end.normalized * realEndCut + path[i];

            res.Add(startCutPoint);
            res.Add(endCutPoint);
        }

        res.Add(path[^1]);

        return res;
    }

    /// <summary>
    /// the only difference between generate digit functions in different environment is the transform of the digit until now
    /// </summary>
    protected override void GenerateDigits()
    {
        wordObjects = new GameObject[words.Count][];
        List<(int node, bool isWalkable)> path;
        List<(Node node, bool isWalkable)> nodes;

        path = GraphManager.GetRandomPath(graph);

        nodes = path.Select(n => (graph.Nodes[n.node], n.isWalkable)).ToList();
        var nodeCounter = 1;

        // (Vector2 start, Vector2 end, List<Arc>) getDynamicPath(List<Vector2> path)
        // {
        //     var res = new List<Arc>();
        //     const float cut = 1.5f;
        //     for (var i = 2; i < path.Count; i++)
        //     {
        //         var arc = new Arc(path[i - 1], path[i - 2], path[i], cut);
        //
        //         res.Add(arc);
        //     }
        //
        //     return (path[0], path[^1], res);
        // }

        // Debug.Log(string.Join(", ", nodes.Select(n => n.isWalkable)));

        //there would be at least 2 nodes in the path

        for (var w = 0; w < words.Count; w++)
        {
            while (!nodes[nodeCounter].isWalkable && nextNode())
            {
            }
            //skip all jumper edges

            var subPath = new List<Vector3> { nodes[nodeCounter - 1].node.Position }.ToList();
            //the first edge is added anyway, this is the first nodes of it and the second in do statement

            do subPath.Add(nodes[nodeCounter].node.Position);
            while (nextNode() && nodes[nodeCounter].isWalkable);
            //add all walkable edges //edge type follows the second node

            subPath = SmoothenAngles(subPath);
            subPath = SmoothenAngles(subPath);

            var fillableWords = 1;
            var totalDigits = words[w].Length;
            //there at least the current word in the edge

            var edgesLength = GetEdgeLengths(subPath);
            // var edgesLength = GetEdgeLengths(start, end, arcs);

            while (w + 1 < words.Count && totalDigits + fullWordLength(words[w + 1]) <= edgesLength)
            {
                w++;
                totalDigits += words[w].Length;
                fillableWords++;
            }
            //try add more words if possible

            var actualDigitSize = (edgesLength - fillableWords * SPACE_DISTANCE) / totalDigits;
            if (actualDigitSize > MAX_DIGIT_SIZE) actualDigitSize = MAX_DIGIT_SIZE;
            //set digit size

            var passedDistance = SPACE_DISTANCE / 2f;

            using var subPathE = subPath.AsEnumerable().GetEnumerator();

            for (var lw = 0; lw < fillableWords; lw++)
            {
                var globalWordIndex = w - (fillableWords - 1) + lw;

                var currentWord = words[globalWordIndex];
                var wordObject = new GameObject[currentWord.Length];
                wordObjects[globalWordIndex] = new GameObject[currentWord.Length];

                var digitStartPoint = GetPointOnPath(subPath, passedDistance);

                for (var i = 0; i < currentWord.Length; i++)
                {
                    passedDistance += actualDigitSize;

                    // var digitEndPoint = GetPointOnPath(start, end, arcs, passedDistance);
                    // subPathE = ShortenPath(subPathE, passedDistance);
                    // var digitEndPoint = subPathE.Current;
                    var digitEndPoint = GetPointOnPath(subPath, passedDistance);

                    var digitStartProjection = GetProjectedPoz(digitStartPoint);
                    var digitEndProjection = GetProjectedPoz(digitEndPoint);

                    var currentDigit = currentWord[i];

                    var finalPoz = Vector3.Lerp(digitStartProjection, digitEndProjection, .5f);

                    var digitObject = Instantiate(digitPrefab, finalPoz, Quaternion.identity, transform);
                    wordObjects[globalWordIndex][i] = digitObject;

                    digitObject.GetComponent<MeshFilter>().mesh = GetDigitMesh(currentDigit);

                    digitObject.transform.LookAt(digitEndProjection);
                    digitObject.transform.eulerAngles -= digitAddedRotation;
                    digitObject.transform.position += Vector3.up * .4f;
                    digitObject.transform.localScale = Vector3.one * actualDigitSize * DIGIT_FILL_PERCENT;

                    wordObject[i] = digitObject;

                    digitStartPoint = digitEndPoint;
                }

                passedDistance += SPACE_DISTANCE;
            }

            if (!nextNode()) break;

            bool nextNode()
            {
                if (nodeCounter == nodes.Count - 1) //the last node already
                {
                    Debug.Log("out of nodes!");
                    return false;
                }

                nodeCounter++;
                return true;
            }
        }


        float fullWordLength(string word)
        {
            return DIGIT_SIZE * word.Length + SPACE_DISTANCE;
        }
    }

    private static Vector3 GetPointOnPath(List<Vector3> path, float passedDistance)
    {
        var distanceCounter = 0f;
        for (var i = 1; i < path.Count; i++)
        {
            var edgeDistance = Vector3.Distance(path[i - 1], path[i]);

            if (passedDistance < distanceCounter + edgeDistance)
            {
                var edgePassedDistance = passedDistance - distanceCounter;
                var passedDistanceRatio = edgePassedDistance / edgeDistance;
                return Vector3.Lerp(path[i - 1], path[i], passedDistanceRatio);
            }

            distanceCounter += edgeDistance;
        }

        throw new ArgumentOutOfRangeException(nameof(passedDistance),
            "the passed distance exceeds the given path length");
    }
}