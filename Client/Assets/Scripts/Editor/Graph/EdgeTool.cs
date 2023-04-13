using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using SystemRandom = System.Random;

public class EdgeTool
{
    private readonly GraphTool gt;
    public EdgeTool(GraphTool gt)
    {
        this.gt = gt;
    }

    private readonly Color[] edgeGroupColors =
    {
        // Color.Lerp(Color.blue, Color.black, 0f),
        Color.blue,
        Color.cyan,
        Color.green,
        Color.yellow,
    };

    public List<int> GetRoad(Edge startEdge)
    {
        var nodesEdges = GraphManager.GetNodeEdges(gt.GraphData);

        var res = getRoad(startEdge.End);
        res.Reverse();
        res.AddRange(getRoad(startEdge.Start));
        //search both direction because you may choose an edge from the center

        Debug.Log(string.Join(", ", res));

        return res;

        List<int> getRoad(int startNode)
        {
            var currentEnd = GraphManager.GetOtherEnd(startEdge, startNode);
            var lastExtend = startEdge;

            var road = new List<int> { currentEnd };
            while (nodesEdges[currentEnd].Count(e => e != lastExtend) == 1)
            {
                lastExtend = nodesEdges[currentEnd].First(e => e != lastExtend);

                currentEnd = GraphManager.GetOtherEnd(lastExtend, currentEnd);
                road.Add(currentEnd);

                if (currentEnd == startNode)
                {
                    Debug.LogWarning("you directed a looping branch");
                    break;
                }
            }

            return road;
        }
    }

    private const float WIDTH = .15f, CAP_SIZE = .1f;

    public void DrawEdges()
    {
        Handles.zTest = CompareFunction.LessEqual;
        // Handles.zTest = CompareFunction.Always;
        Handles.color = Color.white;


        for (var i = 0; i < gt.Edges.Count; i++)
        {
            var edge = gt.Edges[i];

            // Handles.color = edgeGroupColors[edge.Group];    
            // Handles.DrawLine(gt.Nodes[edge.Start].Position, gt.Nodes[edge.End].Position, 5f);
            DrawMeshLine(edge);

            if (gt.viewMode) continue;

            DrawDirectionCap(edge, ref i);
            //the counter is changes in case of edge deletion
        }
    }

    public void DrawMeshLine(Edge edge)
    {
        Handles.zTest = CompareFunction.Less;
        Handles.color = edgeGroupColors[edge.Group];

        var middleNormal = Vector3.Lerp(gt.Nodes[edge.Start].Normal, gt.Nodes[edge.End].Normal, .5f);
        var direction = (gt.Nodes[edge.End].Position - gt.Nodes[edge.Start].Position).normalized;
        var widthDir = Vector3.Cross(middleNormal, direction);

        var color = edgeGroupColors[edge.Group];
        var width = WIDTH;
        if (edge.Type == 1)
        {
            width = .2f;
            color -= Color.white * .5f;
        }


        var upLeft = gt.Nodes[edge.Start].Position + widthDir * width;
        var upRight = gt.Nodes[edge.End].Position + widthDir * width;
        var downLeft = gt.Nodes[edge.Start].Position - widthDir * width;
        var downRight = gt.Nodes[edge.End].Position - widthDir * width;

        Handles.DrawSolidRectangleWithOutline(new[] { downLeft, upLeft, upRight, downRight }, color, color);

        //draw caps on the corners
        // Handles.DrawLine(gt.Nodes[edge.End].Position, gt.Nodes[edge.End].Position + widthDir * .5f, 3f);
        // Handles.color = Color.black;
        // Handles.Button(downLeft, Quaternion.identity, .1f, .1f, Handles.SphereHandleCap);
        // Handles.color = Color.white;
        // Handles.Button(upLeft, Quaternion.identity, .1f, .1f, Handles.SphereHandleCap);
        // Handles.color = Color.blue;
        // Handles.Button(downRight, Quaternion.identity, .1f, .1f, Handles.SphereHandleCap);
        // Handles.color = Color.red;
        // Handles.Button(upRight, Quaternion.identity, .1f, .1f, Handles.SphereHandleCap);
    }

