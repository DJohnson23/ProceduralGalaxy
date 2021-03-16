using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshTriangle
{
    public MeshVertex a;
    public MeshVertex b;
    public MeshVertex c;

    public Vector3 Normal
    {
        get
        {
            return Vector3.Cross(b.position - a.position, c.position - a.position).normalized;
        }
    }

    public MeshTriangle(ref MeshVertex a, ref MeshVertex b, ref MeshVertex c)
    {
        a.triangles.Add(this);
        b.triangles.Add(this);
        c.triangles.Add(this);

        this.a = a;
        this.b = b;
        this.c = c;
    }
}
