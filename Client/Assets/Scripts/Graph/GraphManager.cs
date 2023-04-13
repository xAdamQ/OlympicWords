using System;
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
            //the first node edge type is irrelevant, because the edge is related to the end node always
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

            path.AddRange(chosenBranch.sequence.Select((n, i) => (n, chosenBranch.edges[i].Type == 0)));
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

    public static (Vector3 position, Vector3 normal) GetPointOnPath(List<Vector3> path, List<Vector3> normals,
        float targetDistance, ref (float distance, int index) edgeCounter)
    {
        for (; edgeCounter.index < path.Count; edgeCounter.index++)
        {
            var edgeLength = Vector3.Distance(path[edgeCounter.index - 1], path[edgeCounter.index]);

            if (targetDistance < edgeCounter.distance + edgeLength)
            {
                var edgePassedDistance = targetDistance - edgeCounter.distance;
                var passedDistanceRatio = edgePassedDistance / edgeLength;
                var poz = Vector3.Lerp(path[edgeCounter.index - 1], path[edgeCounter.index], passedDistanceRatio);
                var normal = Vector3.Lerp(normals[edgeCounter.index - 1], normals[edgeCounter.index],
                    passedDistanceRatio);
                return (poz, normal);
            }

            edgeCounter.distance += edgeLength;
        }

        throw new ArgumentOutOfRangeException(nameof(targetDistance),
            "the passed distance exceeds the given path length");
    }

    const float cutRatio = .45f, cutValue = 1f;

    private static Node CutEdge(Edge edge, int endNode, IList<Node> nodes)
    {
        var traverseEndNode = nodes[endNode];
        var traverseStart = edge.OtherEnd(endNode);
        var traverseStartNode = nodes[traverseStart];

        var edgeVector = traverseEndNode.Position - traverseStartNode.Position;
        var edgeCutRatio = cutValue > edgeVector.magnitude * .5f ? cutRatio : cutValue / edgeVector.magnitude;
        var edgeCutPoint = Vector3.Lerp(traverseStartNode.Position, traverseEndNode.Position, edgeCutRatio);
        var edgeNormal = Vector3.Lerp(traverseStartNode.Normal, traverseEndNode.Normal, edgeCutRatio);

        return new Node
        {
            Position = edgeCutPoint,
            Normal = edgeNormal,
            Type = traverseEndNode.Type,
        };
    }

    public static GraphData SmoothenGraph(GraphData input)
    {
        var res = input.Copy();
        var nodes = res.Nodes;
        var edges = res.Edges;

        var threadLeads = new Queue<(Edge edge, int end)>();
        threadLeads.Enqueue((edges.First(), edges.First().End));
        var visitedEdges = new List<Edge> { edges.First() };

        while (threadLeads.Count > 0)
        {
            var (edge, traverseEnd) = threadLeads.Dequeue();

            var nodeEdges = edges
                .Where(e => (e.Start == traverseEnd || e.End == traverseEnd) && !visitedEdges.Contains(e))
                .ToList();

            nodes[traverseEnd] = CutEdge(edge, traverseEnd, nodes);
            //replace the old node with the new node at the cut position
            //so the old index belongs to the new node, but the old node reference is intact and you can use
            //it's position to calculate the old edges

            foreach (var nextEdge in nodeEdges)
            {
                visitedEdges.Add(nextEdge);
                var newStart = CutEdge(nextEdge, traverseEnd, nodes);
                nodes.Add(newStart);

                if (nextEdge.Start == traverseEnd)
                    nextEdge.Start = nodes.Count - 1;
                else
                    nextEdge.End = nodes.Count - 1;
                //shorten the edge by making it point to the new node

                var connectingEdge = new Edge(traverseEnd, nodes.Count - 1, nextEdge.Type)
                {
                    //the group doesn't matter
                };
                edges.Add(connectingEdge);

                threadLeads.Enqueue((nextEdge, nextEdge.OtherEnd(traverseEnd)));
            }
        }

        return res;
    }

    private static (Vector3 point, Vector3 normal) CutEdge
        (Vector3 start, Vector3 startNormal, Vector3 end, Vector3 endNormal)
    {
        var vector = end - start;
        var cutPercent = cutValue > vector.magnitude * .5f ? cutRatio : cutValue / vector.magnitude;

        var position = Vector3.Lerp(end, start, cutPercent);
        var normal = Vector3.Lerp(endNormal, startNormal, cutPercent);

        return (position, normal);
    }

    public static IEnumerable<(Vector3 position, Vector3 normal, bool isWalkable)> SmoothenPath
        (IEnumerable<(Vector3 position, Vector3 normal, bool isWalkable)> path)
    {
        using var enumerator = path.GetEnumerator();

        enumerator.MoveNext();
        var first = enumerator.Current;
        enumerator.MoveNext();
        var second = enumerator.Current;

        //if we have less than 3 points, return the exact path, even if it was empty
        if (!enumerator.MoveNext())
        {
            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var value in path)
                yield return value;

            yield break;
        }

        yield return first;

        do
        {
            var third = enumerator.Current;

            if (!third.isWalkable || !second.isWalkable)
            {
                yield return second;

                first = second;
                second = third;

                continue;
            }

            var p1 = CutEdge(first.position, first.normal, second.position, second.normal);
            var p2 = CutEdge(third.position, third.normal, second.position, second.normal);
            var p1Full = (p1.point, p1.normal, true);
            var p2Full = (p2.point, p2.normal, true);

            yield return p1Full;
            yield return p2Full;

            //we use this, to have the shorter version of the edge, we calculate the ratio right
            first = p1Full;
            second = third;
        } while (enumerator.MoveNext());

        yield return second;
    }
}