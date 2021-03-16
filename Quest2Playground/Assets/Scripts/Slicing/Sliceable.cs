using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Slicing
{
    public class Sliceable : MonoBehaviour
    {
        public bool isSolid = true;
        public bool shareVertices = true;
        public bool useGravity = true;
        public float forceOnSlice = 3f;

        [Min(0)]
        public int maxIterations = 2;

        [HideInInspector]
        public int curIteration = 0;

        Vector3 startTip;
        Vector3 startBase;

        Vector3 endTip;

        private void OnTriggerEnter(Collider other)
        {
            if(maxIterations > 0 && curIteration >= maxIterations)
            {
                return;
            }

            LightSaberController saberController = other.GetComponent<LightSaberController>();

            if (saberController == null)
            {
                return;
            }

            Transform tip = saberController.GetTipJointTranform();

            startTip = tip.position;
            startBase = tip.parent.position;
        }

        private void OnTriggerExit(Collider other)
        {
            if(maxIterations > 0 && curIteration >= maxIterations)
            {
                return;
            }

            LightSaberController saberController = other.GetComponent<LightSaberController>();

            if (saberController == null)
            {
                return;
            }

            endTip = saberController.GetTipJointTranform().position;

            MeshFilter meshFilter = GetComponent<MeshFilter>();

            Plane plane = new Plane(transform.InverseTransformPoint(startBase), transform.InverseTransformPoint(startTip), transform.InverseTransformPoint(endTip));

            GameObject[] newObjects = Slicer.Slice(plane, gameObject);

            newObjects[0].GetComponent<Rigidbody>().AddForce(plane.normal * forceOnSlice, ForceMode.Impulse);
            newObjects[1].GetComponent<Rigidbody>().AddForce(-plane.normal * forceOnSlice, ForceMode.Impulse);
            Destroy(gameObject);
        }
    }
}

