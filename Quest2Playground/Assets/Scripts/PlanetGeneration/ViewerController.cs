using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewerController : MonoBehaviour
{
    /*
    public float thrustForce = 5f;
    public float turnForce = 5f;
    public float mass = 1f;
    */
    public float moveSpeed = 1f;

    public PlanetGenerator planet;

    Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float depth = Input.GetAxis("Vertical");

        float vertical = 0;

        if(Input.GetKey(KeyCode.Q))
        {
            vertical += 1;
        }
        
        if(Input.GetKey(KeyCode.E))
        {
            vertical -= 1;
        }

        transform.Translate(new Vector3(horizontal, vertical, depth).normalized * moveSpeed * Time.deltaTime, Space.Self);

        //transform.LookAt(transform.position + GetTangent(), transform.position - planet.transform.position);

        /*
        if(Input.GetKey(KeyCode.Space))
        {
            rb.AddRelativeForce(Vector3.forward * thrustForce * Time.deltaTime);
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        rb.AddRelativeTorque(Vector3.up * horizontal * turnForce * Time.deltaTime);
        rb.AddRelativeTorque(-Vector3.right * vertical * turnForce * Time.deltaTime);

        rb.AddForce(getGravitationalPull(planet) * Time.deltaTime, ForceMode.Acceleration);
        */
    }

    /*
    Vector3 getGravitationalPull(PlanetGenerator planetGen)
    {
        Vector3 direction = (planetGen.transform.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, planetGen.transform.position);

        float magnitude = mass * planetGen.mass / (distance * distance);

        return direction * magnitude;
    }
    */

    Vector3 GetTangent()
    {
        Vector3 dir = (transform.position - planet.transform.position).normalized;

        Vector3 t1 = Vector3.Cross(dir, Vector3.forward);
        Vector3 t2 = Vector3.Cross(dir, Vector3.up);

        if(t1.magnitude > t2.magnitude)
        {
            return t1;
        }

        return t2;
    }
}
