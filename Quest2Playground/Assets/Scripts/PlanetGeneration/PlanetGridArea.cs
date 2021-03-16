using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetGridArea : MonoBehaviour
{
    public float size;

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, Vector3.one * size);
    }
}
