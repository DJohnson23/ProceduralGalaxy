using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Spaceship : GravityObject
{
    public float mass;
    public float acceleration;
    public float rotSpeed;
    public float rollSpeed;
    public float rotSmoothSpeed;
    public bool lockCursor;

    Rigidbody rb;
    Quaternion targetRot;
    Quaternion smoothedRot;
    Vector3 input;

    public LayerMask groundedMask;
    int numCollisionTouches;

    private void Awake()
    {
        InitRigidbody();

        targetRot = transform.rotation;
        smoothedRot = transform.rotation;

        if(lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        int verticalThrust = 0;
        if(Input.GetKey(KeyCode.Space))
        {
            if(Input.GetKey(KeyCode.LeftShift))
            {
                verticalThrust = -1;
            }
            else
            {
                verticalThrust = 1;
            }
        }

        input = new Vector3(Input.GetAxisRaw("Horizontal"), verticalThrust, Input.GetAxisRaw("Vertical"));

        float yawInput = Input.GetAxisRaw("Mouse X") * rotSpeed;
        float pitchInput = Input.GetAxisRaw("Mouse Y") * rotSpeed;
        float rollInput = (Input.GetKey(KeyCode.Q) ? -1 : Input.GetKey(KeyCode.E) ? 1 : 0) * rollSpeed * Time.deltaTime;

        if (numCollisionTouches == 0)
        {
            Quaternion yaw = Quaternion.AngleAxis(yawInput, transform.up);
            Quaternion pitch = Quaternion.AngleAxis(-pitchInput, transform.right);
            Quaternion roll = Quaternion.AngleAxis(-rollInput, transform.forward);

            targetRot = yaw * pitch * roll * targetRot;
            smoothedRot = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotSmoothSpeed);
        }
        else
        {
            targetRot = transform.rotation;
            smoothedRot = transform.rotation;
        }

        Debug.Log("Grounded: " + (numCollisionTouches > 0));
    }

    private void FixedUpdate()
    {
        CelestialBody[] bodies = FindObjectsOfType<CelestialBody>();

        foreach(CelestialBody body in bodies)
        {
            Vector3 offsetToBody = body.transform.position - rb.position;
            float sqrDst = offsetToBody.sqrMagnitude;
            Vector3 dirToBody = offsetToBody / Mathf.Sqrt(sqrDst);
            float acceleration = Universe.gravitationalConstant * body.mass / sqrDst;
            rb.AddForce(dirToBody * acceleration, ForceMode.Acceleration);
        }

        Vector3 rocketAcceleration = transform.TransformVector(input) * acceleration;
        rb.AddForce(rocketAcceleration, ForceMode.Acceleration);

        if(numCollisionTouches == 0)
        {
            rb.MoveRotation(smoothedRot);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(groundedMask == (groundedMask | (1 << collision.gameObject.layer)))
        {
            numCollisionTouches++;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if(groundedMask == (groundedMask | (1 << collision.gameObject.layer)))
        {
            numCollisionTouches--;
        }
    }

    void InitRigidbody()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.mass = mass;
        rb.centerOfMass = Vector3.zero;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void OnValidate()
    {
        InitRigidbody();
    }
}
