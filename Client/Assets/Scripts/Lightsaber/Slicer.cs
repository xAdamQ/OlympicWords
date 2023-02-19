using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Scripts
{
    public static class Slicer
    {
        /// <summary>
        /// Slice the object by the plane 
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="objectToCut"></param>
        /// <returns></returns>
        public static GameObject[] Slice(Plane plane, GameObject objectToCut)
        {
            //Get the current mesh and its verts and tris
            var mesh = objectToCut.GetComponent<MeshFilter>().mesh;
            mesh.GetSubMesh(0);
            var sliceable = objectToCut.GetComponent<Sliceable>();

            if (sliceable == null)
                throw new NotSupportedException
                    ("Cannot slice non sliceable object, add the sliceable script to the object or inherit from sliceable to support slicing");

            //Create left and right slice of hollow object
            var slicesMeta = new SlicesMetadata(plane, mesh, sliceable.IsSolid,
                sliceable.ReverseWireTriangles, sliceable.ShareVertices, sliceable.SmoothVertices);

            var positiveObject = CreateMeshGameObject(objectToCut);
            positiveObject.name = $"{objectToCut.name}_positive";

            var negativeObject = CreateMeshGameObject(objectToCut);
            negativeObject.name = $"{objectToCut.name}_negative";

            var positiveSideMeshData = slicesMeta.PositiveSideMesh;
            var negativeSideMeshData = slicesMeta.NegativeSideMesh;

            positiveObject.GetComponent<MeshFilter>().mesh = positiveSideMeshData;
            negativeObject.GetComponent<MeshFilter>().mesh = negativeSideMeshData;

            SetupCollidersAndRigidBodys(ref positiveObject, positiveSideMeshData, sliceable.UseGravity);
            SetupCollidersAndRigidBodys(ref negativeObject, negativeSideMeshData, sliceable.UseGravity);

            return new GameObject[] { positiveObject, negativeObject };
        }

        /// <summary>
        /// Creates the default mesh game object.
        /// </summary>
        /// <param name="originalObject">The original object.</param>
        /// <returns></returns>
        private static GameObject CreateMeshGameObject(GameObject originalObject)
        {
            var originalMaterial = originalObject.GetComponent<MeshRenderer>().materials;

            var meshGameObject = new GameObject();
            var originalSliceable = originalObject.GetComponent<Sliceable>();

            meshGameObject.AddComponent<MeshFilter>();
            meshGameObject.AddComponent<MeshRenderer>();
            var sliceable = meshGameObject.AddComponent<Sliceable>();

            sliceable.IsSolid = originalSliceable.IsSolid;
            sliceable.ReverseWireTriangles = originalSliceable.ReverseWireTriangles;
            sliceable.UseGravity = originalSliceable.UseGravity;

            meshGameObject.GetComponent<MeshRenderer>().materials = originalMaterial;

            meshGameObject.transform.localScale = originalObject.transform.localScale;
            meshGameObject.transform.rotation = originalObject.transform.rotation;
            meshGameObject.transform.position = originalObject.transform.position;

            meshGameObject.tag = originalObject.tag;

            return meshGameObject;
        }

        /// <summary>
        /// Add mesh collider and rigid body to game object
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="mesh"></param>
        private static void SetupCollidersAndRigidBodys(ref GameObject gameObject, Mesh mesh, bool useGravity)
        {
            if (mesh.vertexCount == 0)
            {
                Debug.Log("vet count 0");
                return;
            }

            var meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = useGravity;
        }
    }
}