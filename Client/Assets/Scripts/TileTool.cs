using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering;

[EditorTool("Tile Tool")]
internal class TileTool : EditorTool
{
    // Serialize this value to set a default value in the Inspector.
    [SerializeField] private Texture2D icon;

    public GraphData GraphData;

    private bool DrawMode, ViewMode;

    private List<Node> Nodes => GraphData.Nodes;
    private List<Edge> Edges => GraphData.Edges;

    private int? StartNode;

    public override GUIContent toolbarIcon => new()
    {
        image = icon,
        text = "Tile Tool",
        tooltip = "Tile Tool"
    };

    private void OnEnable()
    {
        StartNode = null;

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
                    DrawMode = !DrawMode;
                    break;
                case KeyCode.Escape:
                    StartNode = null;
                    break;
                case KeyCode.S:
                    Save();
                    break;
                case KeyCode.V:
                    ViewMode = !ViewMode;
                    break;
                case KeyCode.C:
                    CheckOrphans();
                    break;
                case KeyCode.L:
                    Load();
                    break;
            }
        }

        if (DrawMode)
        {
            DrawPointer(spawnPoz);
        }

        DrawNodes();
        DrawEdges();
    }

    private void DrawPointer(Vector3 spawnPoz)
    {
        Handles.color = Color.red;
        Handles.DrawWireCube(spawnPoz, Vector3.one * .2f);
    }

    private void CheckOrphans()
    {
        foreach (var edge in Edges)
        {
            if (!Nodes.Contains(edge.Start))
                Debug.Log("edge has lost start node at ");

            if (!Nodes.Contains(edge.Start))
                Debug.Log("edge has lost end node at ");
        }


        foreach (var orphanNode in Nodes.Where(node => Edges.Count(e => e.Start == node || e.End == node) < 2))
        {
            Debug.Log($"orphan node at: {orphanNode}");
        }

        // foreach (var node in Nodes.Where(node => node.Edges.Count < 2))
        // {
        //     Debug.Log(node);
        // }
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
        if (GraphData != null)
        {
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent("saved"), .1f);
        }
        else
        {
            SceneView.lastActiveSceneView.ShowNotification(new GUIContent("no graph dats object found"), .1f);
        }
    }

    private void Load()
    {
        if (!GraphData)
        {
            Debug.LogError("no graph data found");
            return;
        }

        // GraphData.SetNavProps();
        // Nodes = GraphData.Nodes;
        // Edges = GraphData.Edges;
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
            Handles.color = StartNode == node ? Color.red : node.Type == 0 ? Color.blue : Color.magenta;

            Handles.zTest = CompareFunction.Always;
            node.Position = Handles.PositionHandle(node.Position, Quaternion.identity);

            Handles.zTest = CompareFunction.Less;
            if (Handles.Button(node.Position + Vector3.one * .2f, Quaternion.identity, .15f, .15f, Handles.SphereHandleCap))
            {
                if (Event.current.control)
                {
                    RemoveNode(node);
                    i--;
                }
                else
                {
                    ConnectNode(node);
                }
            }
        }
    }

    private void DrawEdges()
    {
        for (var i = 0; i < Edges.Count; i++)
        {
            Handles.color = Edges[i].Type == 0 ? Color.blue : Color.red;

            Handles.zTest = CompareFunction.Less;
            Handles.DrawLine(Edges[i].Start.Position, Edges[i].End.Position, 7f);

            Handles.zTest = CompareFunction.Always;
            if (!ViewMode)
            {
                var clicked = Handles.Button(Vector3.Lerp(Edges[i].Start.Position, Edges[i].End.Position, .5f), Quaternion.identity, .15f, .15f, Handles.CubeHandleCap);
                if (clicked)
                {
                    StartNode = null;

                    // Edges[i].Start.Edges.Remove(Edges[i]);
                    // Edges[i].End.Edges.Remove(Edges[i]);

                    Edges.Remove(Edges[i]);

                    i--;
                }
            }
        }
    }


    private void ConnectNode(int node)
    {
        RecordUndo();

        var type = Event.current.shift ? 1 : 0;


        if (StartNode == null)
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

            StartNode = null;

            // RefreshEditor();
        }
    }

    private void RemoveNode(int node)
    {
        RecordUndo();

        StartNode = null;

        Nodes.Remove(node);

        Edges.RemoveAll(e => e.Start == node || e.End == node);

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

        // RefreshEditor();
    }

    // private void RefreshEditor()
    // {
    //     EditorUtility.SetDirty(GraphData);
    //
    //     // AssetDatabase.SaveAssets();
    //     // AssetDatabase.Refresh();
    // }

    private void RecordUndo()
    {
        Undo.RecordObject(GraphData, "graphTool");
        EditorUtility.SetDirty(GraphData);
    }
}