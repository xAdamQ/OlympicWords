using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

// ReSharper disable AccessToModifiedClosure

[EditorTool("GraphTool")]
internal class GraphTool : EditorTool
{
    // Serialize this value to set a default value in the Inspector.
    [SerializeField] private Texture2D icon;

    private GraphData graphData;

    private bool pointerEnabled, drawMode, viewMode, algoMode, checkMode;


    private List<Node> Nodes => graphData.Nodes;
    private List<Edge> Edges => graphData.Edges;

    private int startNode;

    public static GraphTool I;

    public override GUIContent toolbarIcon => new()
    {
        image = icon,
        text = "GraphTool",
        tooltip = "GraphTool",
    };

    private void OnEnable()
    {
        I = this;

        startNode = -1;

        Load();
    }

    private List<Vector3> smoothGraph;

    public override void OnToolGUI(EditorWindow window)
    {
        if (window is not SceneView or null || !ToolManager.IsActiveTool(this) || graphData is null) return;

        var (spawnPoz, spawnNormal) = GetSpawnPosition();

        Handles.zTest = CompareFunction.Less;

        switch (Event.current.type)
        {
            case EventType.MouseDown
                when Event.current.button == 0 && Event.current.shift && drawMode:
                SpawnNode(spawnPoz, spawnNormal, 0);
                break;
            case EventType.MouseDown
                when Event.current.button == 1 && Event.current.shift && drawMode:
                SpawnNode(spawnPoz, spawnNormal, 1);
                break;
            case EventType.Layout:
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
                break;
        }

        if (Event.current.type == EventType.KeyDown)
        {
            switch (Event.current.keyCode)
            {
                case KeyCode.P:
                    pointerEnabled = !pointerEnabled;
                    break;
                case KeyCode.Escape:
                    startNode = -1;
                    break;
                case KeyCode.S:
                    Save();
                    break;
                case KeyCode.V:
                    viewMode = !viewMode;
                    break;
                case KeyCode.C:
                    Validate();
                    break;
                case KeyCode.L:
                    Load();
                    break;
                case KeyCode.D:
                    drawMode = !drawMode;
                    break;
                // case KeyCode.J:
                //     jumperDraw = !jumperDraw;
                //     break;
                case KeyCode.G:
                    algoMode = !algoMode;
                    SceneView.lastActiveSceneView.ShowNotification(new GUIContent("AlgoMode " + algoMode), .1f);
                    break;
                case KeyCode.O:
                    EditorCoroutineUtility.StartCoroutineOwnerless(MassTestPaths());
                    break;
                case KeyCode.X:
                    CreateRandomPath();
                    break;
                case KeyCode.Backslash:
                    DeleteOrphans();
                    break;
                case KeyCode.H:
                    smoothLevel++;
                    Debug.Log(smoothLevel);
                    break;
                case KeyCode.F:
                    smoothLevel--;
                    if (smoothLevel < 0) smoothLevel = 0;
                    Debug.Log(smoothLevel);
                    break;
                case KeyCode.U:
                    Nodes.ForEach(n => n.Normal = Vector3.up);
                    break;
            }
        }

        if (pointerEnabled) DrawPointer(spawnPoz);

        if (drawMode) DrawHotEdge(spawnPoz);

        if (algoMode) DrawAlgoPath();

        DrawEdges();
        DrawNodes();
    }


    private void Last2PointsWorks()
    {
        var n1 = Nodes[^1];
        var n2 = Nodes[^2];

        Handles.color = Color.cyan;
        Handles.DrawLine(n1.Position, n1.Position + n1.Normal * 2, 3f);
        Handles.DrawLine(n2.Position, n2.Position + n2.Normal * 2, 3f);

        var digitNormal = Vector3.Lerp(n1.Normal, n2.Normal, .5f);

        var digitStartProjection = BasicGraphEnv.GetProjectedPoz(n1.Position, n2.Normal);
        var digitEndProjection = BasicGraphEnv.GetProjectedPoz(n2.Position, n2.Normal);
        var digitPoz = Vector3.Lerp(digitStartProjection, digitEndProjection, .5f);

        Handles.DrawLine(digitPoz, digitPoz + digitNormal * 2, 3f);
        //
        // var dir = n2.Position - n1.Position;
        //
        // var obj = GameObject.Find("tstChr");
        // obj.transform.position = n1.Position;
        // obj.transform.rotation = Quaternion.LookRotation(n1.Normal, dir);
        // // obj.transform.LookAt(n2.Position);
        // // obj.transform.eulerAngles += new Vector3(0, -90, 90);

        // var obj2 = GameObject.Find("alp_B");
        // Debug.Log(obj2.transform.rotation);
    }

