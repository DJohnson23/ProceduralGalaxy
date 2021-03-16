using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class OrbitDebugDisplay : MonoBehaviour
{
    public int numSteps = 1000;
    public float timeStep = 0.1f;
    public bool usePhysicsTimeStep;

    public bool relativeToBody;
    public CelestialBody centralBody;
    public float width = 100;
    public bool useThickLines;

    // Start is called before the first frame update
    void Start()
    {
        if(Application.isPlaying)
        {
            HideOrbits();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(!Application.isPlaying)
        {
            DrawOrbits();
        }
    }
    
    void DrawOrbits()
    {
        CelestialBody[] bodies = FindObjectsOfType<CelestialBody>();
        VirtualBody[] virtualBodies = new VirtualBody[bodies.Length];
        Vector3[][] drawPoints = new Vector3[bodies.Length][];
        int referenceFrameIndex = 0;
        Vector3 referenceBodyInitialPosition = Vector3.zero;

        for(int i = 0; i < virtualBodies.Length; i++)
        {
            virtualBodies[i] = new VirtualBody(bodies[i]);
            drawPoints[i] = new Vector3[numSteps];

            if(bodies[i] == centralBody && relativeToBody)
            {
                referenceFrameIndex = i;
                referenceBodyInitialPosition = virtualBodies[i].position;
            }
        }

        for(int step = 0; step < numSteps; step++)
        {
            Vector3 referenceBodyPosition = (relativeToBody) ? virtualBodies[referenceFrameIndex].position : Vector3.zero;

            for(int i = 0; i < virtualBodies.Length; i++)
            {
                virtualBodies[i].velocity += CalculateAcceleration(i, virtualBodies) * timeStep;
            }

            for(int i = 0; i < virtualBodies.Length; i++)
            {
                Vector3 newPos = virtualBodies[i].position + virtualBodies[i].velocity * timeStep;
                virtualBodies[i].position = newPos;

                if(relativeToBody)
                {
                    if (i == referenceFrameIndex)
                    {
                        newPos = referenceBodyInitialPosition;
                    }
                    else
                    {
                        Vector3 referenceFrameOffset = referenceBodyPosition - referenceBodyInitialPosition;
                        newPos -= referenceFrameOffset;
                    }
                }

                drawPoints[i][step] = newPos;
            }
        }

        for(int bodyIndex = 0; bodyIndex < virtualBodies.Length; bodyIndex++)
        {
            Color pathColor = bodies[bodyIndex].gameObject.GetComponentInChildren<MeshRenderer>().sharedMaterial.color;

            if(useThickLines)
            {
                LineRenderer lineRenderer = bodies[bodyIndex].gameObject.GetComponentInChildren<LineRenderer>();
                lineRenderer.enabled = true;
                lineRenderer.positionCount = numSteps;
                lineRenderer.SetPositions(drawPoints[bodyIndex]);
                lineRenderer.startColor = pathColor;
                lineRenderer.endColor = pathColor;
                lineRenderer.widthMultiplier = width;
            }
            else
            {
                for(int i = 0; i < drawPoints[bodyIndex].Length - 1; i++)
                {
                    Debug.DrawLine(drawPoints[bodyIndex][i], drawPoints[bodyIndex][i + 1], pathColor);
                }

                LineRenderer lineRenderer = bodies[bodyIndex].gameObject.GetComponentInChildren<LineRenderer>();
                if(lineRenderer)
                {
                    lineRenderer.enabled = false;
                }
            }
        }

    }

    Vector3 CalculateAcceleration(int i, VirtualBody[] virtualBodies)
    {
        Vector3 acceleration = Vector3.zero;
        for(int j = 0; j < virtualBodies.Length; j++)
        {
            if(i == j)
            {
                continue;
            }

            Vector3 forceDir = (virtualBodies[j].position - virtualBodies[i].position).normalized;
            float sqrDst = (virtualBodies[j].position - virtualBodies[i].position).sqrMagnitude;
            acceleration += forceDir * Universe.gravitationalConstant * virtualBodies[j].mass / sqrDst;
        }

        return acceleration;
    }

    void HideOrbits()
    {
        CelestialBody[] bodies = FindObjectsOfType<CelestialBody>();

        for(int bodyIndex = 0; bodyIndex < bodies.Length; bodyIndex++)
        {
            LineRenderer lineRenderer = bodies[bodyIndex].gameObject.GetComponentInChildren<LineRenderer>();
            lineRenderer.positionCount = 0;
        }
    }

    private void OnValidate()
    {
        if(usePhysicsTimeStep)
        {
            timeStep = Universe.physicsTimeStep;
        }
    }

    class VirtualBody
    {
        public Vector3 position;
        public Vector3 velocity;
        public float mass;

        public VirtualBody(CelestialBody body)
        {
            position = body.transform.position;
            velocity = body.initialVelocity;
            mass = body.mass;
        }
    }
}