    /// <summary>
    /// contains direction click functions
    /// </summary>
    private void DrawDirectionCap(Edge edge, ref int edgeCounter)
    {
        Handles.zTest = CompareFunction.Always;
        Handles.color = Color.red;
        // edgeGroupColors[edge.Group] + new Color(.4f, .4f, .4f, 0f);

        Quaternion capDir;
        Handles.CapFunction capFunction;
        if (edge.Direction != 0)
        {
            var edgeVector = gt.Nodes[edge.End].Position - gt.Nodes[edge.Start].Position;
            capDir = Quaternion.LookRotation(edgeVector.normalized * edge.Direction);
            capFunction = Handles.ConeHandleCap;
        }
        else
        {
            capDir = Quaternion.identity;
            capFunction = Handles.CubeHandleCap;
        }


        var capPoz = Vector3.Lerp(gt.Nodes[edge.Start].Position, gt.Nodes[edge.End].Position, .3f);
        var capSize = gt.GetRelativeCapSize(capPoz, CAP_SIZE);
        var dirClicked = Handles.Button(capPoz, capDir, capSize, capSize, capFunction);

        if (!dirClicked) return;

        DirectionClicked(edge, ref edgeCounter);
    }

    private void DirectionClicked(Edge edge, ref int edgeCounter)
    {
        if (Event.current.control)
        {
            gt.startNode = -1;
            gt.Edges.Remove(gt.Edges[edgeCounter]);
            edgeCounter--;
        }
        else if (Event.current.shift)
        {
            edge.Group++;
            edge.Group %= edgeGroupColors.Length;
        }
        else
        {
            var relativeDir = (edge.Direction + 2) % 3 - 1;
            var road = GetRoad(edge);

            for (var j = 1; j < road.Count; j++)
            {
                var start = road[j - 1];
                var end = road[j];

                var normalEdge =
                    gt.GraphData.Edges.FirstOrDefault(e => e.Start == start && e.End == end);
                if (normalEdge is not null) normalEdge.Direction = relativeDir;
                var reverseEdge =
                    gt.GraphData.Edges.FirstOrDefault(e => e.End == start && e.Start == end);
                if (reverseEdge is not null) reverseEdge.Direction = -relativeDir;

                if (normalEdge is not null && reverseEdge is not null)
                    Debug.LogWarning("ATTENTION, is that possible?");
            }
        }
        //direction button
    }

    private List<(int node, bool isWalkable)> algoPath;
    private List<Edge> remainingAlgoEdges;

    public void CreateRandomPath()
    {
        var r = new SystemRandom(Random.Range(0, 99999));
        algoPath = GraphManager.GetRandomPath(gt.GraphData, r);
    }

    public void DrawAlgoPath()
    {
        Handles.zTest = CompareFunction.Always;

        if (algoPath is null) return;

        for (var i = 0; i < algoPath.Count - 1; i++)
        {
            Handles.color = Color.Lerp(Color.white, Color.magenta, i / (float)algoPath.Count);
            Handles.DrawLine(gt.Nodes[algoPath[i].node].Position, gt.Nodes[algoPath[i + 1].node].Position, 15f);

            // var e = gt.Edges.First(e => (e.Start == algoPath[i].node && e.End == algoPath[i + 1].node) ||
            //                             (e.End == algoPath[i].node && e.Start == algoPath[i + 1].node));
            // DrawMeshLine(e);
        }

        if (remainingAlgoEdges == null) return;

        Handles.color = Color.cyan;
        foreach (var edge in remainingAlgoEdges)
            Handles.DrawLine(gt.Nodes[edge.Start].Position, gt.Nodes[edge.End].Position, 10f);
    }

