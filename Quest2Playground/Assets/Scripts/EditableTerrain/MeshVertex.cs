using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshVertex
{
    public Vector3 position;
    public Vector3 normal;
    public Vector2 uv;

    public List<MeshTriangle> triangles;
    public int index;

    public MeshVertex(Vector3 position)
    {
        triangles = new List<MeshTriangle>();
        this.position = position;
        index = -1;
    }

    public void CalculateNormal()
    {
        normal = new Vector3();

        foreach(MeshTriangle tri in triangles)
        {
            normal += tri.Normal;
        }

        normal.Normalize();
    }
}
