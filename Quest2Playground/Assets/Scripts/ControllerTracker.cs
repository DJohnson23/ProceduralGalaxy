using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ControllerTracker : MonoBehaviour
{
    [SerializeField]
    OVRInput.Controller controller;
    [SerializeField]
    GameObject grabObject;
    public float maxPointerDistance = 5f;
    public float pointerStartDistance = 0.1f;

    private LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateInput();
        UpdateLine();
    }

    void UpdateInput()
    {
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, controller))
        {
            grabObject.transform.position = transform.position;
            grabObject.transform.rotation = transform.rotation;
        }
    }

    void UpdateLine()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, Vector3.forward * pointerStartDistance);

        RaycastHit hit;

        
        if(Physics.Raycast(transform.position + (transform.forward * pointerStartDistance), transform.forward, out hit, maxPointerDistance))
        {
            float distance = Vector3.Distance(transform.position, hit.point);
            lineRenderer.SetPosition(1, Vector3.forward * distance);
        }
        else
        {
            lineRenderer.SetPosition(1, Vector3.forward * maxPointerDistance);
        }
    }
}
