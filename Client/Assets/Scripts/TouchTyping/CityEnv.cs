using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
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
//
// public class CityEnv : BasicGraphEnv
// {
//     // private static float GetEdgeLengths(Vector2 start, Vector2 end, List<Arc> arcs)
//     // {
//     //     if (arcs.Count == 0) return Vector3.Distance(start, end);
//     //
//     //     var totalDistance = Vector3.Distance(start, arcs[0].start);
//     //
//     //     for (var i = 0; i < arcs.Count - 1; i++)
//     //         totalDistance += arcs[i].length + Vector2.Distance(arcs[i].end, arcs[i + 1].start);
//     //
//     //     totalDistance += arcs[^1].length + Vector3.Distance(arcs[^1].end, end);
//     //
//     //     return totalDistance;
//     // }
//
//
//
//     // private static Vector2 GetPointOnPath(Vector2 start, Vector2 end, List<Arc> arcs, float passedDistance)
//     // {
//     //     if (arcs.Count == 0) return Vector3.Lerp(start, end, passedDistance / Vector3.Distance(start, end));
//     //
//     //     var distanceCounter = 0f;
//     //     var isArc = false;
//     //     var arcIndex = 0;
//     //
//     //     while (true)
//     //     {
//     //         float edgeDistance;
//     //
//     //         if (isArc)
//     //         {
//     //             edgeDistance = arcs[arcIndex].length;
//     //
//     //             if (passedDistance < distanceCounter + edgeDistance)
//     //             {
//     //                 var edgePassedDistance = passedDistance - distanceCounter;
//     //                 return arcs[arcIndex].GetPointAt(edgePassedDistance);
//     //             }
//     //
//     //             arcIndex++;
//     //         }
//     //         else
//     //         {
//     //             Vector2 curStart, curEnd;
//     //
//     //             if (arcIndex == arcs.Count - 1)
//     //                 (curStart, curEnd) = (arcs[^1].end, end);
//     //             else if (arcIndex == 0)
//     //                 (curStart, curEnd) = (start, arcs[0].start);
//     //             else
//     //                 (curStart, curEnd) = (arcs[arcIndex - 1].end, arcs[arcIndex].start);
//     //
//     //             edgeDistance = Vector2.Distance(curStart, curEnd);
//     //
//     //             if (passedDistance < distanceCounter + edgeDistance)
//     //             {
//     //                 var edgePassedDistance = passedDistance - distanceCounter;
//     //                 return Vector2.Lerp(curStart, curEnd, edgePassedDistance / edgeDistance);
//     //             }
//     //
//     //             if (arcIndex == arcs.Count) break;
//     //         }
//     //
//     //         isArc = !isArc;
//     //         distanceCounter += edgeDistance;
//     //     }
//     //
//     //     throw new ArgumentOutOfRangeException(nameof(passedDistance),
//     //         "the passed distance exceeds the given path length");
//     // }
//
//
//
//     // private static List<Vector3> ShortenPath(List<Vector3> path, float passedDistance)
//     // {
//     //     var distanceCounter = 0f;
//     //     for (var i = 1; i < path.Count; i++)
//     //     {
//     //         var edgeDistance = Vector3.Distance(path[i - 1], path[i]);
//     //
//     //         if (passedDistance < distanceCounter + edgeDistance)
//     //         {
//     //             var edgePassedDistance = passedDistance - distanceCounter;
//     //             var passedDistanceRatio = edgePassedDistance / edgeDistance;
//     //             var start = Vector3.Lerp(path[i - 1], path[i], passedDistanceRatio);
//     //
//     //             var res = new List<Vector3>(path.Count - i + 1) { start };
//     //             res.AddRange(path.GetRange(i, path.Count - i));
//     //
//     //             return res;
//     //         }
//     //
//     //         distanceCounter += edgeDistance;
//     //     }
//     //
//     //     throw new ArgumentOutOfRangeException(nameof(passedDistance),
//     //         "the passed distance exceeds the given path length");
//     // }
//
//     private static IEnumerator<Vector3> ShortenPath(IEnumerator<Vector3> path, float passedDistance)
//     {
//         var distanceCounter = 0f;
//
//         path.MoveNext();
//         var first = path.Current;
//         while (path.MoveNext())
//         {
//             var second = path.Current;
//
//             var edgeDistance = Vector3.Distance(first, second);
//
//             if (passedDistance < distanceCounter + edgeDistance)
//             {
//                 var edgePassedDistance = passedDistance - distanceCounter;
//                 var passedDistanceRatio = edgePassedDistance / edgeDistance;
//                 var start = Vector3.Lerp(first, second, passedDistanceRatio);
//
//                 yield return start;
//                 while (path.MoveNext()) yield return path.Current;
//
//                 yield break;
//
//                 // var res = new List<Vector3>(path.Count - i + 1) { start };
//                 // res.AddRange(path.GetRange(i, path.Count - i));
//                 // return res;
//             }
//
//             distanceCounter += edgeDistance;
//             first = second;
//         }
//
//         throw new ArgumentOutOfRangeException(nameof(passedDistance),
//             "the passed distance exceeds the given path length");
//     }
// }