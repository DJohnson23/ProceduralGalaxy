using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SphereGeneratorTest : MonoBehaviour
{
    public float radius = 1;
    public int subdivisions = 1;

    MeshFilter meshFilter;

    // Start is called before the first frame update
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = OctahedronSphereGenerator.Create(subdivisions, radius);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
