using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using Wintellect.PowerCollections;

public class Segment : IComparable<Segment>
{
    public Point left, right;

    public float Y => left.Position.y;


    public int CompareTo(Segment other)
    {
        return -Y.CompareTo(other.Y);
    }

    public override string ToString()
    {
        return left + " - " + right;
    }
}

public class Point
{
    public Segment Segment;
    public Vector2 Position;
    public bool IsLeft;
}

// internal struct Event
// {
//     public float x, y;
//     public bool isLeft;
//     public float index;
//
//
//     // This is for maintaining the order in set.
//     public static bool operator <(Event a, Event b)
//     {
//         if (a.y == b.y) return a.x < b.x;
//         return a.y < b.y;
//     }
//
//     // This is for maintaining the order in set.
//     public static bool operator >(Event a, Event b)
//     {
//         if (a.y == b.y) return a.x > b.x;
//         return a.y > b.y;
//     }
// }

public class MakeGraph : MonoBehaviour
{
    [SerializeField] private LineRenderer[] SceneLines;
    [SerializeField] private float MinLineSpacing;

    /// <summary>
    /// set right/left point based on their x
    /// extend them to an error value
    /// </summary>
    /// <returns></returns>
    private List<Segment> MakeSegments()
    {
        var segments = new List<Segment>();
        foreach (var sceneLine in SceneLines)
        {
            var segment = new Segment();

            var ends = (sceneLine.GetPosition(0).TakeXZ() + sceneLine.transform.position.TakeXZ(), sceneLine.GetPosition(1).TakeXZ() + sceneLine.transform.position.TakeXZ());
            var left = ends.Item1.x < ends.Item2.x ? ends.Item1 : ends.Item2;
            var right = ends.Item1.x >= ends.Item2.x ? ends.Item1 : ends.Item2;

            var dirL = (left - right).normalized;
            var dirR = (right - left).normalized;

            var extendedL = new Point
            {
                Position = left + dirL * MinLineSpacing,
                Segment = segment,
                IsLeft = true,
            };
            var extendedR = new Point
            {
                Position = right + dirR * MinLineSpacing,
                Segment = segment,
                IsLeft = false,
            };

            segment.left = extendedL;
            segment.right = extendedR;

            segments.Add(segment);
        }

        return segments;
    }

    public void Start()
    {
        var segments = MakeSegments();
        var intersections =
            from master in segments
            from slave in segments
            where master != slave && DoIntersect(master, slave)
            select SegmentsIntersectionPoint(master, slave);

        var remainingIntersections = intersections.ToList();
        var reducedIntersections = new List<Vector2>();
        while (remainingIntersections.Count > 0)
        {
            var nearPoints = remainingIntersections.Where(i => Vector2.Distance(i, remainingIntersections[0]) < MinLineSpacing).ToList();
            //including self

            remainingIntersections.RemoveAll(i => nearPoints.Contains(i));
            reducedIntersections.Add(nearPoints.Sum() / nearPoints.Count);
        }

        foreach (var intersection in intersections)
        {
            GameObject.CreatePrimitive(PrimitiveType.Sphere).transform.position = new Vector3(intersection.x, .5f, intersection.y);
        }
    }

