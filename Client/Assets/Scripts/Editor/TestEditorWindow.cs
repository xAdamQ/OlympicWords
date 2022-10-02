using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphEditorWindow : EditorWindow
{
    [MenuItem("Window/GraphSettings")]
    public static void ShowWindow()
    {
        OpenOrRefreshWindow();
    }

    private static void OpenOrRefreshWindow()
    {
        var wnd = GetWindow<GraphEditorWindow>();
        if (wnd != null)
        {
            wnd.Close();
            wnd = GetWindow<GraphEditorWindow>();
        }

        wnd.titleContent = new GUIContent("Positioning Test Window");
    }

    public static GraphEditorWindow I;

    //create new graph? yes, orphans are still applicable 
    //choose graph
    //delete? no

    private const string GRAPH_ROOT = "Assets/Resources/Graphs";
    private List<(string name, List<GraphData> graphs)> graphTree = new();

    private void OnEnable()
    {
        I = this;

        var subFolders = AssetDatabase.GetSubFolders(GRAPH_ROOT);
        foreach (var subFolder in subFolders)
        {
            var guids = AssetDatabase.FindAssets("", new[] { subFolder });

            var graphs = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GraphData>)
                .ToList();

            graphTree.Add((System.IO.Path.GetFileName(subFolder), graphs));
        }
    }

    private int selectedGraphList;


    private int SelectedGraphList
    {
        set
        {
            selectedGraphList = value;

            listButtons[selectedGraphList].style.backgroundColor = Color.grey;
            listButtons[value].style.backgroundColor = Color.Lerp(Color.black, Color.cyan, .7f);
            //update button colors

            var hi = graphTree[value].graphs.Count - 1;
            selectGraphSlider.highValue = hi;
            selectGraphSlider.value = SelectedGraph >= hi ? 0 : SelectedGraph;
            //update slider

            SelectedGraph = selectGraphSlider.value;
            //call graph fun, and update graph value if changes from slider
        }
        get => selectedGraphList;
    }


    private int selectedGraph;

    private int SelectedGraph
    {
        set
        {
            selectedGraph = value;
            selectedGraphName.text = ChosenGraph ? ChosenGraph.name : "null";
            if (GraphTool.I) GraphTool.I.Load();
        }
        get => selectedGraph;
    }

    public GraphData ChosenGraph
    {
        get
        {
            try
            {
                return graphTree[selectedGraphList].graphs[selectedGraph];
            }
            catch (ArgumentOutOfRangeException e)
            {
                return null;
            }
        }
    }

    public List<GraphData> ChosenGraphList
    {
        get
        {
            try
            {
                return graphTree[selectedGraphList].graphs;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }
    }

    private List<Button> listButtons = new();
    private Label selectedGraphName;
    private SliderInt selectGraphSlider;


    /// <summary>
    /// this is not GUI update! only called once!
    /// </summary>
    private void CreateGUI()
    {
        for (var i = 0; i < graphTree.Count; i++)
        {
            var graphList = graphTree[i];
            var graphButton = new Button()
            {
                text = graphList.name,
            };

            var iCopy = i;
            graphButton.clicked += () => SelectedGraphList = iCopy;

            rootVisualElement.Add(graphButton);
            listButtons.Add(graphButton);
        }


        selectGraphSlider = new SliderInt
        {
            lowValue = 0
        };

        selectGraphSlider.RegisterValueChangedCallback(e => SelectedGraph = e.newValue);
        rootVisualElement.Add(selectGraphSlider);

        selectedGraphName = new Label()
        {
            text = ChosenGraph.name,
            style =
            {
                alignContent = Align.Center,
            }
        };
        rootVisualElement.Add(selectedGraphName);

        SelectedGraphList = selectedGraphList;


        var refreshButton = new Button()
        {
            text = "refresh",
            style =
            {
                backgroundColor = Color.Lerp(Color.black, Color.yellow, .7f),
            }
        };
        refreshButton.clicked += OpenOrRefreshWindow;
        rootVisualElement.Add(refreshButton);
    }

    // private void OnGUI()
    // {
    //     if (SelectedGraphList >= graphTree.Count) SelectedGraphList = 0;
    //     if (SelectedGraph >= graphTree[SelectedGraphList].graphs.Count) SelectedGraph = 0;
    // }
}