using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingCubeDigTest : MonoBehaviour
{
    public float digSpeed = 0.5f;
    public float radius = 2f;

    private void Update()
    {
        
        PlanetChunk[] allGrids = FindObjectsOfType<PlanetChunk>();

        foreach(PlanetChunk cubeGrid in allGrids)
        {
            cubeGrid.Dig(transform.position, radius, digSpeed * Time.deltaTime);
        }
        
    }

    private void OnDrawGizmos()
    {

        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
