using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDigTest : MonoBehaviour
{
    public EditableTerrain terrain;
    public float digStrength = 10;
    public float radius = 5;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            //terrain.Dig(transform.position, transform.forward, radius, digStrength);
        }
    }
}
