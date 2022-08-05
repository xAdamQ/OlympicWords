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

    private GraphData GraphData;

    private bool PointerEnabled, DrawMode, JumperDraw, ViewMode, AlgoMode, CheckMode;


    private List<Node> Nodes => GraphData.Nodes;
    private List<Edge> Edges => GraphData.Edges;

    private int StartNode;

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

        StartNode = -1;

        Load();
    }

    private List<Vector3> smoothGraph;

    public override void OnToolGUI(EditorWindow window)
    {
        if (window is not SceneView or null || !ToolManager.IsActiveTool(this) || GraphData is null) return;

        var spawnPoz = GetSpawnPosition();

        Handles.zTest = CompareFunction.Less;

        switch (Event.current.type)
        {
            case EventType.MouseDown
                when Event.current.button == 0 && Event.current.shift && DrawMode:
                SpawnNode(spawnPoz, 0);
                break;
            case EventType.MouseDown when Event.current.button == 1 && Event.current.control:
                SpawnNode(spawnPoz, 1);
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
                    PointerEnabled = !PointerEnabled;
                    break;
                case KeyCode.Escape:
                    StartNode = -1;
                    break;
                case KeyCode.S:
                    Save();
                    break;
                case KeyCode.V:
                    ViewMode = !ViewMode;
                    break;
                case KeyCode.C:
                    Check();
                    break;
                case KeyCode.L:
                    Load();
                    break;
                case KeyCode.D:
                    DrawMode = !DrawMode;
                    break;
                case KeyCode.J:
                    JumperDraw = !JumperDraw;
                    break;
                case KeyCode.G:
                    AlgoMode = !AlgoMode;
                    SceneView.lastActiveSceneView.ShowNotification(new GUIContent("AlgoMode " + AlgoMode), .1f);
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
            }
        }

        if (PointerEnabled) DrawPointer(spawnPoz);

        if (DrawMode) DrawHotEdge(spawnPoz);

        if (AlgoMode) DrawAlgoPath();

        DrawEdges();
        DrawNodes();
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

    private List<int> OrphanNodes = new();

    private void Check()
    {
        OrphanNodes = Enumerable.Range(0, Nodes.Count)
            .Where(node =>
            {
                var nodeEdges = Edges.Where(e => e.Start == node || e.End == node).ToList();
                var inOutCount = nodeEdges.Count(e => e.Direction == 0);
                var inOutTreated = inOutCount > 0 ? inOutCount - 1 : 0; //treat on of the in out as in only
                var outCount = nodeEdges.Count(e => e.Direction != 0 && e.CanMoveOut(node));
                var outableCount = inOutTreated + outCount;

                return outableCount == 0;
            })
            .ToList();

        Debug.Log($"there's {OrphanNodes.Count} orphans");
        foreach (var node in OrphanNodes) Debug.Log(Nodes[node]);
        //////////////////////////


        //check for nodes that has only edges of type 1
        //1. get all edges of each node
        //2. check if all edges are of type 1
        //3. if so, remove node
        var isJumperOnly = new Dictionary<int, bool>();
        isJumperOnly = Nodes.Select((n, i) => i).ToDictionary(i => i, _ => true);
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
        var msg = GraphData != null ? "saved" : "no graph dats object found";
        SceneView.lastActiveSceneView.ShowNotification(new GUIContent(msg), .1f);
    }

    public void Load()
    {
        if (GraphEditorWindow.I is null) GraphEditorWindow.ShowWindow();

        GraphData = GraphEditorWindow.I!.ChosenGraph;
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


    private static Vector3 GetSpawnPosition()
    {
        var mousePosition = Event.current.mousePosition;
        var validSpawn = HandleUtility.PlaceObject(mousePosition, out var worldPoz, out var normal);
        return validSpawn ? worldPoz : HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).GetPoint(10);
    }

    private void DrawNodes()
    {
        if (ViewMode) return;

        for (var i = 0; i < Nodes.Count; i++)
        {
            var node = Nodes[i];

            if (i == StartNode)
            {
                Handles.zTest = CompareFunction.Always;
                node.Position = Handles.PositionHandle(node.Position, Quaternion.identity);
                Handles.zTest = CompareFunction.Less;
                continue;
            }

            Handles.color = node.Type == 0 ? Color.green : Color.magenta;

            if (AlgoMode && AlgoFinishNode == i) Handles.color = Color.white;

            var absCapSize = OrphanNodes.Contains(i) ? 1f : .15f;
            var relativeCapSize = HandleUtility.GetHandleSize(node.Position) * absCapSize;

            if (Handles.Button(node.Position, Quaternion.identity, relativeCapSize, relativeCapSize,
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
                    ConnectNode(i);
                }
            }

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
        for (var i = 0; i < Edges.Count; i++)
        {
            var edge = Edges[i];

            Handles.color = edgeGroupColors[edge.Group];
            if (Edges[i].Type == 1) Handles.color -= Color.white * .5f;

            Handles.zTest = CompareFunction.Less;
            Handles.DrawLine(Nodes[edge.Start].Position, Nodes[edge.End].Position, 3f);
            Handles.Label(Vector3.Lerp(Nodes[edge.Start].Position, Nodes[edge.End].Position, .5f), i.ToString());


            Handles.zTest = CompareFunction.Always;
            if (ViewMode) continue;


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
                StartNode = -1;

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

                    var normalEdge = GraphData.Edges.FirstOrDefault(e => e.Start == start && e.End == end);
                    if (normalEdge is not null) normalEdge.Direction = relativeDir;
                    var reverseEdge = GraphData.Edges.FirstOrDefault(e => e.End == start && e.Start == end);
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
        // var subPath = Nodes.Where((_, i) => i > Nodes.Count - 5).Select(n => n.Position).ToList();
        // for (int i = 0; i < smoothLevel; i++) subPath = CityEnv.SmoothenAngles(subPath);
        // for (int i = 0; i < subPath.Count - 1; i++) Handles.DrawLine(subPath[i], subPath[i + 1]);
        // subPath.ForEach(n => Handles.DrawWireCube(n, Vector3.one * .1f));


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
        var nodesEdges = GraphManager.GetNodeEdges(GraphData);

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
        Handles.color = JumperDraw ? Color.red : Color.blue;
        if (StartNode == -1) return;
        Handles.DrawLine(Nodes[StartNode].Position, spawnPoint, 7f);
    }

    #endregion

    private void ConnectNode(int node)
    {
        if (StartNode == node)
        {
            Debug.LogWarning("you're trying to make a zero-edge-loop");
            return;
        }

        RecordUndo();

        // var type = Event.current.shift ? 1 : 0;
        var type = JumperDraw ? 1 : 0;

        if (StartNode == -1)
        {
            StartNode = node;
        }
        else
        {
            if (Edges.Any(e => e.Start == StartNode && e.End == node || e.Start == node && e.End == StartNode)) return;

            var edge = new Edge(StartNode, node, type);

            Edges.Add(edge);

            // StartNode.Edges.Add(edge);
            // node.Edges.Add(edge);

            StartNode = -1;

            // RefreshEditor();
        }
    }

    private void RemoveNode(int node)
    {
        RecordUndo();

        StartNode = -1;

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

    private void SpawnNode(Vector3 spawnPoz, int type)
    {
        RecordUndo();

        Nodes.Add(new Node { Position = spawnPoz, Type = type });

        if (DrawMode)
        {
            ConnectNode(Nodes.Count - 1);
            StartNode = Nodes.Count - 1;
        }
    }

    // private void RefreshEditor()
    // {
    //     EditorUtility.SetDirty(GraphData);
    //
    //     // AssetDatabase.SaveAssets();
    //     // AssetDatabase.Refresh();
    // }


    private List<(int node, bool isWalkable)> AlgoPath;
    private List<Edge> RemainingAlgoEdges;
    private int AlgoFinishNode;
    private int smoothLevel;

    private void CreateRandomPath()
    {
        AlgoPath = GraphManager.GetRandomPath(GraphData);
    }


    private void DrawAlgoPath()
    {
        Handles.zTest = CompareFunction.Always;

        if (AlgoPath is null) return;

        Handles.color = Color.magenta;
        for (var i = 0; i < AlgoPath.Count - 1; i++)
        {
            // Handles.color = AlgoPath[i + 1].isWalkable ? Color.yellow : Color.white;
            Handles.color = Color.Lerp(Color.magenta, Color.black, i / (float)AlgoPath.Count);

            Handles.DrawLine(Nodes[AlgoPath[i].node].Position, Nodes[AlgoPath[i + 1].node].Position, 10f);
        }

        Handles.color = Color.green;
        if (RemainingAlgoEdges != null)
            for (var i = 0; i < RemainingAlgoEdges.Count; i++)
            {
                var edge = RemainingAlgoEdges[i];
                Handles.DrawLine(Nodes[edge.Start].Position, Nodes[edge.End].Position, 10f);
            }
    }

    private void RecordUndo()
    {
        Undo.RecordObject(GraphData, "graphTool");
        EditorUtility.SetDirty(GraphData);
    }
}