using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Capsule : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private Vector3 maxYVertex;
    private Vector3 minYVertex;
    private Vector3 maxXVertex;
    private Vector3 minXVertex;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        vertices = mesh.vertices;
        triangles = mesh.triangles;

        //Get the minimum and maximum verteces
        maxYVertex = vertices.FirstOrDefault();

    }
 
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        if(vertices == null)
        {
            return;
        }

        var count = 0;
        foreach(var vertex in vertices)
        {
            if(count==3)
            {
                count = 0;
            }

            var vert = new Vector3(transform.position.x + (vertex.x * transform.localScale.x), transform.position.y + (vertex.y * transform.localScale.y), transform.position.z + (vertex.z * transform.localScale.z));

            if(count==0)
            {
                Gizmos.color = Color.yellow;
            }
            if((count)==1)
            {
                Gizmos.color = Color.red;
            }
            else if ((count)==2)
            {
                Gizmos.color = Color.green;
            }
            else if((count)==3)
            {
                Gizmos.color = Color.blue;
            }

            Gizmos.DrawSphere(vert, 0.01f);

            count++;
        }
    }
}
