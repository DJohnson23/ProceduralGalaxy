using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditableTerrainMesh
{
    private float width;
    private float length;
    private int widthRes;
    private int lengthRes;

    private float edgeMin;
    private float edgeMax;

    private List<MeshTriangle> triangles;
    private List<MeshVertex> vertices;

    public EditableTerrainMesh(float width, float length, int widthRes, int lengthRes, float edgeMin, float edgeMax)
    {
        this.width = width;
        this.length = length;
        this.widthRes = widthRes;
        this.lengthRes = lengthRes;
        this.edgeMin = edgeMin;
        this.edgeMax = edgeMax;
        Refresh();
    }

    public void Refresh()
    {
        GenerateTrisAndVerts();
        CalculateNormals();
        GenerateUV();
    }

    private void GenerateTrisAndVerts()
    {
        vertices = new List<MeshVertex>();
        triangles = new List<MeshTriangle>();

        float rowStep = length / lengthRes;
        float colStep = width / widthRes;

        for (int r = 0; r <= lengthRes; r++)
        {
            for (int c = 0; c <= widthRes; c++)
            {
                Vector3 vPos = new Vector3();
                vPos.x = c * colStep;
                vPos.z = r * rowStep;
                vPos.y = 0;

                MeshVertex vert = new MeshVertex(vPos);

                vertices.Add(vert);
            }
        }

        int v = 0;
        for(int r = 0; r <= lengthRes; r++)
        {
            for(int c = 0; c <= widthRes; c++)
            {
                if (c != widthRes && r != lengthRes)
                {
                    MeshVertex v1 = vertices[v];
                    MeshVertex v2 = vertices[v + widthRes + 1];
                    MeshVertex v3 = vertices[v + widthRes + 2];
                    MeshVertex v4 = vertices[v + 1];

                    triangles.Add(new MeshTriangle(ref v1, ref v2, ref v3));
                    triangles.Add(new MeshTriangle(ref v1, ref v3, ref v4));
                }

                v++;
            }
        }
    }

    public void CalculateNormals()
    {
        foreach(MeshVertex vert in vertices)
        {
            vert.CalculateNormal();
        }
    }

    private void GenerateUV()
    {
        for(int i = 0; i < vertices.Count; i++)
        {
            MeshVertex v = vertices[i];

            v.uv.x = v.position.x / width;
            v.uv.y = v.position.z / length;
        }
    }

    public Mesh GetMesh()
    {
        Vector3[] rawVertices = new Vector3[vertices.Count];
        Vector3[] normals = new Vector3[vertices.Count];
        Vector2[] uv = new Vector2[vertices.Count];

        int[] rawTriangles = new int[triangles.Count * 3];

        for(int i = 0; i < vertices.Count; i++)
        {
            MeshVertex v = vertices[i];
            v.index = i;
            rawVertices[i] = v.position;
            normals[i] = v.normal;
            uv[i] = v.uv;
        }

        for(int i = 0; i < triangles.Count; i++)
        {
            MeshTriangle tri = triangles[i];
            rawTriangles[3 * i] = tri.a.index;
            rawTriangles[3 * i + 1] = tri.b.index;
            rawTriangles[3 * i + 2] = tri.c.index;
        }


        Mesh mesh = new Mesh();
        mesh.vertices = rawVertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = rawTriangles;

        return mesh;
    }

    public void Dig(Vector3 position, float radius)
    {
        bool[] safe = new bool[vertices.Count];
        int[] newIndex = new int[vertices.Count];
        List<MeshVertex> newVertices = new List<MeshVertex>();
        List<MeshTriangle> newTriangles = new List<MeshTriangle>();

        int safeCount = 0;

        for(int i = 0; i < vertices.Count; i++)
        {
            MeshVertex v = vertices[i];
            v.index = i;
            float distance = Vector3.Distance(position, v.position);

            if(distance >= radius)
            {
                safe[i] = true;
                newIndex[i] = safeCount++;
                newVertices.Add(v);
            }
            else
            {
                safe[i] = false;
                newIndex[i] = -1;
            }
        }

        for(int i = 0; i < triangles.Count; i++)
        {
            MeshTriangle tri = triangles[i];

            MeshVertex a = tri.a;
            MeshVertex b = tri.b;
            MeshVertex c = tri.c;

            if(safe[a.index] && safe[b.index] && safe[c.index])
            {
                newTriangles.Add(tri);
            }
            else if(safe[a.index])
            {
                if(safe[b.index])
                {
                    SliceTriangleDouble(ref a, ref b, ref c, ref newTriangles);
                }
                else if(safe[c.index])
                {
                    SliceTriangleDouble(ref c, ref a, ref b, ref newTriangles);
                }
                else
                {
                    SliceTriangleSingle(ref a, ref b, ref c, ref newTriangles);
                }
            }
            else if(safe[b.index])
            {
                if (safe[c.index])
                {
                    SliceTriangleDouble(ref b, ref c, ref a, ref newTriangles);
                }
                else
                {
                    SliceTriangleSingle(ref b, ref c, ref a, ref newTriangles);
                }
            }
            else if(safe[c.index])
            {
                SliceTriangleSingle(ref c, ref a, ref b, ref newTriangles);
            }
        }
    }

    private void SliceTriangleSingle(ref MeshVertex vSafe, ref MeshVertex vUnsafe1, ref MeshVertex vUnsafe2, ref List<MeshTriangle> triangles)
    {

    }

    private void SliceTriangleDouble(ref MeshVertex vSafe1, ref MeshVertex vSafe2, ref MeshVertex vUnsafe, ref List<MeshTriangle> triangles)
    {

    }
}
