﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public struct Triangle
{
    public Vector3 A;
    public Vector3 B;
    public Vector3 C;
}

public struct GridPoint
{
    public Vector3 position;
    public float val;
}

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshCollider))]
public class PlanetChunk : MonoBehaviour
{
    public bool drawGizmos = false;
    public float size = 100;
    public int resolution = 5;
    [Range(0f, 1f)]
    public float surfaceLevel = 0.5f;
    public ComputeShader marchingCubeShader;
    public ComputeShader planetChunkInitShader;
    [SerializeField]
    public SimplexNoiseSettings noiseSettings;

    public Vector3 center = new Vector3(0, 0, 0);
    public float surfaceHeight = 15f;
    public float coreRadius = 50f;

    public int LOD
    {
        get
        {
            return lod;
        }

        set
        {
            if(lod != value)
            {
                lod = value;
                refreshMesh = true;
            }
        }
    }

    private int lod = 0;
    private GridPoint[] gridPoints;

    MeshFilter meshFilter;
    MeshCollider meshCollider;
    SimplexNoiseGenerator noiseGenerator;

    ComputeBuffer triangleBuffer;
    ComputeBuffer gridPointBuffer;
    ComputeBuffer argBuffer;

    int pointsPerRow;
    int numCells;

    bool refreshMesh = false;
    bool initialized = false;
    Vector3 worldPosition;

    private void Start()
    {
        InitChunk();
    }

    private void Update()
    {

        if (refreshMesh)
        {
            GenerateMesh();
            refreshMesh = false;
        }
    }

    public void SetVisible(bool visible)
    {
        GetComponent<MeshRenderer>().enabled = visible;
    }

    public void InitChunk()
    {
        worldPosition = transform.position;
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        ThreadStart threadStart = delegate
        {
            InitPoints();
        };

        new Thread(threadStart).Start();
    }

    private void InitPoints()
    {
        noiseGenerator = new SimplexNoiseGenerator(noiseSettings.seed);

        pointsPerRow = (int)Mathf.Pow(2, resolution) + 1;
        numCells = (int)Mathf.Pow(8, resolution);

        gridPoints = new GridPoint[pointsPerRow * pointsPerRow * pointsPerRow];

        float step = size / (pointsPerRow - 1);
        float halfSize = size / 2;

        for (int x = 0; x < pointsPerRow; x++)
        {
            for (int y = 0; y < pointsPerRow; y++)
            {
                for (int z = 0; z < pointsPerRow; z++)
                {
                    int index = to1D(x, y, z);
                    Vector3 position = new Vector3(x * step - halfSize, y * step - halfSize, z * step - halfSize);
                    float val = CalcPointValue(position);
                    gridPoints[index] = new GridPoint();
                    gridPoints[index].position = position;
                    gridPoints[index].val = val;
                }
            }
        }

        initialized = true;
        refreshMesh = true;
    }

    private void GenerateMesh()
    {
        
        if(!initialized)
        {
            Debug.LogError("Planet Chunk not initialized - Can not Generate Mesh");
            return;
        }
        

        if(marchingCubeShader == null)
        {
            Debug.LogError("No Compute Shader Set for Planet Chunk");
            return;
        }

        gridPointBuffer = new ComputeBuffer(gridPoints.Length, sizeof(float) * 4);
        gridPointBuffer.SetData(gridPoints);

        triangleBuffer = new ComputeBuffer(5 * numCells, sizeof(float) * 9, ComputeBufferType.Append);
        triangleBuffer.SetCounterValue(0);

        marchingCubeShader.SetBuffer(0, "gridPoints", gridPointBuffer);
        marchingCubeShader.SetBuffer(0, "triangles", triangleBuffer);
        marchingCubeShader.SetFloat("isoLevel", surfaceLevel);
        marchingCubeShader.SetInt("pointsPerRow", pointsPerRow);
        marchingCubeShader.SetInt("incr", (int)Mathf.Pow(2, lod));

        int cellsPerRow = pointsPerRow - 1;

        int numGroups = Mathf.CeilToInt(cellsPerRow / 8.0f / (lod + 1));
        marchingCubeShader.Dispatch(0, numGroups, numGroups, numGroups);

        int[] args = { 0 };
        argBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.IndirectArguments);
        argBuffer.SetData(args);

