using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

public class Arc
{
    public Vector2 center, start, end;
    public float r, angle, length, startAngle, segmentsAngle;

    public Arc(Vector2 p0, Vector2 pl, Vector2 pr, float cut)
    {
        segmentsAngle = Vector2.SignedAngle(p0 - pl, p0 - pr) / 2 * Mathf.Deg2Rad;
        var tan = Mathf.Abs(Mathf.Tan(segmentsAngle));

        r = tan * cut;

        start = (pl - p0).normalized * cut + p0;
        end = (pr - p0).normalized * cut + p0;

        center = start + Vector2.Perpendicular((pl - p0) * segmentsAngle.Sign()).normalized * r;

        angle = Mathf.Atan(cut / r) * 2f;
        length = r * angle;
        startAngle = Vector2.SignedAngle(start - center, Vector2.up) * Mathf.Deg2Rad;

        // var step = Vector2.Distance(cutPointL, cutPointR)/10f;
        // var dir = (cutPointR - cutPointL).normalized;
        //
        // for (var i = 0; i <= 10; i++)
        // {
        //     var p = cutPointL + step * i * dir;
        //     var x = p.x;
        //     
        //     var a = 1;
        //     var b = -2 * centerL.y;
        //     var c =
        //         Mathf.Pow(centerL.y, 2)
        //         + Mathf.Pow(x, 2)
        //         - 2 * x * centerL.x
        //         + Mathf.Pow(centerL.x, 2)
        //         - Mathf.Pow(r, 2);
        //
        //     var y1 = (-b + Mathf.Sqrt(Mathf.Pow(b, 2) - 4 * a * c)) / 2 * a;
        //     var y2 = (-b - Mathf.Sqrt(Mathf.Pow(b, 2) - 4 * a * c)) / 2 * a;
        //     
        //     Handles.DrawWireCube(new Vector3(x, 0, y1), Vector3.one * .125f); 
        //     Handles.DrawWireCube(new Vector3(x, 0, y2), Vector3.one * .125f); 
        // }
    }

    public Vector2 GetPointAt(float distance)
    {
        if (distance > length)
            throw new ArgumentOutOfRangeException(nameof(distance), "the given distance is higher than arc lenght");

        var netAngle = distance / r;
        //angle for the small arc

        var finalAngle = startAngle + netAngle * segmentsAngle.Sign();

        return new Vector2(center.x + r * Mathf.Sin(finalAngle), center.y + r * Mathf.Cos(finalAngle));
    }
}

public class CityEnv : EnvBase
{
    private const float DIGIT_SIZE = .7f,
        DIGIT_FILL_PERCENT = .8f,
        SPACE_DISTANCE = 1f,
        MAX_DIGIT_SIZE = 1.5f,
        DIGIT_Y_EXTEND = .13f,
        SPACING_Y = .1f;

    [SerializeField] private GraphData[] cityGraphs;
    private GraphData cityGraph => cityGraphs[chosenGraphIndex];
    private int chosenGraphIndex;

    private GameObject[][] wordObjects;


    private readonly Vector3 digitAddedRotation = Vector3.zero;

    protected override void Awake()
    {
        base.Awake();
        chosenGraphIndex = Random.Range(0, cityGraphs.Length);
    }

    protected override void Start()
    {
        base.Start();


        MyPlayer.MovedADigit += OnMyDigitMoved;

        MyPlayer.MovedAWord += ColorActiveWord;
        MyPlayer.MovedAWord += wordIndex => OnMyDigitMoved(wordIndex, 0);

        ColorActiveWord(0);
        ColorCurrentDigit(0, 0);
    }

    private void OnMyDigitMoved(int wordIndex, int digitIndex)
    {
        ColorCurrentDigit(wordIndex, digitIndex);

        if (digitIndex != 0)
        {
            var digit = GetWordObjects(wordIndex)[digitIndex - 1];
            MinimizeDigit(digit);
        }
    }

    public override GameObject[] GetWordObjects(int wordIndex)
    {
        return wordObjects[wordIndex];
    }

    private void ColorCurrentDigit(int wordIndex, int digitIndex)
    {
        if (digitIndex != 0)
            GetWordObjects(wordIndex)[digitIndex - 1]
                .GetComponent<Renderer>().material = wordHighlightMat;

        if (GetDigitAt(wordIndex, digitIndex) != ' ')
            GetWordObjects(wordIndex)[digitIndex]
                .GetComponent<Renderer>().material = digitHighlightMat;
    }

    private void ColorActiveWord(int wordIndex)
    {
        // if (wordIndex != 0)
        //     foreach (var digit in GetWordObjects(wordIndex - 1))
        //     {
        //         digit.GetComponent<Renderer>().material = BaseMaterial;
        //         digit.layer = 0;
        //     }

        foreach (var digit in GetWordObjects(wordIndex))
        {
            digit.GetComponent<Renderer>().material = wordHighlightMat;
            digit.layer = 7;
        }
    }

    private void MinimizeDigit(GameObject digit)
    {
        // digit.transform.DOScale(Vector3.zero, .7f);

        var digitRenderer = digit.GetComponent<Renderer>();

        digitRenderer.material = fadeMaterial;
        digitRenderer.material.DOFade(.1f, .3f).SetEase(Ease.OutCirc)
            .OnComplete(() => fadeMaterial.color = Color.white);
    }

    public Material fadeMaterial;


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

