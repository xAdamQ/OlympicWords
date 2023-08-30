using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public abstract class GraphEnv : RootEnv
{
    public new static GraphEnv I;

    public float PlayerWordSpacing = 0,
        DigitSize = .8f,
        DigitFillPercent = .8f,
        SpacingYSpacingY = .1f,
        MaxDigitSize = 1.1f;
    [FormerlySerializedAs("MinDigitSIze")] public float MinDigitSize = .3f;


    private GameObject[] charObjects;
    [SerializeField] private GraphData[] graphs;

    private GraphData Graph => graphs[chosenGraphIndex];
    private int chosenGraphIndex;

    // public List<(int nodeIndex, Node node, bool isWalkable)> path;
    public List<float> letterDistances;
    public Queue<(float start, float end)> jumperDistances;
    private List<Vector3> pathPositions;
    private List<Vector3> pathNormals;
    private (int, int) linkerEdge;
    public List<(Vector3 position, Vector3 normal, bool isWalkable)> smoothPath;

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

    public (Vector3 poz, Vector3 normal) GetProjectedPoz(Vector3 at, Vector3 normal)
    {
        var rayHit = Physics.Raycast(at + normal * 2, Vector3.down, out var hitInfo, 3, ~6);
        return !rayHit ? (at, normal) : (hitInfo.point + Vector3.up * SpacingYSpacingY, hitInfo.normal);
    }

    public override Vector3 GetCharPozAt(int charIndex, int playerIndex)
    {
        var chr = charObjects[charIndex].transform.GetChild(0);
        var maxBound = chr.GetComponent<MeshRenderer>().bounds.max - new Vector3(.1f, 0, .1f);
        var minBound = chr.GetComponent<MeshRenderer>().bounds.min - new Vector3(.1f, 0, .1f);

        return new Vector3(
            // Random.Range(minBound.x, maxBound.x),
            chr.transform.position.x,
            minBound.y + .1f,
            // Random.Range(minBound.z, maxBound.z)
            chr.transform.position.z
        );
    }

    public override Quaternion GetCharRotAt(int charIndex, int playerIndex)
    {
        return charObjects[charIndex].transform.rotation;
    }
    public override GameObject GetCharObjectAt(int charIndex, int playerIndex)
    {
        var res = charObjects[charIndex];
        return !res ? null : charObjects[charIndex].transform.GetChild(0).gameObject;
        // return charObjects[charIndex].transform.GetChild(0).gameObject;
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
        if (state)
        {
            foreach (var wordObject in GetWordObjects(wordIndex, -1))
            {
                wordObject.transform.parent.gameObject.SetActive(true);
                wordObject.transform.DOScale(0f, .4f).From();
            }
        }
        else
        {
            foreach (var wordObject in GetWordObjects(wordIndex, -1))
            {
                wordObject.transform.DOScale(0f, .4f)
                    .OnComplete(() => wordObject.transform.parent.gameObject.SetActive(false));
            }
        }
    }

    private static float GetEdgesLength(IEnumerable<Vector3> positions)
    {
        using var enumerator = positions.GetEnumerator();
        if (!enumerator.MoveNext()) return 0;

        var totalDistance = 0f;
        var prevNode = enumerator.Current;

        while (enumerator.MoveNext())
        {
            totalDistance += Vector3.Distance(prevNode, enumerator.Current);
            prevNode = enumerator.Current;
        }

        return totalDistance;
    }

    /// <summary>
    /// given a distance on a path, return the node index where the distance is
    /// so the edge (n,n-1) is where the distance lays
    /// edge cut is used to know how much is passed, because you return the station only(node)
    /// new
    /// </summary>
    private static (int node, float newDistance, float lastEdgeDistanceDiff) GetNodeAtDistance(
        IEnumerable<(Vector3 position, bool isWalkable)> positions, float distance)
    {
        using var enumerator = positions.GetEnumerator();
        if (!enumerator.MoveNext()) throw new ArgumentException("positions must not be empty", nameof(positions));

        var totalDistance = 0f;
        var prevNode = enumerator.Current;
        var n = 0;
        var inJumper = false;

        while (enumerator.MoveNext())
        {
            var prevDistance = totalDistance;
            stepForward();

            if (totalDistance >= distance)
            {
                if (!enumerator.Current.isWalkable)
                {
                    inJumper = true;
                    break;
                }

                return (n, distance, distance - prevDistance);
            }
        }

        //if the distance is in a jumper, we need to find the next first walkable node
        if (inJumper)
        {
            while (enumerator.MoveNext())
            {
                var lastEdgeLength = Vector3.Distance(prevNode.position, enumerator.Current.position);
                stepForward();

                if (enumerator.Current.isWalkable)
                    return (n, totalDistance - lastEdgeLength, 0);
            }
        }

        throw new Exception($"the given distance {distance} exceeds the path length {totalDistance}");

        void stepForward()
        {
            totalDistance += Vector3.Distance(prevNode.position, enumerator.Current.position);
            prevNode = enumerator.Current;
            n++;
        }
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
            p.transform.rotation = rot;

            pozPointer += -charObj.transform.parent.forward * .5f;
        }
    }

    /// <summary>
    /// the only difference between generate digit functions in different environment is the transform of the digit until now
    /// </summary>
    protected override void GenerateWords(System.Random random)
    {
        letterDistances = new();
        smoothPath = new();
        pathPositions = new();
        pathNormals = new();
        jumperDistances = new();
        linkerEdge = (-1, -1);
        charObjects = new GameObject[Text.Length];
        chosenGraphIndex = random.Next(graphs.Length);

        GeneratePath(random);

        var (nodeCounter, globalDistance, lastEdgeDistanceDiff) =
            GetNodeAtDistance(smoothPath.Select(n => (n.position, n.isWalkable)), PlayerWordSpacing);
        var edgeCounter = (distance: 0f, index: 1);
        var c = 0;
        var w = 0;

        while (w < Words.Count)
        {
            while (!smoothPath[nodeCounter].isWalkable)
            {
                var start = globalDistance;
                globalDistance += Vector3.Distance(smoothPath[nodeCounter - 1].position, smoothPath[nodeCounter].position);
                jumperDistances.Enqueue((start, globalDistance));
                nextNode();
            }
            //skip all jumper edges

            var branchStart = nodeCounter - 1;
            skipToBranchEnd();
            var branchEnd = nodeCounter;

            if (branchStart == branchEnd)
                Debug.LogError($"abnormal behaviour, branch length if zero, start: {branchStart} end: {branchEnd}");

            var edgesLength = GetEdgesLength(pathPositions.GetRange(branchStart, branchEnd - branchStart + 1)) - lastEdgeDistanceDiff;
            lastEdgeDistanceDiff = 0;
            var totalDigitsCount = 0f;
            var usedDistance = 0f;
            var branchEndWord = w;

            while (branchEndWord < Words.Count && usedDistance + FullWordLength(Words[branchEndWord]) <= edgesLength)
            {
                usedDistance += FullWordLength(Words[branchEndWord]);
                totalDigitsCount += Words[branchEndWord].Length;
                branchEndWord++;
            }
            //try add more words if possible

            if (totalDigitsCount == 0 && usedDistance + MinWordLength(Words[branchEndWord]) <= edgesLength)
            {
                totalDigitsCount += Words[branchEndWord].Length;
                branchEndWord++;
            } //if we can't add any word, try add a single word with min size

            var fillableWords = branchEndWord - w;

            var actualLetterSize = (edgesLength - fillableWords * SPACE_DISTANCE) / totalDigitsCount;
            if (actualLetterSize > MaxDigitSize) actualLetterSize = MaxDigitSize;
            //set digit size
            var finalDistance = globalDistance + edgesLength;

            globalDistance += SPACE_DISTANCE / 2f;


            //here we work on a connected branch without jumper edges
            for (; w < branchEndWord; w++)
            {
                var currentWord = Words[w];
                var wordObject = new GameObject[currentWord.Length];

                var (letterStartPoint, letterStartNormal, _) =
                    GraphManager.GetPointOnPath(pathPositions, pathNormals, globalDistance, ref edgeCounter);

                for (var i = 0; i < currentWord.Length; i++)
                {
                    globalDistance += actualLetterSize;

                    var (letterEndPoint, letterEndNormal, _) =
                        GraphManager.GetPointOnPath(pathPositions, pathNormals, globalDistance, ref edgeCounter);

                    var letterStartProjection = GetProjectedPoz(letterStartPoint, letterStartNormal);
                    var letterStartNormalEndProjection = GetProjectedPoz(letterEndPoint, letterEndNormal);

                    var finalPoz = Vector3.Lerp(letterStartProjection.poz, letterStartNormalEndProjection.poz, .5f);
                    var finalNormal = Vector3.Lerp(letterStartProjection.normal, letterStartNormalEndProjection.normal, .5f);

                    letterDistances.Add(globalDistance);
                    var rot = Quaternion.LookRotation(letterStartNormalEndProjection.poz - letterStartProjection.poz, finalNormal);

                    var letterObject = Instantiate(digitPrefab, finalPoz, rot, transform);

                    var currentLetter = currentWord[i];
                    letterObject.transform.GetChild(0).GetComponent<MeshFilter>().mesh =
                        EnvShared.I.GetLetterMesh(currentLetter);

                    letterObject.transform.localScale = Vector3.one * actualLetterSize * DigitFillPercent;

                    charObjects[c] = letterObject;
                    wordObject[i] = letterObject;

                    letterStartPoint = letterEndPoint;
                    letterStartNormal = letterEndNormal;

                    // letterObject.transform.GetChild(0).transform.localScale = Vector3.zero;
                    letterObject.SetActive(false);

                    c++;
                }

                globalDistance += SPACE_DISTANCE;
            }

            globalDistance = finalDistance;
            //add the final error value, because we fit letters to a part of the chosen branch

            nodeCounter++;
            //all what it do is when the path ends, we regenerate a connected path
        }

        bool nextNode()
        {
            if (nodeCounter == smoothPath.Count - 1)
            {
                Debug.Log($"out of nodes! regenerating");

                GeneratePath(random);

                nodeCounter++;
                return true;
            }

            nodeCounter++;
            return false;
        }

        // (List<Vector3> position, List<Vector3> normals) getConnectedBranch()
        // {
        //     var positions = new List<Vector3> { path[nodeCounter - 1].node.Position };
        //     var normals = new List<Vector3> { path[nodeCounter - 1].node.Normal };
        //     //the first edge is added anyway, this is the first nodes of it and the second in do statement
        //
        //     do
        //     {
        //         positions.Add(path[nodeCounter].node.Position);
        //         normals.Add(path[nodeCounter].node.Normal);
        //
        //         //if we regenerated the path, don't treat the new as a continuous path
        //         if (nextNode()) break;
        //     } while (path[nodeCounter].isWalkable);
        //     //add all walkable edges //edge type follows the second node
        //     return (positions, normals);
        // }

        void skipToBranchEnd()
        {
            do
            {
                //if we regenerated the path, don't treat the new as a continuous path
                if (nextNode()) break;
            } while (smoothPath[nodeCounter].isWalkable);

            nodeCounter--;
            // when we find the edge is not walkable or the graph has ended, the branch end is node before
        }
    }

    private void GeneratePath(System.Random random)
    {
        var newPath = GraphManager.GetRandomPath(Graph, linkerEdge, random)
            .Select(n => (nodeIndex: n.node, node: Graph.Nodes[n.node], n.isWalkable))
            .ToList();

        if (newPath.Count == 0)
        {
            Debug.LogError("Failed to create path");
            GeneratePath(random);
            return;
        }

        while (!newPath.Last().isWalkable)
        {
            if (newPath.Count == 1)
            {
                GeneratePath(random);
                return;
            }

            newPath.RemoveAt(newPath.Count - 1);
        }
        //remove the last jumpers

        linkerEdge = (newPath[^2].nodeIndex, newPath[^1].nodeIndex);
        Debug.Log("the new linker is: " + linkerEdge);

        // path.AddRange(newPath);

        var newSmoothPath = GraphManager.SmoothenPath
            (newPath.Select(n => (n.node.Position, n.node.Normal, n.isWalkable)));
        smoothPath.AddRange(newSmoothPath);

        pathPositions.AddRange(newSmoothPath.Select(n => n.position));
        pathNormals.AddRange(newSmoothPath.Select(n => n.normal));
    }

    private float FullWordLength(string word)
    {
        return DigitSize * word.Length + SPACE_DISTANCE;
    }
    private float MinWordLength(string word)
    {
        return MinDigitSize * word.Length + SPACE_DISTANCE;
    }

    protected override void ColorFillers(List<(int index, int player)> fillerWords)
    {
        foreach (var (index, player) in fillerWords)
            foreach (var wordObject in GetWordObjects(index, -1))
                wordObject.GetComponent<Renderer>().material = PlayerMats[player];
    }
}