using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceHandController : MonoBehaviour
{
    public bool showGizmos = false;
    public OVRInput.Controller controller;
    public float maxForceDistance = 10f;
    public float forceSightAngle = 30f;

    public float forceHoldDistance = .5f;
    public float pullSpeed = 0.1f;

    public int focusHistoryLength = 5;
    public float forceRadius = .5f;
    public int forceLookSteps = 2;

    [SerializeField]
    ForceLightning forceLightning;

    List<Forceable> forceObjectsInRange;

    Forceable currentFocus;

    Vector3[] focusHistory;
    int focusHistoryIndex;

    MeshCollider viewCollider;

    float origEndSpread;

    enum ForceState
    {
        Idle,
        Pulling,
        Lightning
    }

    ForceState forceState;

    // Start is called before the first frame update
    void Start()
    {
        forceState = ForceState.Idle;
        forceLightning.enabled = false;
        origEndSpread = forceLightning.endSpread;
        forceObjectsInRange = new List<Forceable>();
        GenerateCollider();
    }

    // Update is called once per frame
    void Update()
    {
        CheckFocus();
        ForcePull();
        ForceLightning();
    }

    void GenerateCollider()
    {
        Mesh coneMesh = new Mesh();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        float radius = maxForceDistance * Mathf.Sin(Mathf.Deg2Rad * forceSightAngle);

        int capVerts = Mathf.RoundToInt(radius * 2 * Mathf.PI);
        float angleStep = Mathf.PI * 2 / capVerts;
        Vector3 center = Vector3.forward * maxForceDistance;

        Vector3 firstPoint = center + Vector3.right * radius;

        vertices.Add(Vector3.zero);
        vertices.Add(center);
        vertices.Add(firstPoint);

        for (int i = 1; i < capVerts; i++)
        {
            float x = radius * Mathf.Cos(i * angleStep);
            float y = radius * Mathf.Sin(i * angleStep);

            Vector3 point = center + new Vector3(x, y, 0);

            vertices.Add(point);

            triangles.Add(0);
            triangles.Add(vertices.Count - 1);
            triangles.Add(vertices.Count - 2);

            triangles.Add(1);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);
        }

        triangles.Add(0);
        triangles.Add(2);
        triangles.Add(vertices.Count - 1);

        triangles.Add(1);
        triangles.Add(vertices.Count - 1);
        triangles.Add(2);

        coneMesh.vertices = vertices.ToArray();
        coneMesh.triangles = triangles.ToArray();

        viewCollider = gameObject.GetComponent<MeshCollider>();
        if(viewCollider == null)
        {
            viewCollider = gameObject.AddComponent<MeshCollider>();
        }

        viewCollider.sharedMesh = coneMesh;

        viewCollider.convex = true;
        viewCollider.isTrigger = true;
    }

    void ForceLightning()
    {
        if(OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controller))
        {
            if(forceState == ForceState.Idle)
            {
                forceState = ForceState.Lightning;
                forceLightning.enabled = true;
            }

            if(forceState == ForceState.Lightning)
            {
                if(currentFocus == null)
                {
                    forceLightning.endPos = transform.position + transform.forward * maxForceDistance;
                    forceLightning.endSpread = origEndSpread;
                }
                else
                {
                    forceLightning.endPos = currentFocus.transform.position;
                    forceLightning.endSpread = 0;
                }
            }
        }
        else if(forceState == ForceState.Lightning)
        {
            forceState = ForceState.Idle;
            forceLightning.enabled = false;
        }
    }

    void ForcePull()
    {
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, controller) && currentFocus != null)
        {
            if(forceState != ForceState.Pulling && forceState != ForceState.Idle)
            {
                return;
            }

            Rigidbody rb = currentFocus.GetComponent<Rigidbody>();

            if (rb != null && !rb.isKinematic)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if(forceState != ForceState.Pulling)
            {
                forceState = ForceState.Pulling;
                focusHistory = new Vector3[focusHistoryLength];
                focusHistoryIndex = 0;
            }
            Vector3 pullPosition = transform.position + transform.forward * forceHoldDistance;

            Vector3 direction = (pullPosition - currentFocus.transform.position).normalized;
            float distance = Vector3.Distance(currentFocus.transform.position, pullPosition);

            Vector3 delta = direction * pullSpeed;
            Vector3 newPosition;
            if (delta.magnitude >= distance)
            {
                newPosition = pullPosition;
            }
            else
            {
                newPosition = currentFocus.transform.position + delta;
            }

            focusHistory[focusHistoryIndex] = (newPosition - currentFocus.transform.position) / Time.deltaTime;
            focusHistoryIndex = (focusHistoryIndex + 1) % focusHistoryLength;

            currentFocus.transform.position = newPosition;
        }
        else if(forceState == ForceState.Pulling)
        {
            Rigidbody rb = currentFocus.GetComponent<Rigidbody>();

            if(rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;

                Vector3 velocity = new Vector3();
                foreach(Vector3 vel in focusHistory)
                {
                    velocity += vel;
                }

                velocity /= focusHistoryLength;

                rb.velocity = velocity;
            }

            forceState = ForceState.Idle;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Forceable forceable = other.GetComponent<Forceable>();

        if(forceable != null)
        {
            forceObjectsInRange.Add(forceable);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Forceable forceable = other.GetComponent<Forceable>();

        if(forceable != null)
        {
            if(currentFocus == forceable && forceState != ForceState.Pulling)
            {
                EndFocus();
            }

            int index = forceObjectsInRange.IndexOf(forceable);

            if(index != -1)
            {
                forceObjectsInRange.RemoveAt(index);
            }
        }
    }

    void CheckFocus()
    {
        if(forceState == ForceState.Pulling)
        {
            return;
        }

        foreach(Forceable forceable in forceObjectsInRange)
        {
            Renderer renderer = forceable.GetComponent<Renderer>();

            if(renderer != null && renderer.isVisible)
            {
                FocusForceObject(forceable);
                return;
            }
        }

        EndFocus();
    }

    void FocusForceObject(Forceable forceable)
    {
        if(forceable != currentFocus)
        {
            EndFocus();
        }

        if (currentFocus == null)
        {
            forceable.Focused = true;
            currentFocus = forceable;
        }
    }

    void EndFocus()
    {
        if(currentFocus != null)
        {
            currentFocus.Focused = false;
            currentFocus = null;
        }
    }

    private void OnDrawGizmos()
    {
        if(!showGizmos)
        {
            return;
        }

        Gizmos.color = Color.blue;
        Vector3 holdPosition = transform.position + transform.forward * forceHoldDistance;
        Gizmos.DrawLine(transform.position, holdPosition);
        Gizmos.DrawWireSphere(holdPosition, .1f);

        float radius = maxForceDistance * Mathf.Sin(Mathf.Deg2Rad * forceSightAngle);

        int numLines = Mathf.RoundToInt(radius * 2 * Mathf.PI);
        float angleStep = Mathf.PI * 2 / numLines;
        Vector3 center = transform.position + transform.forward * maxForceDistance;

        Vector3 firstPoint = center + transform.right * radius;
        Vector3 prevPoint = firstPoint;

        for(int i = 1; i < numLines; i++)
        {
            float x = radius * Mathf.Cos(i * angleStep);
            float y = radius * Mathf.Sin(i * angleStep);

            Vector3 point = center + transform.right * x + transform.up * y;

            Gizmos.DrawLine(prevPoint, point);

            prevPoint = point;
        }

        Gizmos.DrawLine(prevPoint, firstPoint);
        Gizmos.DrawLine(transform.position, center + transform.right * radius);
        Gizmos.DrawLine(transform.position, center + -transform.right * radius);
        Gizmos.DrawLine(transform.position, center + transform.up * radius);
        Gizmos.DrawLine(transform.position, center + -transform.up * radius);
    }
}