    private IEnumerator MassTestPaths()
    {
        for (var i = 0; i < 100; i++)
        {
            CreateRandomPath();
            yield return new EditorWaitForSeconds(.1f);
        }
    }

    private void DeleteOrphans()
    {
        foreach (var orphanNode in Nodes.Where((_, i) => Edges.Count(e => e.Start == i || e.End == i) < 2).ToList())
            Nodes.Remove(orphanNode);
        SceneView.lastActiveSceneView.ShowNotification(new GUIContent("deletes"), .1f);
    }


    private void DrawPointer(Vector3 spawnPoz)
    {
        Handles.color = Color.red;
        Handles.DrawWireCube(spawnPoz, Vector3.one * .2f);
    }

    private List<int> orphanNodes = new();

    private void Validate()
    {
        orphanNodes = Enumerable.Range(0, Nodes.Count)
            .Where(node =>
            {
                var nodeEdges = Edges.Where(e => e.Start == node || e.End == node).ToList();

                if (node == 0)
                {
                }

                if (Nodes[node].Type is not 1)
                {
                    var inOut = nodeEdges.Where(e => e.Direction == 0).ToList();
                    var outNodes = nodeEdges.Where(e => e.Direction != 0 && e.CanMoveOut(node)).ToList();

                    //treat one of the in out as in only, so subtract one from them
                    var outableCount = Math.Max(inOut.Count - 1, 0) + outNodes.Count;
                    return outableCount == 0;
                }
                //test every in edge
                //opt1: all in edges in the same group have the same test

                //test2 every in-out edge
                //opt2: all in-out edges in the same group have the same test

                //what is the difference between both tests?
                //we remove the testing edge from the outables, so the algo is
                //if the group have in-out, make tight test that will work for sure for in only
                //otherwise make loose test

                //this is not true for one thing, why to remove outable (in-out) while it will never be used?
                //so we need an inable edge in a group to perform the test

                //so you will get other groups relative to a group and test the outablity

                var edgeGroups = nodeEdges.GroupBy(e => e.Group).ToList();
                foreach (var group in edgeGroups)
                {
                    if (!group.Any(e => e.CanMoveIn(node))) continue; //skip groups with no ins
                    //this case shouldn't exist AFAIK, but the inverse is possivle 

                    var otherGroups = edgeGroups.Where(g => g.Key != group.Key).SelectMany(g => g);
                    var outableCount = otherGroups.Count(e => e.CanMoveOut(node));

                    if (outableCount == 0) return true;
                }

                return false;
            }).ToList();

        Debug.Log($"there's {orphanNodes.Count} orphans");
        foreach (var node in orphanNodes) Debug.Log(Nodes[node]);
        //////////////////////////


        //check for nodes that has only edges of type 1
        //1. get all edges of each node
        //2. check if all edges are of type 1
        //3. if so, remove node
        var isJumperOnly = Nodes.Select((n, i) => i).ToDictionary(i => i, _ => true);
        foreach (var edge in Edges.Where(edge => edge.Type != 1))
        {
            isJumperOnly[edge.Start] = false;
            isJumperOnly[edge.End] = false;
        }

        foreach (var jumperOnly in isJumperOnly.Where(n => n.Value))
        {
            Debug.Log($"node {Nodes[jumperOnly.Key]} at {jumperOnly.Key} doesn't have any walkable edges");
        }

        foreach (var edge in new List<Edge>(Edges))
        {
            if (edge.Start == edge.End)
            {
                Debug.Log($"Edge {edge} is self loop, removing");
                Edges.Remove(edge);
            }

            if (edge.Start < 0 || edge.Start >= Nodes.Count || edge.End < 0 || edge.End >= Nodes.Count)
            {
                Debug.Log($"Edge {edge.Start} -> {edge.End} has deleted/faulty node ids, removing");
                Edges.Remove(edge);
            }
        }
        //remove looping edges, edges with wrong nodes
    }

    // Called when the active tool is set to this tool instance. Global tools are persisted by the ToolManager,
    // so usually you would use OnEnable and OnDisable to manage native resources, and OnActivated/OnWillBeDeactivated
    // to set up state. See also `EditorTools.{ activeToolChanged, activeToolChanged }` events.
    public override void OnActivated()
    {
        SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Entering Graph Tool"), .1f);
    }

    // Called before the active tool is changed, or destroyed. The exception to this rule is if you have manually
    // destroyed this tool (ex, calling `Destroy(this)` will skip the OnWillBeDeactivated invocation).
    public override void OnWillBeDeactivated()
    {
        SceneView.lastActiveSceneView.ShowNotification(new GUIContent("Exiting Graph Tool"), .1f);
    }

