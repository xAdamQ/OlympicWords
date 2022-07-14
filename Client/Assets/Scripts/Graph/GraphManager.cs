using System;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using Wintellect.PowerCollections;
using Random = UnityEngine.Random;

public class GraphManager
{
    public static List<(int node, bool isWalkable)> GetRandomPath(GraphData graphData)
    {
        var nodesEdges = GetNodeEdges(graphData);
        var remainingEdges = graphData.Edges.ToList();
        var path = new List<(int, bool)>();

        var startEdge = remainingEdges.Where(e => e.Type == 0).ToList().GetRandom();
        var finishNode = Random.Range(0, 2) == 0 ? startEdge.Start : startEdge.End;

        var startNode = startEdge.Start == finishNode ? startEdge.End : startEdge.Start;

        path.Add((startNode, startEdge.Type == 0));
        path.Add((finishNode, startEdge.Type == 0));

        //removing start edge will disjoint the graph, and prevent removing the finish branch
        if (getBranches(finishNode).Count == 2)
            removeEdge(startEdge);

        //remove all branches from start node
        getBranches(startNode).ForEach(b => b.edges.ForEach(removeEdge));

        while (true)
        {
            if (remainingEdges.Count == 0)
            {
                Debug.Log("no remaining edges");
                break;
            }

            var branches = getBranches(finishNode);
            if (branches.Count == 0)
            {
                Debug.Log($"node has no branches, remaining edges count: {remainingEdges.Count}");
                break;
            }

            var chosenBranch = branches.GetRandom();

            //delete all branches
            branches.ForEach(b => b.edges.ForEach(removeEdge));

            path.AddRange(chosenBranch.sequence.Select((n, i) => (n, chosenBranch.edges[i].Type == 0)));
            
            
            if (chosenBranch.looping)
            {
                // Debug.Log("ended with loop");
                path.RemoveAt(path.Count - 1);
                // break;
            }

            finishNode = chosenBranch.sequence.Last();
        }

        return path;

        List<(List<Edge> edges, List<int> sequence, bool looping)> getBranches(int node)
        {
            var branches = new List<(List<Edge> edges, List<int> sequence, bool looping)>();
            var branchStarts = nodesEdges[node].ToList();

            while (branchStarts.Count > 0)
            {
                var branchStart = branchStarts[0];
                branchStarts.RemoveAt(0);

                var branchFinishNode = GetOtherEnd(branchStart, node);

                branches.Add((edges: new List<Edge> { branchStart }, new List<int> { branchFinishNode }, looping: false));

                while (nodesEdges[branchFinishNode].Count == 2)
                {
                    var newExtend = nodesEdges[branchFinishNode].First(e => e != branches.Last().edges.Last());
                    //previously first was olay because we delete, now we have to get the other edge

                    branchFinishNode = GetOtherEnd(newExtend, branchFinishNode);

                    branches[^1].edges.Add(newExtend);
                    branches[^1].sequence.Add(branchFinishNode);

                    if (branchFinishNode != node) continue;

                    branches[^1] = (branches[^1].edges, branches[^1].sequence, true);
                    branchStarts.Remove(newExtend);
                    break;
                }
            }

            return branches;
        }

        void removeEdge(Edge edge)
        {
            remainingEdges.Remove(edge);
            nodesEdges[edge.Start].Remove(edge);
            nodesEdges[edge.End].Remove(edge);
        }
    }

    private static int GetOtherEnd(Edge edge, int otherEnd) => edge.Start == otherEnd ? edge.End : edge.Start;

    private static Dictionary<int, List<Edge>> GetNodeEdges(GraphData graphData)
    {
          var  nodeEdges = Enumerable.Range(0, graphData.Nodes.Count).ToDictionary(i => i, _ => new List<Edge>());

        foreach (var edge in graphData.Edges)
        {
            nodeEdges[edge.Start].Add(edge);
            nodeEdges[edge.End].Add(edge);
        }

        return nodeEdges;
    }
}