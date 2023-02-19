using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Lighsaber : MonoBehaviour
{
    //The number of vertices to create per frame
    private const int NUM_VERTICES = 12;

    [FormerlySerializedAs("_blade")] [SerializeField] [Tooltip("The blade object")]
    private GameObject blade;

    [FormerlySerializedAs("_tip")] [SerializeField] [Tooltip("The empty game object located at the tip of the blade")]
    private GameObject tip;

    [FormerlySerializedAs("_base")] [SerializeField] [Tooltip("The empty game object located at the base of the blade")]
    private GameObject @base;

    [FormerlySerializedAs("_meshParent")]
    [SerializeField]
    [Tooltip("The mesh object with the mesh filter and mesh renderer")]
    private GameObject meshParent;

    [FormerlySerializedAs("_trailFrameLength")]
    [SerializeField]
    [Tooltip("The number of frame that the trail should be rendered for")]
    private int trailFrameLength = 3;

    [FormerlySerializedAs("_colour")]
    [SerializeField]
    [ColorUsage(true, true)]
    [Tooltip("The colour of the blade and trail")]
    private Color colour = Color.red;

    [SerializeField] [Tooltip("The amount of force applied to each side of a slice")]
    private Vector3 forceAppliedToCut;

    private int frameCount;
    private Vector3 triggerEnterTipPosition;
    private Vector3 triggerEnterBasePosition;

    private static readonly int
        Color8F0C0815 = Shader.PropertyToID("Color_8F0C0815"),
        ColorAf2E1Bb = Shader.PropertyToID("Color_AF2E1BB");

    private void Start()
    {
        //Init mesh and triangles
        meshParent.transform.position = Vector3.zero;
        var mesh = new Mesh();
        meshParent.GetComponent<MeshFilter>().mesh = mesh;

        var trailMaterial = Instantiate(meshParent.GetComponent<MeshRenderer>().sharedMaterial);
        trailMaterial.SetColor(Color8F0C0815, colour);
        meshParent.GetComponent<MeshRenderer>().sharedMaterial = trailMaterial;

        var bladeMaterial = Instantiate(blade.GetComponent<MeshRenderer>().sharedMaterial);
        bladeMaterial.SetColor(ColorAf2E1Bb, colour);
        blade.GetComponent<MeshRenderer>().sharedMaterial = bladeMaterial;
    }

    private void LateUpdate()
    {
        //Reset the frame count one we reach the frame length
        if (frameCount == trailFrameLength * NUM_VERTICES)
            frameCount = 0;

        frameCount += NUM_VERTICES;
    }

    private void OnTriggerEnter(Collider other)
    {
        triggerEnterTipPosition = tip.transform.position;
        triggerEnterBasePosition = @base.transform.position;
    }

    private void OnTriggerExit(Collider other)
    {
        Slice(other.gameObject, tip.transform.position, triggerEnterBasePosition, triggerEnterTipPosition,
            forceAppliedToCut);
    }

    public static void Slice(GameObject other, Vector3 exitPosition, Vector3 enterBase, Vector3 enterTip,
        Vector3 cutForce)
    {
        //Create a triangle between the tip and base so that we can get the normal
        var sliceTriangleSide1 = exitPosition - enterTip;
        var sliceTriangleSide2 = exitPosition - enterBase;

        //Get the point perpendicular to the triangle above which is the normal
        //https://docs.unity3d.com/Manual/ComputingNormalPerpendicularVector.html
        var normal = Vector3.Cross(sliceTriangleSide1, sliceTriangleSide2).normalized;

        //Transform the normal so that it is aligned with the object we are slicing's transform.
        var objectTranspose = other.transform.localToWorldMatrix.transpose;
        var transformedNormal = (Vector3)(objectTranspose * normal).normalized;

        //Get the enter position relative to the object we're cutting's local transform
        var transformedStartingPoint = other.transform.InverseTransformPoint(enterTip);

        var plane = new Plane();

        plane.SetNormalAndPosition(transformedNormal, transformedStartingPoint);

        var direction = Vector3.Dot(Vector3.up, transformedNormal);

        //Flip the plane so that we always know which side the positive mesh is on
        if (direction < 0) plane = plane.flipped;

        var slices = Slicer.Slice(plane, other);
        Destroy(other);

        var rigidbody = slices[1].GetComponent<Rigidbody>();

        if (!rigidbody || cutForce.magnitude <= 0) return;

        var newNormal = transformedNormal + cutForce;
        rigidbody.AddForce(newNormal, ForceMode.Impulse);
    }
}