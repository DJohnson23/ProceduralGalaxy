using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Slicing
{
    public class Slicer
    {
        public static GameObject[] Slice(Plane plane, GameObject objectToCut)
        {
            Mesh mesh = objectToCut.GetComponent<MeshFilter>().mesh;

            Sliceable sliceable = objectToCut.GetComponent<Sliceable>();

            if(sliceable == null)
            {
                throw new NotSupportedException("Cannot slice non sliceable object, add the Sliceable script to the object or inherit from Sliceable to support slicing");
            }

            SliceData sliceData = new SliceData(plane, mesh, sliceable.isSolid, sliceable.shareVertices);

            GameObject positiveObject = CreateMeshGameObject(objectToCut);
            positiveObject.name = string.Format("{0}_positive", objectToCut.name);

            GameObject negativeObject = CreateMeshGameObject(objectToCut);
            negativeObject.name = string.Format("{0}_negative", objectToCut.name);

            Mesh positiveMesh = sliceData.PositiveMesh;
            Mesh negativeMesh = sliceData.NegativeMesh;

            positiveMesh.RecalculateNormals();
            negativeMesh.RecalculateNormals();

            positiveObject.GetComponent<MeshFilter>().mesh = positiveMesh;
            negativeObject.GetComponent<MeshFilter>().mesh = negativeMesh;

            SetupCollidersAndRigidBodies(ref positiveObject, positiveMesh, sliceable.useGravity);
            SetupCollidersAndRigidBodies(ref negativeObject, negativeMesh, sliceable.useGravity);

            return new GameObject[] { positiveObject, negativeObject };
        }

        private static GameObject CreateMeshGameObject(GameObject originalObject)
        {
            Material[] originalMaterials = originalObject.GetComponent<MeshRenderer>().materials;

            GameObject meshGameObject = new GameObject();
            Sliceable originalSliceable = originalObject.GetComponent<Sliceable>();

            meshGameObject.AddComponent<MeshFilter>();
            meshGameObject.AddComponent<MeshRenderer>();
            Sliceable sliceable = meshGameObject.AddComponent<Sliceable>();

            sliceable.isSolid = originalSliceable.isSolid;
            sliceable.useGravity = originalSliceable.useGravity;
            sliceable.forceOnSlice = originalSliceable.forceOnSlice;
            sliceable.maxIterations = originalSliceable.maxIterations;
            sliceable.curIteration = originalSliceable.curIteration + 1;

            meshGameObject.GetComponent<MeshRenderer>().materials = originalMaterials;

            meshGameObject.transform.localScale = originalObject.transform.localScale;
            meshGameObject.transform.rotation = originalObject.transform.rotation;
            meshGameObject.transform.position = originalObject.transform.position;

            meshGameObject.tag = originalObject.tag;

            return meshGameObject;
        }

        private static void SetupCollidersAndRigidBodies(ref GameObject gameObject, Mesh mesh, bool useGravity)
        {
            MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true;

            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity = useGravity;
        }
    }
}
