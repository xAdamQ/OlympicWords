using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using Unity.EditorCoroutines.Editor;
using Wintellect.PowerCollections;

[EditorTool("GraphTool")]
internal class GraphTool : EditorTool
{
    // Serialize this value to set a default value in the Inspector.
    [SerializeField] private Texture2D icon;

    public GraphData GraphData;

    private bool PointerEnabled, DrawMode, JumperDraw, ViewMode, AlgoMode;

    private List<Node> Nodes => GraphData.Nodes;
    private List<Edge> Edges => GraphData.Edges;

    private int StartNode;

    public override GUIContent toolbarIcon => new()
    {
        image = icon,
        text = "GraphTool",
        tooltip = "GraphTool",
    };

    private void OnEnable()
    {
        StartNode = -1;

        Load();
    }

    public override void OnToolGUI(EditorWindow window)
    {
        if (window is not SceneView or null || !ToolManager.IsActiveTool(this)) return;

        var spawnPoz = GetSpawnPosition();

        Handles.zTest = CompareFunction.Less;

        switch (Event.current.type)
        {
            case EventType.MouseDown when Event.current.button == 1 && Event.current.shift:
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
                case KeyCode.X:
                    CreateRandomPath();
                    break;
            }
        }

        if (PointerEnabled)
            DrawPointer(spawnPoz);

        if (DrawMode)
            DrawHotEdge(spawnPoz);

        if (AlgoMode)
            DrawAlgoPath();

        DrawNodes();
        DrawEdges();
    }


    private void DrawPointer(Vector3 spawnPoz)
    {
        Handles.color = Color.red;
        Handles.DrawWireCube(spawnPoz, Vector3.one * .2f);
    }

    private void Check()
    {
        foreach (var edge in Edges.Where(edge => edge.Start < 0 || edge.Start >= Nodes.Count || edge.End < 0 || edge.End >= Nodes.Count))
            Debug.Log($"Edge {edge.Start} -> {edge.End} is orphan");

        foreach (var orphanNode in Nodes.Where((_, i) => Edges.Count(e => e.Start == i || e.End == i) < 2))
            Debug.Log($"orphan node at: {orphanNode}");

        foreach (var edge in Edges.Where(edge => edge.Start == edge.End).ToList())
        {
            Debug.Log($"Edge {edge} is self loop, removing");
            Edges.Remove(edge);
        }

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

    private void Load()
    {
        if (!GraphData) Debug.LogError("no graph data found");
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

            if (AlgoFinishNode == i)
            {
                Handles.color = Color.white;
            }

            if (Handles.Button(node.Position + Vector3.one * .0f, Quaternion.identity, .25f, .25f, Handles.SphereHandleCap))
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
        }
    }

    private void SwitchNode(int node)
    {
        RecordUndo();
        Nodes[node].Type = 1 - Nodes[node].Type;
    }


    private void DrawHotEdge(Vector3 spawnPoint)
    {
        Handles.color = JumperDraw ? Color.red : Color.blue;
        if (StartNode == -1) return;
        Handles.DrawLine(Nodes[StartNode].Position, spawnPoint, 7f);
    }

    private void DrawEdges()
    {
        for (var i = 0; i < Edges.Count; i++)
        {
            var edge = Edges[i];

            Handles.color = Edges[i].Type == 0 ? Color.blue : Color.red;

            Handles.zTest = CompareFunction.Less;
            Handles.DrawLine(Nodes[edge.Start].Position, Nodes[edge.End].Position, 3f);

            Handles.zTest = CompareFunction.Always;
            if (!ViewMode)
            {
                var clicked = Handles.Button(Vector3.Lerp(Nodes[edge.Start].Position, Nodes[edge.End].Position, .5f), Quaternion.identity, .15f, .15f, Handles.CubeHandleCap);
                if (clicked)
                {
                    StartNode = -1;

                    Edges.Remove(Edges[i]);

                    i--;
                }
            }
        }
    }


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


    private List<int> AlgoPath;
    private List<Edge> RemainingAlgoEdges;
    private int AlgoFinishNode;

    private void CreateRandomPath()
    {
        AlgoPath = GraphManager.GetRandomPath(GraphData);
    }


    private void DrawAlgoPath()
    {
        Handles.zTest = CompareFunction.Always;

        if (AlgoPath is null) return;

        Handles.color = Color.magenta;
        for (int i = 0; i < AlgoPath.Count - 1; i++)
            Handles.DrawLine(Nodes[AlgoPath[i]].Position, Nodes[AlgoPath[i + 1]].Position, 10f);

        Handles.color = Color.green;
        foreach (var edge in RemainingAlgoEdges)
            Handles.DrawLine(Nodes[edge.Start].Position, Nodes[edge.End].Position, 10f);
    }

    private void RecordUndo()
    {
        Undo.RecordObject(GraphData, "graphTool");
        EditorUtility.SetDirty(GraphData);
    }
}