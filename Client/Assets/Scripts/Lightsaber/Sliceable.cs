using System.Collections;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.Serialization;

public class Sliceable : MonoBehaviour
{
    [FormerlySerializedAs("_isSolid")] [SerializeField]
    private bool isSolid = true;

    [FormerlySerializedAs("_reverseWindTriangles")] [SerializeField]
    private bool reverseWindTriangles;

    [FormerlySerializedAs("_useGravity")] [SerializeField]
    private bool useGravity;

    [FormerlySerializedAs("_shareVertices")] [SerializeField]
    private bool shareVertices;

    [FormerlySerializedAs("_smoothVertices")] [SerializeField]
    private bool smoothVertices;

    public bool IsSolid
    {
        get
        {
            return isSolid;
        }
        set
        {
            isSolid = value;
        }
    }

    public bool ReverseWireTriangles
    {
        get
        {
            return reverseWindTriangles;
        }
        set
        {
            reverseWindTriangles = value;
        }
    }

    public bool UseGravity 
    {
        get
        {
            return useGravity;
        }
        set
        {
            useGravity = value;
        }
    }

    public bool ShareVertices 
    {
        get
        {
            return shareVertices;
        }
        set
        {
            shareVertices = value;
        }
    }

    public bool SmoothVertices 
    {
        get
        {
            return smoothVertices;
        }
        set
        {
            smoothVertices = value;
        }
    }

}
