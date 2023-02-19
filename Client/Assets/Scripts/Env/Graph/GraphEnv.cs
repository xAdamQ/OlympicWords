using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class GraphEnv : RootEnv
{
    public new static GraphEnv I;

    private const float
        DIGIT_SIZE = .3f,
        DIGIT_FILL_PERCENT = .8f,
        SPACING_Y = .1f,
        MAX_DIGIT_SIZE = .5f;

    private GameObject[] charObjects;
    [SerializeField] private GraphData[] graphs;

    private GraphData Graph => graphs[chosenGraphIndex];
    private int chosenGraphIndex;

    protected override void Awake()
    {
        base.Awake();
        I = this;
    }

    public override void PrepareRequestedRoomRpc(RoomPrepareResponse response)
    {
        base.PrepareRequestedRoomRpc(response);
        Debug.Log("prepare child called");
    }

    public static (Vector3 poz, Vector3 normal) GetProjectedPoz(Vector3 at, Vector3 normal)
    {
        var rayHit = Physics.Raycast(at + normal * 2, Vector3.down, out var hitInfo, 3, ~6);
        return !rayHit ? (at, normal) : (hitInfo.point + Vector3.up * SPACING_Y, hitInfo.normal);
    }

    public override Vector3 GetCharPozAt(int charIndex, int playerIndex)
    {
        var chr = charObjects[charIndex].transform.GetChild(0);
        var maxBound = chr.GetComponent<MeshRenderer>().bounds.max - new Vector3(.1f, 0, .1f);
        var minBound = chr.GetComponent<MeshRenderer>().bounds.min - new Vector3(.1f, 0, .1f);

        return new Vector3(
            Random.Range(minBound.x, maxBound.x),
            maxBound.y + .1f,
            Random.Range(minBound.z, maxBound.z));
    }

    public override Vector3 GetCharRotAt(int charIndex, int playerIndex)
    {
        return charObjects[charIndex].transform.eulerAngles;
    }
    public override GameObject GetCharObjectAt(int charIndex, int playerIndex)
    {
        return charObjects[charIndex].transform.GetChild(0).gameObject;
    }
    public override IEnumerable<GameObject> GetWordObjects(int wordIndex, int playerIndex)
    {
        return charObjects[WordMap[wordIndex]..WordMap[wordIndex + 1]]
            .Select(g => g.transform.GetChild(0).gameObject);
    }

    /// <summary>
    /// activate/deactivate the word
    /// </summary>
    public void WordState(int wordIndex, bool state)
    {
        var endScale = Vector3.one * (state ? 1 : 0);
        if (state)
        {
            foreach (var wordObject in GetWordObjects(wordIndex, -1))
            {
                wordObject.transform.parent.gameObject.SetActive(true);
                wordObject.transform.DOScale(endScale, .4f);
            }
        }
        else
        {
            foreach (var wordObject in GetWordObjects(wordIndex, -1))
            {
                wordObject.transform.DOScale(endScale, .4f)
                    .OnComplete(() => wordObject.transform.parent.gameObject.SetActive(false));
            }
        }
    }

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

    protected override void SetPlayersInitialPoz()
    {
        var pozPointer = GetCharPozAt(0, -1);
        var rot = GetCharRotAt(0, -1);
        var charObj = GetCharObjectAt(0, -1);
        foreach (var p in Players)
        {
            p.transform.position = pozPointer;
            p.transform.eulerAngles = rot;

            pozPointer += -charObj.transform.parent.forward * .5f;
        }
    }
    /// <summary>
    /// the only difference between generate digit functions in different environment is the transform of the digit until now
    /// </summary>
    protected override void GenerateDigits(System.Random random)
    {
        chosenGraphIndex = random.Next(graphs.Length);

        charObjects = new GameObject[Text.Length];

        var path = GraphManager.GetRandomPath(Graph, random);
        var nodes = path.Select(n => (node: Graph.Nodes[n.node], n.isWalkable)).ToList();
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
        var charIndex = 0;

        for (var w = 0; w < Words.Count; w++)
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
            var totalDigits = Words[w].Length;
            var usedDistance = fullWordLength(Words[w]);
            //there at least the current word in the edge

            var edgesLength = GetEdgeLengths(connectedBranch);
            // var edgesLength = GetEdgeLengths(start, end, arcs);

            while (w + 1 < Words.Count && usedDistance + fullWordLength(Words[w + 1]) <= edgesLength)
            {
                w++;
                usedDistance += fullWordLength(Words[w]);
                totalDigits += Words[w].Length;
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

                var currentWord = Words[globalWordIndex];
                var wordObject = new GameObject[currentWord.Length];

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
                    digitObject.transform.GetChild(0).GetComponent<MeshFilter>().mesh =
                        EnvShared.I.GetDigitMesh(currentDigit);

                    digitObject.transform.localScale = Vector3.one * actualDigitSize * DIGIT_FILL_PERCENT;

                    charObjects[charIndex] = digitObject;
                    wordObject[i] = digitObject;

                    digitStartPoint = digitEndPoint;
                    digitStartNormal = digitEndNormal;

                    charIndex++;
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
                    path = GraphManager.GetRandomPath(Graph, (path[nodeCounter - 1].node, path[nodeCounter].node),
                        random);
                    nodes = path.Select(n => (Graph.Nodes[n.node], n.isWalkable)).ToList();
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

        try
        {
            foreach (var go in charObjects)
            {
                go.transform.GetChild(0).transform.localScale = Vector3.zero;
                go.SetActive(false);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    protected override void ColorFillers(List<(int index, int player)> fillerWords)
    {
        foreach (var (index, player) in fillerWords)
        foreach (var wordObject in GetWordObjects(index, -1))
            wordObject.GetComponent<Renderer>().material = PlayerMats[player];
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