using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

[ExecuteInEditMode]
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
    public NoiseSettings noiseSettings;

    public Vector3 center = new Vector3(0, 0, 0);
    public float surfaceHeight = 15f;
    public float coreRadius = 50f;

    public System.Action initCallback;

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
                if(initialized)
                {
                    GenerateMesh();
                }
            }
        }
    }

    private int lod = 0;
    private GridPoint[] gridPoints;

    MeshFilter meshFilter;
    MeshCollider meshCollider;

    ComputeBuffer triangleBuffer;
    ComputeBuffer gridPointBuffer;
    ComputeBuffer argBuffer;
    ComputeBuffer initGridPointBuffer;

    int pointsPerRow;
    int numCells;

    public bool initialized { get; private set; } = false;
    Vector3 worldPosition;

    float timeStart;
    Dictionary<int, Mesh> lodMeshes;

    private void Start()
    {
        InitChunk();
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
        lodMeshes = new Dictionary<int, Mesh>();
        InitPoints();
    }

    private void InitPoints()
    {
        timeStart = Time.time;
        pointsPerRow = (int)Mathf.Pow(2, resolution) + 1;
        numCells = (int)Mathf.Pow(8, resolution);

        gridPoints = new GridPoint[pointsPerRow * pointsPerRow * pointsPerRow];

        if(initGridPointBuffer != null)
        {
            initGridPointBuffer.Release();
        }

        initGridPointBuffer = new ComputeBuffer(gridPoints.Length, sizeof(float) * 4);
        initGridPointBuffer.SetData(gridPoints);


        planetChunkInitShader.SetBuffer(0, "gridPoints", initGridPointBuffer);
        planetChunkInitShader.SetInt("pointsPerRow", pointsPerRow);
        planetChunkInitShader.SetFloat("size", size);
        planetChunkInitShader.SetFloat("coreRadius", coreRadius);
        planetChunkInitShader.SetFloat("surfaceHeight", surfaceHeight);
        planetChunkInitShader.SetVector("chunkPosition", transform.position);
        planetChunkInitShader.SetVector("localPlanetCenter", transform.InverseTransformPoint(center));

        planetChunkInitShader.SetFloat("scale", noiseSettings.scale);
        planetChunkInitShader.SetFloat("multiplier", noiseSettings.multiplier);
        planetChunkInitShader.SetInt("numLayers", noiseSettings.numLayers);
        planetChunkInitShader.SetFloat("lacunarity", noiseSettings.lacunarity);
        planetChunkInitShader.SetFloat("persistence", noiseSettings.persistence);

        int numGroups = Mathf.CeilToInt(pointsPerRow / 3.0f);
        planetChunkInitShader.Dispatch(0, numGroups, numGroups, numGroups);

        AsyncGPUReadback.Request(initGridPointBuffer, ReceiveInitData);
    }

    private void ReceiveInitData(AsyncGPUReadbackRequest request)
    {
        //float initTime = Time.time - timeStart;
        //Debug.Log("Init: " + initTime);
        gridPoints = request.GetData<GridPoint>(0).ToArray();
        //initGridPointBuffer.GetData(gridPoints);
        initGridPointBuffer.Release();
        initialized = true;

        if(initCallback != null)
        {
            initCallback();
        }

        //float genStart = Time.time;

        GenerateMesh();

        //float genTime = Time.time - genStart;
        //Debug.Log("Generate Mesh: " + genTime);
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

        if(meshFilter == null)
        {
            return;
        }

        if(lodMeshes.ContainsKey(lod))
        {
            SetMesh(lodMeshes[lod]);
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

        /*
        Vector3[] vertices = new Vector3[0];
        int[] triangles = new int[0];
        CreateUniqueVertexArray(ref newTriangles, ref vertices, ref triangles);
        */
        
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
        SetMesh(mesh);

        lodMeshes.Add(lod, mesh);

        ReleaseBuffers();
    }

    void SetMesh(Mesh mesh)
    {
        meshFilter.mesh = mesh;

        meshCollider.sharedMesh = mesh;
        meshCollider.convex = false;
    }

    void CreateUniqueVertexArray(ref Triangle[] newTriangles, ref Vector3[] vertices, ref int[] triangles)
    {
        List<Vector3> sortedSet = new List<Vector3>();

        for(int i = 0; i < newTriangles.Length; i++)
        {
            AddUniqueSorted(ref newTriangles[i].A, ref sortedSet);
            AddUniqueSorted(ref newTriangles[i].B, ref sortedSet);
            AddUniqueSorted(ref newTriangles[i].C, ref sortedSet);
        }

        vertices = sortedSet.ToArray();
        triangles = new int[newTriangles.Length * 3];

        for(int i = 0; i < newTriangles.Length; i++)
        {
            Triangle curTri = newTriangles[i];
            triangles[i * 3] = binarySearch(ref curTri.A, ref vertices);
            triangles[i * 3 + 1] = binarySearch(ref curTri.B, ref vertices);
            triangles[i * 3 + 2] = binarySearch(ref curTri.C, ref vertices);
        }
    }

    void AddUniqueSorted(ref Vector3 newPoint, ref List<Vector3> points)
    {
        int l = 0;
        int r = points.Count - 1;
        int insertPoint = 0;

        while (r >= l)
        {
            int mid = l + (r - l) / 2;

            int compareVal = CompareVectors(points[mid], newPoint);

            if (compareVal == 0)
            {
                return;
            }
            else if (compareVal > 0)
            {
                r = mid - 1;
                insertPoint = mid;
            }
            else
            {
                l = mid + 1;
                insertPoint = l;
            }
        }

        points.Insert(insertPoint, newPoint);
    }

    int binarySearch(ref Vector3 searchPoint, ref Vector3[] points)
    {
        int l = 0;
        int r = points.Length - 1;
        while(r >= l)
        {
            int mid = l + (r - l) / 2;
            int compareVal = CompareVectors(points[mid], searchPoint);

            if (compareVal == 0)
            {
                return mid;
            }
            else if(compareVal > 0)
            {
                r = mid - 1;
            }
            else
            {
                l = mid + 1;
            }
        }

        return -1;
    }

    public int CompareVectors(Vector3 a, Vector3 b)
    {
        if (a == b)
        {
            return 0;
        }

        return a.x != b.x ? (a.x > b.x ? 1 : -1)
            : a.y != b.y ? (a.y > b.y ? 1 : -1)
            : a.z > b.z ? 1 : -1;
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

        Gizmos.DrawWireCube(transform.position, Vector3.one * size);

        for (int i = 0; i < gridPoints.Length; i++)
        {
            GridPoint point = gridPoints[i];
            Gizmos.color = new Color(point.val, point.val, point.val);
            Gizmos.DrawSphere(transform.TransformPoint(point.position), 0.1f);
        }
    }
}