    public void SweepLine1()
    {
        //         var segments = MakeSegments();
        //
        // var eventsEnumerable = segments.Select(s => s.left).Concat(segments.Select(s => s.right));
        // var events = new OrderedSet<Point>(eventsEnumerable);
        // //todo test x ordering here
        // //right/left points ordered by x
        //
        // var intersections = new List<Point>();
        //
        // var Sweeping = new OrderedSet<Segment>();
        //
        // while (events.Count > 0)
        // {
        //     var e = events.RemoveFirst();
        //     var (upperSegment, lowerSegment) = Sweeping.DirectUpperAndLower(e.Segment);
        //     //get next and prev segments, if any of them doesn't exist return null
        //
        //     if (e.IsLeft)
        //     {
        //         var seg = e.Segment;
        //         Sweeping.Add(seg);
        //
        //         if (upperSegment != null && DoIntersect(e.Segment, upperSegment))
        //         {
        //             var intersectionPoint = SegmentsIntersectionPoint(upperSegment, e.Segment);
        //                 events.Add(new Event(intersectionPoint, upperSegment, currentSegment,
        //                     Enums.EventType.Intersection));
        //         }
        //     }
        // }
        //
        // // var events = new List<Event>();
        // // for (var i = 0; i < segments.Count; i++)
        // // {
        // //     var segment = segments[i];
        // //     events.Add(new Event
        // //     {
        // //         x = segment.left.x,
        // //         y = segment.left.y,
        // //         isLeft = true,
        // //         index = i,
        // //     });
        // //     events.Add(new Event
        // //     {
        // //         x = segment.right.x,
        // //         y = segment.right.y,
        // //         isLeft = false,
        // //         index = i,
        // //     });
        // // }
        // // //create right/left events
        // //
        // // events.OrderBy(e => e.x).ToList();
        // // //sort events on x coordinates
        //
        // //todo set comparer here
        //
        // for (var i = 0; i < events.Count; i++)
        // {
        //     var curr = events[i];
        //
        //     if (curr.IsLeft)
        //     {
        //         Sweeping.Add(curr.Position, curr);
        //
        //         //todo validate this
        //         var upper = Sweeping.GetAdjacent(curr.Position, 1);
        //         if (DoIntersect(upper.Segment, curr.Segment))
        //         {
        //             // events.Add();
        //         }
        //     }
        //     else
        //     {
        //     }
        // }
    }

    private static int VerticalComparison(Point p1, Point p2)
    {
        var v1 = p1.Position;
        var v2 = p2.Position;

        if (v1.x == v2.x && v1.y == v2.y) return 0;

        if (v1.y == v2.y)
        {
            if (v1.x < v2.x) return -1;
            return 1;
        }

        if (v1.y < v2.y) return -1;
        return 1;
    }


    public class VerticalComparer : IComparer<Vector2>
    {
        public int Compare(Vector2 v1, Vector2 v2)
        {
            if (v1.x == v2.x && v1.y == v2.y) return 0;

            if (v1.y == v2.y)
            {
                if (v1.x < v2.x) return -1;
                return 1;
            }

            if (v1.y < v2.y) return -1;
            return 1;
        }
    }

    public class VerticalComparerPoint : IComparer<Point>
    {
        public int Compare(Point p1, Point p2)
        {
            var v1 = p1.Position;
            var v2 = p2.Position;

            if (v1.x == v2.x && v1.y == v2.y) return 0;

            if (v1.y == v2.y)
            {
                if (v1.x < v2.x) return -1;
                return 1;
            }

            if (v1.y < v2.y) return -1;
            return 1;
        }
    }

    private static int orientation(Point p, Point q, Point r)
    {
        var val = (q.Position.y - p.Position.y) * (r.Position.x - q.Position.x) -
                  (q.Position.x - p.Position.x) * (r.Position.y - q.Position.y);

        if (val == 0) return 0; // collinear

        return val > 0 ? 1 : 2; // clock or counter-clock wise
    }

    private static float Slope(Segment segment)
    {
        return (segment.right.Position.y - segment.left.Position.y) / (segment.right.Position.x - segment.left.Position.x);
    }

