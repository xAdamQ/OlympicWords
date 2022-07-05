using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class WriteDigits : MonoBehaviour
{
    [SerializeField] private Mesh[] DigitModels;
    [SerializeField] private LineRenderer[] SceneLines;

    private string[] testWords = { "welcome", "to", "our", "everything", "schreibtisch" };

    [SerializeField] private float MinLineSpacing;

    public Vector3[] tstV;

    private void OnDrawGizmos()
    {
        // if (Mouse.current.IsPressed())
        if (Input.GetMouseButtonDown(0))
            Debug.Log("selected");

        foreach (var v in tstV)
        {
            Gizmos.DrawCube(v, Vector3.one);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // if (Input.GetMouseButtonDown(0))
    }

    // private IEnumerator Start()
    // {
    //     var remainingLines = SceneLines.ToList();
    //     var words = testWords.ToList();
    //     var allPoints = SceneLines.Select(l => new LinePoint { AbsPosition = l.transform.position + l.GetPosition(0), Line = l })
    //         .Concat(SceneLines.Select(l => new LinePoint { AbsPosition = l.transform.position + l.GetPosition(1), Line = l })).ToList();
    //
    //     //1. get a start line
    //     //todo now it will pick the min X, later it can be changed and change the camera view
    //     // var startLine = CandidateLines.OrderBy(l => l.transform.position.x).First();
    //     var firstLine = remainingLines[Random.Range(0, remainingLines.Count)];
    //     firstLine.startColor = Color.blue;
    //     firstLine.endColor = Color.blue;
    //
    //     remainingLines.Remove(firstLine);
    //     // var secondLineCandidates = SceneLines.Where(l => LinesDistance(firstLine, l) < MinLineSpacing).ToList();
    //     // var secondLineCandidatesInverse = SceneLines.Where(l => LinesDistance(l, firstLine) < MinLineSpacing).ToList();
    //     //
    //     // var winnerSecondLines = secondLineCandidates;
    //     // if (secondLineCandidates.Count == 0)
    //     //
    //     //     if (secondLineCandidates.Count == 0)
    //     //         secondLineCandidates = new List<LineRenderer> { SceneLines.OrderBy(l => Vector3.Distance(l.transform.position, firstLine.transform.position)).First() };
    //     //second line will decide the direction
    //
    //     //1-0 = 1
    //     //1-1 = 0
    //
    //     //0-0 = 0
    //     //0-1 = -1
    //
    //     /*
    //      
    //      0+1 = 1
    //      1+1 = 2 => 2%2 => 0
    //      
    //      0+0 = 0
    //      1+0 = 1
    //       
    //      */
    //
    //     var direction = Random.Range(0, 2);
    //
    //     var prevLine = firstLine;
    //
    //     while (words.Count > 0)
    //     {
    //         var candidateLines = remainingLines.Where(l => LinesDistance(prevLine, l, direction) < MinLineSpacing).ToList();
    //         if (candidateLines.Count == 0)
    //             candidateLines = new List<LineRenderer> { remainingLines.OrderBy(l => LinesDistance(prevLine, l, direction)).First() };
    //
    //         var nextLine = candidateLines[Random.Range(0, candidateLines.Count)];
    //
    //         Debug.Log(prevLine.name);
    //         Debug.Log(prevLine.GetPosition(0));
    //         Debug.Log(prevLine.GetPosition(1));
    //
    //         Debug.Log(nextLine.name);
    //         Debug.Log(nextLine.GetPosition(0));
    //         Debug.Log(nextLine.GetPosition(1));
    //
    //
    //         nextLine.startColor = Color.red;
    //         nextLine.endColor = Color.red;
    //
    //         remainingLines.Remove(nextLine);
    //         words.RemoveAt(0);
    //
    //         prevLine = nextLine;
    //
    //         yield return new WaitForSeconds(1f);
    //     }
    //     //1. find geometrically sequent lines
    //     //chosen lines should be removed from the remaining
    //     //use center position far to sort them, then use end to tart far to choose with prob
    // }

    private IEnumerator Start()
    {
        var allPoints = SceneLines.Select(l => new LinePoint { AbsPosition = l.transform.position + l.GetPosition(0), Line = l })
            .Concat(SceneLines.Select(l => new LinePoint { AbsPosition = l.transform.position + l.GetPosition(1), Line = l })).ToList();

        var vertices = new List<Vertex>();
        var lines = new List<Line>();

        //vertex map can be precomputed
        while (allPoints.Count > 0)
        {
            var masterPoint = allPoints[0];
            allPoints.Remove(masterPoint);
            var vertex = new Vertex { Lines = new List<Line>(), Position = masterPoint.AbsPosition };
            vertices.Add(vertex);

            var vertexRealPoints = new List<Vector3> { masterPoint.AbsPosition };

            var masterLine = lines.FirstOrDefault(l => l.Renderer == masterPoint.Line);
            if (masterLine == null)
            {
                masterLine = new Line { Renderer = masterPoint.Line, Ends = (vertex, null) };
                lines.Add(masterLine);
            }
            else
            {
                masterLine.Ends.Y = vertex;
            }

            vertex.Lines.Add(masterLine);

            foreach (var slavePoint in allPoints.Where(p => Vector3.Distance(masterPoint.AbsPosition, p.AbsPosition) < MinLineSpacing).ToList())
            {
                vertexRealPoints.Add(slavePoint.AbsPosition);
                vertex.Position = vertexRealPoints.Sum() / vertexRealPoints.Count;

                allPoints.Remove(slavePoint);

                var slaveLine = lines.FirstOrDefault(l => l.Renderer == slavePoint.Line);
                if (slaveLine == null)
                {
                    slaveLine = new Line { Renderer = slavePoint.Line, Ends = (vertex, null) };
                    lines.Add(slaveLine);
                }
                else
                {
                    slaveLine.Ends.Y = vertex;
                }

                vertex.Lines.Add(slaveLine);
            }
        }

        yield return null;

        vertices.ForEach(v =>
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.transform.position = v.Position;
            if (v.Lines.Count < 2)
                obj.GetComponent<Renderer>().material.color = Color.red;
        });
    }

    //start and end doesn't even have a meaning

    // private float LinesDistance(LineRenderer startLine, LineRenderer endLine, int direction)
    // {
    //     var first = (1 + direction) % 2;
    //     var second = (0 + direction) % 2;
    //
    //
    //     return Vector3.Distance(startLine.transform.position + startLine.GetPosition(first), endLine.transform.position + endLine.GetPosition(second));
    // }
}

public class LinePoint
{
    public LineRenderer Line;
    public Vector3 AbsPosition;
}

public class Vertex
{
    public List<Line> Lines;
    public Vector3 Position;

    // public Vertex(Line lineX, Line lineY, Vector3 position)
    // {
    //     Lines = new List<Line> { lineX, lineY };
    //     Position = position;
    // }
}

public class Line
{
    public (Vertex X, Vertex Y) Ends;
    public LineRenderer Renderer;
    public (Vector3 X, Vector3 Y) NumericEnds;

    // public Line(LineRenderer renderer, Vertex vertex)
    // {
    //     NumericEnds = (renderer.GetPosition(0), renderer.GetPosition(1));
    //     Ends = (vertex, null);
    // }
}


//create the vertex representation of the made approximations