    /// <summary>
    /// the only difference between generate digit functions in different environment is the transform of the digit until now
    /// </summary>
    protected override void GenerateDigits()
    {
        wordObjects = new GameObject[words.Count][];
        List<(int node, bool isWalkable)> path;
        List<(Node node, bool isWalkable)> nodes;

        path = GraphManager.GetRandomPath(cityGraph);

        nodes = path.Select(n => (cityGraph.Nodes[n.node], n.isWalkable)).ToList();
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

    private static Vector3 GetProjectedPoz(Vector3 at)
    {
        var res = at;
        var rayHit = Physics.Raycast(at + Vector3.up * 2, Vector3.down, out var hitInfo, 3, ~6);

        if (!rayHit) return res;

        res = hitInfo.point + (DIGIT_Y_EXTEND + SPACING_Y) * Vector3.up;

        return res;
    }

    // private static float GetEdgeLengths(Vector2 start, Vector2 end, List<Arc> arcs)
    // {
    //     if (arcs.Count == 0) return Vector3.Distance(start, end);
    //
    //     var totalDistance = Vector3.Distance(start, arcs[0].start);
    //
    //     for (var i = 0; i < arcs.Count - 1; i++)
    //         totalDistance += arcs[i].length + Vector2.Distance(arcs[i].end, arcs[i + 1].start);
    //
    //     totalDistance += arcs[^1].length + Vector3.Distance(arcs[^1].end, end);
    //
    //     return totalDistance;
    // }

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

    // private static Vector2 GetPointOnPath(Vector2 start, Vector2 end, List<Arc> arcs, float passedDistance)
    // {
    //     if (arcs.Count == 0) return Vector3.Lerp(start, end, passedDistance / Vector3.Distance(start, end));
    //
    //     var distanceCounter = 0f;
    //     var isArc = false;
    //     var arcIndex = 0;
    //
    //     while (true)
    //     {
    //         float edgeDistance;
    //
    //         if (isArc)
    //         {
    //             edgeDistance = arcs[arcIndex].length;
    //
    //             if (passedDistance < distanceCounter + edgeDistance)
    //             {
    //                 var edgePassedDistance = passedDistance - distanceCounter;
    //                 return arcs[arcIndex].GetPointAt(edgePassedDistance);
    //             }
    //
    //             arcIndex++;
    //         }
    //         else
    //         {
    //             Vector2 curStart, curEnd;
    //
    //             if (arcIndex == arcs.Count - 1)
    //                 (curStart, curEnd) = (arcs[^1].end, end);
    //             else if (arcIndex == 0)
    //                 (curStart, curEnd) = (start, arcs[0].start);
    //             else
    //                 (curStart, curEnd) = (arcs[arcIndex - 1].end, arcs[arcIndex].start);
    //
    //             edgeDistance = Vector2.Distance(curStart, curEnd);
    //
    //             if (passedDistance < distanceCounter + edgeDistance)
    //             {
    //                 var edgePassedDistance = passedDistance - distanceCounter;
    //                 return Vector2.Lerp(curStart, curEnd, edgePassedDistance / edgeDistance);
    //             }
    //
    //             if (arcIndex == arcs.Count) break;
    //         }
    //
    //         isArc = !isArc;
    //         distanceCounter += edgeDistance;
    //     }
    //
    //     throw new ArgumentOutOfRangeException(nameof(passedDistance),
    //         "the passed distance exceeds the given path length");
    // }

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

    // private static List<Vector3> ShortenPath(List<Vector3> path, float passedDistance)
    // {
    //     var distanceCounter = 0f;
    //     for (var i = 1; i < path.Count; i++)
    //     {
    //         var edgeDistance = Vector3.Distance(path[i - 1], path[i]);
    //
    //         if (passedDistance < distanceCounter + edgeDistance)
    //         {
    //             var edgePassedDistance = passedDistance - distanceCounter;
    //             var passedDistanceRatio = edgePassedDistance / edgeDistance;
    //             var start = Vector3.Lerp(path[i - 1], path[i], passedDistanceRatio);
    //
    //             var res = new List<Vector3>(path.Count - i + 1) { start };
    //             res.AddRange(path.GetRange(i, path.Count - i));
    //
    //             return res;
    //         }
    //
    //         distanceCounter += edgeDistance;
    //     }
    //
    //     throw new ArgumentOutOfRangeException(nameof(passedDistance),
    //         "the passed distance exceeds the given path length");
    // }

    private static IEnumerator<Vector3> ShortenPath(IEnumerator<Vector3> path, float passedDistance)
    {
        var distanceCounter = 0f;

        path.MoveNext();
        var first = path.Current;
        while (path.MoveNext())
        {
            var second = path.Current;

            var edgeDistance = Vector3.Distance(first, second);

            if (passedDistance < distanceCounter + edgeDistance)
            {
                var edgePassedDistance = passedDistance - distanceCounter;
                var passedDistanceRatio = edgePassedDistance / edgeDistance;
                var start = Vector3.Lerp(first, second, passedDistanceRatio);

                yield return start;
                while (path.MoveNext()) yield return path.Current;

                yield break;

                // var res = new List<Vector3>(path.Count - i + 1) { start };
                // res.AddRange(path.GetRange(i, path.Count - i));
                // return res;
            }

            distanceCounter += edgeDistance;
            first = second;
        }

        throw new ArgumentOutOfRangeException(nameof(passedDistance),
            "the passed distance exceeds the given path length");
    }
}