    private static bool DoIntersect(Segment s1, Segment s2)
    {
        Point p1 = s1.left, q1 = s1.right, p2 = s2.left, q2 = s2.right;

        // Find the four orientations needed for general and special cases
        var o1 = orientation(p1, q1, p2);
        var o2 = orientation(p1, q1, q2);
        var o3 = orientation(p2, q2, p1);
        var o4 = orientation(p2, q2, q1);

        // General case
        if (o1 != o2 && o3 != o4)
            return true;

        // Special Cases
        // p1, q1 and p2 are collinear and p2 lies on segment p1q1
        if (o1 == 0 && onSegment(p1, p2, q1)) return true;

        // p1, q1 and q2 are collinear and q2 lies on segment p1q1
        if (o2 == 0 && onSegment(p1, q2, q1)) return true;

        // p2, q2 and p1 are collinear and p1 lies on segment p2q2
        if (o3 == 0 && onSegment(p2, p1, q2)) return true;

        // p2, q2 and q1 are collinear and q1 lies on segment p2q2
        if (o4 == 0 && onSegment(p2, q1, q2)) return true;

        return false; // Doesn't fall in any of the above cases
    }


    // Given three collinear points p, q, r, the function checks if point q lies on line segment 'pr'
    static bool onSegment(Point p, Point q, Point r)
    {
        return q.Position.x <= Mathf.Max(p.Position.x, r.Position.x) && q.Position.x >= Mathf.Min(p.Position.x, r.Position.x) &&
               q.Position.y <= Mathf.Max(p.Position.y, r.Position.y) && q.Position.y >= Mathf.Min(p.Position.y, r.Position.y);
    }

    public static Vector2 SegmentsIntersectionPoint(Segment segment1, Segment segment2)
    {
        float m;
        float c;
        float x;
        //y=mx+c
        //m1x+c1 = m2x+c2
        //x = (c2-c1)/(m1-m2)

        if (segment1.left.Position.x != segment1.right.Position.x && segment2.left.Position.x != segment2.right.Position.x)
            //non of the segments are horizontal
        {
            var m1 = Slope(segment1);
            var m2 = Slope(segment2);
            var c1 = segment1.left.Position.y - m1 * segment1.left.Position.x;
            var c2 = segment2.left.Position.y - m2 * segment2.left.Position.x;

            m = m1;
            c = c1;
            x = (c2 - c1) / (m1 - m2);
        }
        else if (segment1.left.Position.x != segment1.right.Position.x)
            //segment1 is horizontal
        {
            m = Slope(segment2);
            c = segment2.left.Position.y - m * segment2.left.Position.x;
            x = segment1.left.Position.x;
        }
        else
        {
            //segment2 is horizontal
            m = Slope(segment1);
            c = segment1.left.Position.y - m * segment1.left.Position.x;
            x = segment2.left.Position.x;
        }

        var y = m * x + c;

        return new Vector2(x, y);
    }
}


/*
 int orientation(Point p, Point q, Point r)
{
// See https://www.geeksforgeeks.org/orientation-3-ordered-points/
// for details of below formula.
int val = (q.y - p.y) * (r.x - q.x) -
          (q.x - p.x) * (r.y - q.y);

if (val == 0) return 0;  // collinear

return (val > 0)? 1: 2; // clock or counterclock wise
}
 
bool doIntersect(Segment s1, Segment s2)
{
Point p1 = s1.left, q1 = s1.right, p2 = s2.left, q2 = s2.right;

// Find the four orientations needed for general and
// special cases
int o1 = orientation(p1, q1, p2);
int o2 = orientation(p1, q1, q2);
int o3 = orientation(p2, q2, p1);
int o4 = orientation(p2, q2, q1);

// General case
if (o1 != o2 && o3 != o4)
    return true;

// Special Cases
// p1, q1 and p2 are collinear and p2 lies on segment p1q1
if (o1 == 0 && onSegment(p1, p2, q1)) return true;

// p1, q1 and q2 are collinear and q2 lies on segment p1q1
if (o2 == 0 && onSegment(p1, q2, q1)) return true;

// p2, q2 and p1 are collinear and p1 lies on segment p2q2
if (o3 == 0 && onSegment(p2, p1, q2)) return true;

 // p2, q2 and q1 are collinear and q1 lies on segment p2q2
if (o4 == 0 && onSegment(p2, q1, q2)) return true;

return false; // Doesn't fall in any of the above cases
}

  
 */