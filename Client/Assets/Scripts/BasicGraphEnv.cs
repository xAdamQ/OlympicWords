using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class BasicGraphEnv : EnvBase
{
    public new static BasicGraphEnv I;

    private GameObject[][] wordObjects;
    private readonly Vector3 digitAddedRotation = Vector3.zero;
    [SerializeField] private GraphData[] graphs;

    private GraphData graph => graphs[chosenGraphIndex];
    private int chosenGraphIndex;

    private const float DIGIT_SIZE = .3f,
        DIGIT_FILL_PERCENT = .8f,
        SPACE_DISTANCE = .5f,
        MAX_DIGIT_SIZE = .5f,
        SPACING_Y = .1f;


    protected override void Awake()
    {
        base.Awake();
        I = this;
        chosenGraphIndex = Random.Range(0, graphs.Length);
    }

    public static (Vector3 poz, Vector3 normal) GetProjectedPoz(Vector3 at, Vector3 normal)
    {
        var rayHit = Physics.Raycast(at + normal * 2, Vector3.down, out var hitInfo, 3, ~6);
        return !rayHit ? (at, normal) : (hitInfo.point + Vector3.up * SPACING_Y, hitInfo.normal);
    }

    public override Vector3 GetDigitPozAt(int wordIndex, int digitIndex)
    {
        var digit = wordObjects[wordIndex][digitIndex];
        var maxBound = digit.transform.GetChild(0).GetComponent<MeshRenderer>().bounds.max - new Vector3(.1f, 0, .1f);
        var minBound = digit.transform.GetChild(0).GetComponent<MeshRenderer>().bounds.min - new Vector3(.1f, 0, .1f);

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
        return wordObjects[wordIndex].Select(g => g.transform.GetChild(0).gameObject).ToArray();
    }

    public void WordState(int wordIndex, bool state)
    {
        var endScale = Vector3.one * (state ? 1 : 0);
        if (state)
        {
            foreach (var wordObject in wordObjects[wordIndex])
            {
                wordObject.SetActive(true);
                wordObject.transform.GetChild(0).transform.DOScale(endScale, .4f);
            }
        }
        else
        {
            foreach (var wordObject in wordObjects[wordIndex])
            {
                wordObject.transform.GetChild(0).transform.DOScale(endScale, .4f)
                    .OnComplete(() => wordObject.SetActive(false));
            }
        }
    }

    public override int WordsCount => wordObjects.Length;

    private static float GetEdgeLengths(List<Vector3> nodes)
    {
        var totalDistance = 0f;
        for (var i = 1; i < nodes.Count; i++)
            totalDistance += Vector3.Distance(nodes[i - 1], nodes[i]);

        return totalDistance;
    }

    public static (List<Vector3>, List<Vector3>) SmoothenAngles(List<Vector3> path, List<Vector3> normals)
    {
        if (path.Count <= 2) return (path, normals);

        const float cutRatio = .45f, cutValue = 1f;

        var res = new List<Vector3> { path[0] };
        var resNormals = new List<Vector3> { normals[0] };
        for (var i = 1; i < path.Count - 1; i++)
        {
            var start = path[i - 1] - path[i];
            var end = path[i + 1] - path[i];

            //the cut value is relatively big
            // var realStartCut = cutValue > start.magnitude * .5f ? start.magnitude * cutRatio : cutValue;
            // var realEndCut = cutValue > end.magnitude * .5f ? end.magnitude * cutRatio : cutValue;

            var startCutRatio = cutValue > start.magnitude * .5f ? cutRatio : cutValue / start.magnitude;
            var endCutRatio = cutValue > start.magnitude * .5f ? cutRatio : cutValue / end.magnitude;

            // var startCutPoint = start.normalized * realStartCut + path[i];
            // var endCutPoint = end.normalized * realEndCut + path[i];
            var startCutPoint = Vector3.Lerp(path[i], path[i - 1], startCutRatio);
            var endCutPoint = Vector3.Lerp(path[i], path[i + 1], endCutRatio);
            res.Add(startCutPoint);
            res.Add(endCutPoint);

            var startNormal = Vector3.Lerp(normals[i], normals[i - 1], startCutRatio);
            var endNormal = Vector3.Lerp(normals[i], normals[i + 1], endCutRatio);
            resNormals.Add(startNormal);
            resNormals.Add(endNormal);
        }

        res.Add(path[^1]);
        resNormals.Add(normals[^1]);

        return (res, resNormals);
    }

    /// <summary>
    /// the only difference between generate digit functions in different environment is the transform of the digit until now
    /// </summary>
    protected override void GenerateDigits()
    {
        wordObjects = new GameObject[words.Count][];

        var path = GraphManager.GetRandomPath(graph);
        var nodes = path.Select(n => (node: graph.Nodes[n.node], n.isWalkable)).ToList();
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
            var lastWalkable = nodeCounter;
            while (!nodes[nodeCounter].isWalkable)
                nextNode(lastWalkable);
            //skip all jumper edges

            var connectedBranch = new List<Vector3> { nodes[nodeCounter - 1].node.Position };
            var subPathNormals = new List<Vector3> { nodes[nodeCounter - 1].node.Normal };
            //the first edge is added anyway, this is the first nodes of it and the second in do statement

            do
            {
                connectedBranch.Add(nodes[nodeCounter].node.Position);
                subPathNormals.Add(nodes[nodeCounter].node.Normal);

                nextNode();
                //if we regenerated the path, don't treat the new as a continuous path
                if (nodeCounter == 1) break;
            } while (nodes[nodeCounter].isWalkable);
            //add all walkable edges //edge type follows the second node

            (connectedBranch, subPathNormals) = SmoothenAngles(connectedBranch, subPathNormals);
            // (subPath, subPathNormals) = SmoothenAngles(subPath, subPathNormals);

            var fillableWords = 1;
            var totalDigits = words[w].Length;
            var usedDistance = fullWordLength(words[w]);
            //there at least the current word in the edge

            var edgesLength = GetEdgeLengths(connectedBranch);
            // var edgesLength = GetEdgeLengths(start, end, arcs);

            while (w + 1 < words.Count && usedDistance + fullWordLength(words[w + 1]) <= edgesLength)
            {
                w++;
                usedDistance += fullWordLength(words[w]);
                totalDigits += words[w].Length;
                fillableWords++;
            }
            //try add more words if possible

            var actualDigitSize = (edgesLength - fillableWords * SPACE_DISTANCE) / totalDigits;
            if (actualDigitSize > MAX_DIGIT_SIZE) actualDigitSize = MAX_DIGIT_SIZE;
            //set digit size

            var passedDistance = SPACE_DISTANCE / 2f;

            using var subPathE = connectedBranch.AsEnumerable().GetEnumerator();

            for (var lw = 0; lw < fillableWords; lw++)
            {
                var globalWordIndex = w - (fillableWords - 1) + lw;

                var currentWord = words[globalWordIndex];
                var wordObject = new GameObject[currentWord.Length];
                wordObjects[globalWordIndex] = new GameObject[currentWord.Length];

                var (digitStartPoint, digitStartNormal) =
                    GetPointOnPath(connectedBranch, subPathNormals, passedDistance);

                for (var i = 0; i < currentWord.Length; i++)
                {
                    passedDistance += actualDigitSize;

                    var (digitEndPoint, digitEndNormal) =
                        GetPointOnPath(connectedBranch, subPathNormals, passedDistance);

                    var digitStartProjection = GetProjectedPoz(digitStartPoint, digitStartNormal);
                    var digitEndProjection = GetProjectedPoz(digitEndPoint, digitEndNormal);

                    var finalPoz = Vector3.Lerp(digitStartProjection.poz, digitEndProjection.poz, .5f);
                    var finalRot = Vector3.Lerp(digitStartProjection.normal, digitEndProjection.normal, .5f);

                    var digitObject = Instantiate(digitPrefab, finalPoz,
                        Quaternion.LookRotation(digitEndProjection.poz - digitStartProjection.poz, finalRot),
                        transform);

                    var currentDigit = currentWord[i];
                    digitObject.transform.GetChild(0).GetComponent<MeshFilter>().mesh = GetDigitMesh(currentDigit);

                    digitObject.transform.localScale = Vector3.one * actualDigitSize * DIGIT_FILL_PERCENT;

                    wordObjects[globalWordIndex][i] = digitObject;
                    wordObject[i] = digitObject;

                    digitStartPoint = digitEndPoint;
                    digitStartNormal = digitEndNormal;
                }

                passedDistance += SPACE_DISTANCE;
            }

            if (nodeCounter != 1) nextNode();
            //if the current node is the start node of the graph,
            //this means we didn't use the new graph yet

            void nextNode(int lastWalkable = -1)
            {
                if (nodeCounter == nodes.Count - 1)
                {
                    Debug.Log($"out of nodes! regenerating at {path[nodeCounter].node}");

                    if (lastWalkable != -1) nodeCounter = lastWalkable;
                    path = GraphManager.GetRandomPath(graph, (path[nodeCounter - 1].node, path[nodeCounter].node));
                    nodes = path.Select(n => (graph.Nodes[n.node], n.isWalkable)).ToList();
                    nodeCounter = 1;
                    return;
                }

                nodeCounter++;
            }
        }

        float fullWordLength(string word)
        {
            return DIGIT_SIZE * word.Length + SPACE_DISTANCE;
        }

        foreach (var go in wordObjects.SelectMany(g => g))
        {
            go.transform.GetChild(0).transform.localScale = Vector3.zero;
            go.SetActive(false);
        }
    }

    private static (Vector3, Vector3) GetPointOnPath(List<Vector3> path, List<Vector3> normals, float passedDistance)
    {
        var distanceCounter = 0f;
        for (var i = 1; i < path.Count; i++)
        {
            var edgeDistance = Vector3.Distance(path[i - 1], path[i]);

            if (passedDistance < distanceCounter + edgeDistance)
            {
                var edgePassedDistance = passedDistance - distanceCounter;
                var passedDistanceRatio = edgePassedDistance / edgeDistance;
                var poz = Vector3.Lerp(path[i - 1], path[i], passedDistanceRatio);
                var normal = Vector3.Lerp(normals[i - 1], normals[i], passedDistanceRatio);
                return (poz, normal);
            }

            distanceCounter += edgeDistance;
        }

        throw new ArgumentOutOfRangeException(nameof(passedDistance),
            "the passed distance exceeds the given path length");
    }
}