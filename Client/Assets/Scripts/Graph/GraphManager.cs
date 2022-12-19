using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

// ReSharper disable AccessToModifiedClosure

public static class GraphManager
{
    public static List<(int node, bool isWalkable)> GetRandomPath(GraphData graphData, Random random)
    {
        return GetRandomPath(graphData, (-1, -1), random);
    }

    public static List<(int node, bool isWalkable)> GetRandomPath(GraphData graphData,
        (int start, int end) linkerEdge, Random random)
    {
        var nodesEdges = GetNodeEdges(graphData);
        var remainingEdges = graphData.Edges.ToList();

        Edge startEdge;
        int startNode, finishNode;

        if (linkerEdge == (-1, -1))
        {
            startEdge = remainingEdges.Where(e => e.Type == 0).ToList().GetRandom(random);
            //get random walkable edge
            finishNode = startEdge.RealFinish;
            if (finishNode == -1) //this means the edge is bidirectional 
                finishNode = random.Next(2) == 0 ? startEdge.Start : startEdge.End;
            //if not directed choose random dir, otherwise stick with the dir
            startNode = startEdge.Start == finishNode ? startEdge.End : startEdge.Start;
            //choose the other node as start
        }
        else
        {
            startEdge = nodesEdges[linkerEdge.end]
                .Where(e => e.Start != linkerEdge.start && e.End != linkerEdge.start)
                .ToList().GetRandom(random);
            startNode = linkerEdge.end;
            finishNode = startEdge.OtherEnd(startNode);
        }

        var path = new List<(int, bool)>
        {
            (startNode, startEdge.Type == 0),
            (finishNode, startEdge.Type == 0)
        };

        //removing start edge will disjoint the graph, and prevent removing the finish branch
        if (getBranches(finishNode).Count == 2) removeEdge(startEdge);

        //remove all branches from start node
        getBranches(startNode).ForEach(b => b.edges.ForEach(removeEdge));

        var lastEdge = startEdge;

        while (true)
        {
            if (remainingEdges.Count == 0)
            {
                Debug.Log("no remaining edges");
                break;
            }

            var branches = getBranches(finishNode);

            branches.Where(b => b.looping && !b.edges[0].CanMoveOut(finishNode)).ForEach(b =>
            {
                if (!b.edges.Last().CanMoveOut(finishNode))
                    Debug.Log(
                        $"path with start {b.edges.First()} and end {b.edges.Last()} is a strange loop, full path is" +
                        $" {string.Join(", ", path)}");

                b.edges.Reverse();
                b.sequence.Reverse();
            });
            //reverse looping branches that has reversed order

            var eligibleBranches = branches
                .Where(b => b.edges[0].CanMoveOut(finishNode) &&
                            (graphData.Nodes[finishNode].Type != 1 ||
                             b.edges[0].Group != lastEdge.Group))
                .ToList();

            if (eligibleBranches.Count == 0)
            {
                Debug.Log($"node has no branches, remaining edges count: {remainingEdges.Count}");
                break;
            }

            var chosenBranch = eligibleBranches.GetRandom(random);
            //choose eligible branch in terms of grouping and direction

            lastEdge = chosenBranch.edges.Last();

            branches.ForEach(b => b.edges.ForEach(removeEdge));
            //delete all branches

            path.AddRange(
                chosenBranch.sequence.Select((n, i) => (n, chosenBranch.edges[i].Type == 0)));
            //add chosen sequence to the path

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
            //branch is single thread sequence
            var branchStarts = new List<Edge>(nodesEdges[node]);
            //edges out of the start node

            while (branchStarts.Count > 0)
            {
                var lastExtend = branchStarts[0];
                branchStarts.RemoveAt(0);

                var branchFinishNode = GetOtherEnd(lastExtend, node);

                branches.Add((new List<Edge> { lastExtend }, new List<int> { branchFinishNode },
                    false));


                while (nodesEdges[branchFinishNode].Count(e => e != lastExtend) == 1)
                    //we don't delete edges as we go because this alter the graph and makes issues
                {
                    lastExtend = nodesEdges[branchFinishNode].First(e => e != lastExtend);
                    if (!lastExtend.CanMoveOut(branchFinishNode))
                        break;

                    branchFinishNode = GetOtherEnd(lastExtend, branchFinishNode);

                    branches[^1].edges.Add(lastExtend);
                    branches[^1].sequence.Add(branchFinishNode);

                    if (branchFinishNode != node) continue;

                    //looping, caller should handles loops
                    branches[^1] = (branches[^1].edges, branches[^1].sequence, true);
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

    public static int GetOtherEnd(Edge edge, int otherEnd) =>
        edge.Start == otherEnd ? edge.End : edge.Start;

    public static Dictionary<int, List<Edge>> GetNodeEdges(GraphData graphData)
    {
        var nodeEdges = Enumerable.Range(0, graphData.Nodes.Count)
            .ToDictionary(i => i, _ => new List<Edge>());

        foreach (var edge in graphData.Edges)
        {
            nodeEdges[edge.Start].Add(edge);
            nodeEdges[edge.End].Add(edge);
        }

        return nodeEdges;
    }
}