    // public void DrawSmoothPath()
    // {
    //     if (gt.smoothLevel <= 0) return;
    //
    //     Handles.zTest = CompareFunction.Always;
    //     Handles.color = Color.white;
    //
    //     var positions = gt.Nodes.Select(n => n.Position).ToList();
    //     var normals = gt.Nodes.Select(n => n.Normal).ToList();
    //
    //     var smoothPath = GraphEnv.SmoothenAngles(positions, normals).Item1;
    //
    //     for (var i = 0; i < smoothPath.Count - 2; i++)
    //     {
    //         Handles.DrawLine(smoothPath[i], smoothPath[i + 1], 15f);
    //     }
    // }

    #region old
    //draw up edge
    // if (AlgoMode)
    // {
    //     var lastEdge = Edges.Last();
    //     var dir = (Nodes[lastEdge.End].Position - Nodes[lastEdge.Start].Position).normalized;
    //
    //     var center = Vector3.Lerp(Nodes[lastEdge.Start].Position, Nodes[lastEdge.End].Position, .5f);
    //
    //     var upDir = Vector3.Cross(dir, Vector3.forward).normalized;
    //
    //     Handles.DrawLine(center, upDir * 10, 3f);
    // }

    // Handles.color = Color.magenta;
    //
    // // AlgoPath = GraphManager.GetRandomPath(GraphData);
    // var nodes = AlgoPath.Select(n => Nodes[n.node]).ToList();
    //
    // var positions = nodes.Select(n => n.Position).ToList();
    // var normals = nodes.Select(n => n.Normal).ToList();
    //
    // var smoothPath = BasicGraphEnv.SmoothenAngles(positions, normals).Item1;
    //
    // for (var i = 0; i < smoothPath.Count - 1; i++)
    //     Handles.DrawLine(smoothPath[i], smoothPath[i + 1]);


    // var arc =  GetArc(Nodes[^2].Position.TakeXZ(), Nodes[^1].Position.TakeXZ(), Nodes[^3].Position.TakeXZ(), 1.5f);
    // for (var i = 0; i < 10; i++)
    // {
    //     Handles.color = Color.Lerp(Color.blue, Color.red, i/10f); 
    //     
    //     var ax = center.x + r * Mathf.Sin(36 * i * Mathf.Deg2Rad);
    //     var ay = center.y + r * Mathf.Cos(36 * i * Mathf.Deg2Rad);
    //
    //     Handles.DrawWireCube( new Vector3(ax, 0, ay), Vector3.one *.125f);
    // }


    // var endAngle = arc.startAngle + arc.angle;
    // var endAngle = Vector2.SignedAngle(arc.end - arc.center, Vector2.up) * Mathf.Deg2Rad;
    // var anglePointer = arc.startAngle;
    // var angleStep = (endAngle - arc.startAngle)/10f;
    // for (var i = 0; i < 10; i++)
    // {
    //     Handles.color = Color.Lerp(Color.blue, Color.red, i/10f); 
    //     
    //     var ax = arc.center.x + arc.r * Mathf.Sin(anglePointer);
    //     var ay = arc.center.y + arc.r * Mathf.Cos(anglePointer);
    //
    //     Handles.DrawWireCube( new Vector3(ax, 0, ay), Vector3.one *.05f);
    //
    //     anglePointer += angleStep;
    // }

    // var p1 = arc.GetPointAt(arc.length * .3f);
    // var p2 = arc.GetPointAt(arc.length * .5f);
    // var p3 = arc.GetPointAt(arc.length * .7f);
    // var p4 = arc.GetPointAt(arc.length * .8f);
    //
    // Handles.DrawWireCube(p1.XYInXZ(),  Vector3.one*.1f);
    // Handles.DrawWireCube(p2.XYInXZ(),  Vector3.one*.1f);
    // Handles.DrawWireCube(p3.XYInXZ(),  Vector3.one*.1f);
    // Handles.DrawWireCube(p4.XYInXZ(),  Vector3.one*.1f);
    #endregion
}