    private void Save()
    {
        var msg = graphData != null ? "saved" : "no graph dats object found";
        SceneView.lastActiveSceneView.ShowNotification(new GUIContent(msg), .1f);
    }

    public void Load()
    {
        if (GraphEditorWindow.I is null) GraphEditorWindow.ShowWindow();

        graphData = GraphEditorWindow.I!.ChosenGraph;
        //can be null

        // var loadedScenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToList();
        //
        // try
        // {
        //     var envScene = loadedScenes.First(sc => sc.name.Contains("Env"));
        //     if (GraphData == null)
        //         GraphData = Resources.Load<GraphData>(envScene.name + " Graph");
        //
        //     if (!GraphData) Debug.LogError("no graph data found");
        // }
        // catch (Exception e)
        // {
        //     Debug.LogWarning("there's no graph");
        // }    
    }


    private static (Vector3, Vector3) GetSpawnPosition()
    {
        var mousePosition = Event.current.mousePosition;
        var validSpawn = HandleUtility.PlaceObject(mousePosition, out var worldPoz, out var normal);
        var spawnPoint = validSpawn
            ? worldPoz
            : HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).GetPoint(10);
        return (spawnPoint, normal);
    }

    private void DrawNodes()
    {
        if (viewMode) return;

        Handles.zTest = drawMode ? CompareFunction.Less : CompareFunction.Always;

        for (var i = 0; i < Nodes.Count; i++)
        {
            var node = Nodes[i];

            if (i == startNode)
            {
                Handles.zTest = CompareFunction.Always;
                node.Position = Handles.PositionHandle(node.Position, Quaternion.identity);
                Handles.zTest = CompareFunction.Less;
                continue;
            }

            Handles.color = node.Type == 0 ? Color.green : Color.magenta;

            if (algoMode && algoFinishNode == i) Handles.color = Color.white;

            var absCapSize = orphanNodes.Contains(i) ? 1f : .15f;
            var relativeCapSize = HandleUtility.GetHandleSize(node.Position) * absCapSize;
            var capSize = drawMode ? .1f : relativeCapSize;

            if (Handles.Button(node.Position, Quaternion.identity, capSize, capSize,
                    Handles.SphereHandleCap))
            {
                if (Event.current.control)
                {
                    RemoveNode(i);
                    i--;
                }
                else if (Event.current.shift)
                {
                    SwitchNode(i);
                }
                else
                {
                    ConnectNode(i, 0);
                }
            }

            Handles.DrawLine(node.Position, node.Position + node.Normal * .5f, 3f);

            Handles.Label(node.Position, i.ToString());
        }
    }

    private void SwitchNode(int node)
    {
        RecordUndo();
        Nodes[node].Type = 1 - Nodes[node].Type;
    }

    #region edges

    private readonly Color[] edgeGroupColors =
    {
        // Color.Lerp(Color.blue, Color.black, 0f),
        Color.blue,
        Color.cyan,
        Color.green,
        Color.yellow,
    };

    private void DrawEdges()
    {
        Handles.zTest = drawMode ? CompareFunction.Less : CompareFunction.Always;

        for (var i = 0; i < Edges.Count; i++)
        {
            var edge = Edges[i];

            Handles.color = edgeGroupColors[edge.Group];
            if (Edges[i].Type == 1) Handles.color -= Color.white * .5f;

            // Handles.zTest = CompareFunction.Less;
            Handles.DrawLine(Nodes[edge.Start].Position, Nodes[edge.End].Position, 3f);
            // Handles.Label(Vector3.Lerp(Nodes[edge.Start].Position, Nodes[edge.End].Position, .5f), i.ToString());

            // Handles.zTest = CompareFunction.Always;
            if (viewMode) continue;


            Quaternion capDir;
            Handles.CapFunction capFunction;
            if (edge.Direction != 0)
            {
                var edgeVector = Nodes[edge.End].Position - Nodes[edge.Start].Position;
                capDir = Quaternion.LookRotation(edgeVector.normalized * edge.Direction);
                capFunction = Handles.ConeHandleCap;
            }
            else
            {
                capDir = Quaternion.identity;
                capFunction = Handles.CubeHandleCap;
            }

            var capPoz = Vector3.Lerp(Nodes[edge.Start].Position, Nodes[edge.End].Position, .3f);
            var capSize = HandleUtility.GetHandleSize(capPoz) * .15f;

            var dirClicked = Handles.Button(capPoz, capDir, capSize, capSize, capFunction);

            if (!dirClicked) continue;
            if (Event.current.control)
            {
                startNode = -1;

                Edges.Remove(Edges[i]);

                i--;
            }
            else if (Event.current.shift)
            {
                edge.Group++;
                edge.Group %= edgeGroupColors.Length;
            }
            else
            {
                var relativeDir = ((edge.Direction + 2) % 3) - 1;
                //1. sets edges between 0:2 rather than -1:1, then add 1 to cycle it
                //2. keep between 0:2
                //3. return it to -1:1 form

                var road = GetRoad(edge);

                for (var j = 1; j < road.Count; j++)
                {
                    var start = road[j - 1];
                    var end = road[j];

                    var normalEdge = graphData.Edges.FirstOrDefault(e => e.Start == start && e.End == end);
                    if (normalEdge is not null) normalEdge.Direction = relativeDir;
                    var reverseEdge = graphData.Edges.FirstOrDefault(e => e.End == start && e.Start == end);
                    if (reverseEdge is not null) reverseEdge.Direction = -relativeDir;

                    if (normalEdge is not null && reverseEdge is not null)
                        Debug.LogWarning("ATTENTION, is that possible?");
                }
            }
            //direction button
        }


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
    }

    public List<int> GetRoad(Edge startEdge)
    {
        var nodesEdges = GraphManager.GetNodeEdges(graphData);

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


    private void DrawHotEdge(Vector3 spawnPoint)
    {
        Handles.zTest = CompareFunction.Always;

        // Handles.color = jumperDraw ? Color.red : Color.blue;
        Handles.color = Color.blue;
        if (startNode == -1) return;
        Handles.DrawLine(Nodes[startNode].Position, spawnPoint, 7f);
    }

    #endregion

    private void ConnectNode(int node, int type)
    {
        if (startNode == node)
        {
            Debug.LogWarning("you're trying to make a zero-edge-loop");
            return;
        }

        RecordUndo();


        if (startNode == -1)
        {
            startNode = node;
        }
        else
        {
            if (Edges.Any(e => e.Start == startNode && e.End == node || e.Start == node && e.End == startNode)) return;

            var edge = new Edge(startNode, node, type);

            Edges.Add(edge);

            // StartNode.Edges.Add(edge);
            // node.Edges.Add(edge);

            startNode = -1;

            // RefreshEditor();
        }
    }

    private void RemoveNode(int node)
    {
        RecordUndo();

        startNode = -1;

        Edges.RemoveAll(e => e.Start == node || e.End == node);

        Nodes.RemoveAt(node);

        foreach (var edge in Edges)
        {
            if (edge.Start > node) edge.Start--;
            if (edge.End > node) edge.End--;
        }

        // Edges.RemoveAll(e => node.Edges.Contains(e));
        // node.Edges.ForEach(e =>
        // {
        // if (e.Start != node)
        // e.Start.Edges.Remove(e);
        // if (e.End != node)
        // e.End.Edges.Remove(e);
        // });
    }

    private void SpawnNode(Vector3 spawnPoz, Vector3 spawnNormal, int connectionType)
    {
        RecordUndo();

        Nodes.Add(new Node { Position = spawnPoz, Type = 0, Normal = spawnNormal });

        if (!drawMode) return;

        ConnectNode(Nodes.Count - 1, connectionType);
        startNode = Nodes.Count - 1;
    }

    // private void RefreshEditor()
    // {
    //     EditorUtility.SetDirty(GraphData);
    //
    //     // AssetDatabase.SaveAssets();
    //     // AssetDatabase.Refresh();
    // }


    private List<(int node, bool isWalkable)> algoPath;
    private List<Edge> remainingAlgoEdges;
    private int algoFinishNode;
    private int smoothLevel;

    private void CreateRandomPath()
    {
        algoPath = GraphManager.GetRandomPath(graphData);
    }


    private void DrawAlgoPath()
    {
        Handles.zTest = CompareFunction.Always;

        if (algoPath is null) return;

        for (var i = 0; i < algoPath.Count - 1; i++)
        {
            // Handles.color = AlgoPath[i + 1].isWalkable ? Color.yellow : Color.white;
            Handles.color = Color.Lerp(Color.white, Color.magenta, i / (float)algoPath.Count);

            Handles.DrawLine(Nodes[algoPath[i].node].Position, Nodes[algoPath[i + 1].node].Position, 10f);
        }

        Handles.color = Color.cyan;
        if (remainingAlgoEdges != null)
            for (var i = 0; i < remainingAlgoEdges.Count; i++)
            {
                var edge = remainingAlgoEdges[i];
                Handles.DrawLine(Nodes[edge.Start].Position, Nodes[edge.End].Position, 10f);
            }
    }

    private void RecordUndo()
    {
        Undo.RecordObject(graphData, "graphTool");
        EditorUtility.SetDirty(graphData);
    }
}