        ComputeBuffer.CopyCount(triangleBuffer, argBuffer, 0);
        argBuffer.GetData(args);

        Triangle[] newTriangles = new Triangle[args[0]];
        triangleBuffer.GetData(newTriangles);

        Vector3[] vertices = new Vector3[newTriangles.Length * 3];
        int[] triangles = new int[newTriangles.Length * 3];

        for (int i = 0; i < newTriangles.Length; i++)
        {
            Triangle triangle = newTriangles[i];
            vertices[i * 3] = triangle.A;
            vertices[i * 3 + 1] = triangle.B;
            vertices[i * 3 + 2] = triangle.C;

            triangles[i * 3] = i * 3;
            triangles[i * 3 + 1] = i * 3 + 1;
            triangles[i * 3 + 2] = i * 3 + 2;
        }

        Mesh mesh = new Mesh();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;

        ReleaseBuffers();
    }

    float CalcPointValue(Vector3 point)
    {
        Vector3 worldPoint = worldPosition + point;
        float distance = Vector3.Distance(worldPoint, center);

        if(distance <= coreRadius)
        {
            return 1f;
        }
        else if(distance > coreRadius + surfaceHeight)
        {
            return 0f;
        }

        return 0;
        //return noiseGenerator.coherentNoise(worldPoint.x, worldPoint.y, worldPoint.z, noiseSettings);
    }

    void ReleaseBuffers()
    {
        if (triangleBuffer != null)
        {
            triangleBuffer.Release();
            gridPointBuffer.Release();
            argBuffer.Release();
        }
    }

    int to1D(int x, int y, int z)
    {
        return (z * pointsPerRow * pointsPerRow) + (y * pointsPerRow) + x;
    }

    public void Dig(Vector3 position, float radius, float amt)
    {
        float step = size / (pointsPerRow - 1);
        int cellsPerRow = pointsPerRow - 1;

        Vector3 localPos = transform.InverseTransformPoint(position);
        Vector3Int min = new Vector3Int();
        min.x = Mathf.Max(Mathf.CeilToInt((localPos.x - radius) / step), 0);
        min.y = Mathf.Max(Mathf.CeilToInt((localPos.y - radius) / step), 0);
        min.z = Mathf.Max(Mathf.CeilToInt((localPos.z - radius) / step), 0);

        Vector3Int max = new Vector3Int();
        max.x = Mathf.Min(Mathf.FloorToInt((localPos.x + radius) / step), cellsPerRow);
        max.y = Mathf.Min(Mathf.FloorToInt((localPos.y + radius) / step), cellsPerRow);
        max.z = Mathf.Min(Mathf.FloorToInt((localPos.z + radius) / step), cellsPerRow);

        bool found = false;

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    int index = to1D(x, y, z);
                    GridPoint point = gridPoints[index];
                    float distance = Vector3.Distance(localPos, point.position);
                    if (distance <= radius)
                    {
                        float before = point.val;
                        point.val = Mathf.Min(Mathf.Max(point.val - amt, 0f), 1f);
                        gridPoints[index] = point;

                        if (point.val != before)
                        {
                            found = true;
                        }
                    }
                }
            }
        }

        if (found)
        {
            Debug.Log("Dug");
            GenerateMesh();
        }
    }

    private void OnDrawGizmos()
    {

        if (gridPoints == null)
        {
            return;
        }

        if (!drawGizmos)
        {
            return;
        }

        Gizmos.DrawWireCube(transform.TransformPoint(Vector3.one * size / 2), Vector3.one * size);

        for (int i = 0; i < gridPoints.Length; i++)
        {
            GridPoint point = gridPoints[i];
            Gizmos.color = new Color(point.val, point.val, point.val);
            Gizmos.DrawSphere(transform.TransformPoint(point.position), 0.1f);
        }
    }
}