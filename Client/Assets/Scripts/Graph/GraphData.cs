using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using DG.Tweening.Core.Easing;
using PlasticPipe.PlasticProtocol.Client;
using UnityEngine;


[CreateAssetMenu(fileName = "NewGraphData", menuName = "Graph/GraphData")]
public class GraphData : ScriptableObject
{
    public List<Node> Nodes = new();
    public List<Edge> Edges = new();

    // public void SetNavProps()
    // {
    //     // Nodes.ForEach(n => n.Edges = new());
    //
    //     foreach (var edge in Edges)
    //     {
    //         var startNode = Nodes.FirstOrDefault(n => n == edge.Start);
    //         var endNode = Nodes.FirstOrDefault(n => n == edge.End);
    //
    //         Node n = default;
    //
    //         if (startNode == null)
    //         {
    //             Debug.LogWarning($"there's a saved orphan edge: {edge}, with start node {startNode}");
    //             continue;
    //         }
    //
    //         if (endNode == null)
    //         {
    //             Debug.LogWarning($"there's a saved orphan edge: {edge}, with start node {endNode}");
    //             continue;
    //         }
    //
    //         startNode.Edges.Add(edge);
    //         endNode.Edges.Add(edge);
    //     }
    // }
    //
}

// [Serializable]
// public class GraphData
// {
//     [SerializeField] private List<Node> Nodes = new();
//     [SerializeField] private List<Edge> Edges = new();
//
//     public (List<Node>, List<Edge>) GetData()
//     {
//         Nodes.ForEach(n => n.Edges = new());
//
//         foreach (var edge in Edges)
//         {
//             var startNode = Nodes.FirstOrDefault(n => n == edge.Start);
//             var endNode = Nodes.FirstOrDefault(n => n == edge.End);
//
//             Node n = default;
//
//             if (startNode == null)
//             {
//                 Debug.LogWarning($"there's a saved orphan edge: {edge}, with start node {startNode}");
//                 continue;
//             }
//
//             if (endNode == null)
//             {
//                 Debug.LogWarning($"there's a saved orphan edge: {edge}, with start node {endNode}");
//                 continue;
//             }
//
//             startNode.Edges.Add(edge);
//             endNode.Edges.Add(edge);
//         }
//
//         return (Nodes, Edges);
//     }
//
//     //no need to set data because you hold references to the nodes and edges in the graph
//     // public void SetData(List<Node> nodes, List<Edge> edges)
//     // {
//     //     Nodes = nodes;
//     //     Edges = edges;
//     // }
//
//     public GraphData(string name)
//     {
//         Name = name;
//     }
//
//     #region file utils
//
//     public string Name;
//
//     public static GraphData Load(string name)
//     {
//         var dataPath = GetFullPath(name);
//
//         if (!File.Exists(dataPath)) return LoadDefault(name);
//
//         var formatter = new BinaryFormatter();
//         using var stream = new FileStream(dataPath, FileMode.Open);
//
//         return formatter.Deserialize(stream) as GraphData;
//     }
//
//     public void Save()
//     {
//         var formatter = new BinaryFormatter();
//         var stream = new FileStream(GetFullPath(Name), FileMode.Create);
//         formatter.Serialize(stream, this);
//         stream.Close();
//     }
//
//
//     public void Delete()
//     {
//         var dataPath = GetFullPath(Name);
//
//         if (File.Exists(dataPath))
//         {
//             File.Delete(dataPath);
//             Debug.Log("data deleted successfully");
//         }
//         else
//         {
//             Debug.Log("data already deleted");
//         }
//     }
//
//     private static string GetFullPath(string name)
//     {
//         return Application.streamingAssetsPath + "/GraphData/" + name + ".txt";
//     }
//
//     private static GraphData LoadDefault(string name)
//     {
//         return new GraphData(name);
//     }
//
//     #endregion
// }
//

[Serializable]
public class Edge
{
    public Edge(int start, int end, int type)
    {
        Start = start;
        End = end;
        Type = type;
    }

    public int Type, Direction, Group;
    public int Start, End;

    public int RealFinish => Direction switch
    {
        0 => -1,
        1 => End,
        -1 => Start,
        _ => throw new ArgumentOutOfRangeException()
    };

    public int RealStart => Direction switch
    {
        0 => -1,
        1 => Start,
        -1 => End,
        _ => throw new ArgumentOutOfRangeException()
    };

    public bool CanMoveOut(int node)
    {
        var realStart = RealStart;
        return realStart == -1 || realStart == node;
    }

    public bool CanMoveIn(int node)
    {
        var realFinish = RealFinish;
        return realFinish == -1 || realFinish == node;
    }

    public override string ToString()
    {
        return $"edge>> start: {Start} end: {End} type: {Type}";
    }

    public int OtherEnd(int node)
    {
        return Start == node ? End : Start;
    }
}

[Serializable]
public class Node
{
    public bool Equals(Node other)
    {
        if (other is null) return false; //because this cannot be null

        return Position.Equals(other.Position) && Type == other.Type;
    }

    public override bool Equals(object obj)
    {
        return obj is Node other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Type);
    }

    public Vector3 Position, Normal;

    public int Type;

    public override string ToString()
    {
        return $"pos: {Position}\n" +
               $"type: {Type}";
        //WRITING EDGE here will make infinite loop I think
    }

    public static bool operator ==(Node lhs, Node rhs)
    {
        if (lhs is null) return rhs is null;

        return lhs.Equals(rhs);
    }

    public static bool operator !=(Node lhs, Node rhs)
    {
        return !(lhs == rhs);
    }
}

/*

why I don't use structs?
because I want to reference the nodes in the graph, so when I move them they are moved in the graph

why I will use their identifiers instead?
because references are broken when I make undo, but this won't solve the update issue, identifiers are got from hashcode I think
no It will solve the issue because their will be always a single node => Nodes[i]

 */