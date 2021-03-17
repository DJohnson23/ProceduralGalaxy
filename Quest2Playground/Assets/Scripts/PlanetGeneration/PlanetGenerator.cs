using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetGenerator : MonoBehaviour
{
    public float maxViewDst = 20f;
    public int depth = 5;
    public float size = 100;
    public Transform viewer;
    public NoiseSettings noiseSettings;
    public float coreRadius = 70f;
    public float surfaceHeight = 15f;
    public int chunkResolution = 5;
    public ComputeShader marchingCubesShader;
    public ComputeShader planetChunkInitShader;

    public Material planetMaterial;
    public float mass = 10f;
    public int largeViewResolution = 5;
    public int largeViewDepth = 1;

    PlanetChunk largeViewChunk;

    [System.Serializable]
    public struct LodDistance
    {
        [SerializeField]
        public int lod;
        [SerializeField]
        public float distance;
    }

    public LodDistance[] lodDistances;

    float chunkSize;
    int chunksVisibleInViewDst;

    Octree<PlanetChunk> planetChunkTree;

    List<PlanetChunk> lastVisibleChunks;

    const float sqrt2 = 1.4142f;

    public enum ViewMode
    {
        Large,
        Chunks
    }

    ViewMode curViewMode;

    private void Start()
    {
        InitGrid();
    }

    public void InitGrid()
    {
        //largeViewChunk = CreateNewChunk(size, transform.position - new Vector3(size / 2, size / 2, size / 2), largeViewResolution);
        largeViewChunk = CreateNewChunk(size, transform.position, largeViewResolution);
        curViewMode = ViewMode.Large;
        planetChunkTree = new Octree<PlanetChunk>(depth, transform.position, size);
        chunkSize = size / Mathf.Pow(2, depth);
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
        lastVisibleChunks = new List<PlanetChunk>();
    }

    public void Update()
    {
        CheckDistance();

        switch(curViewMode)
        {
            case ViewMode.Chunks:
                UpdateVisibleChunks();
                break;
            case ViewMode.Large:
                UpdateLargeView();
                break;
        }
    }

    public void UpdateLargeView()
    {
        if(lastVisibleChunks.Count > 0)
        {
            foreach(PlanetChunk visibleChunk in lastVisibleChunks)
            {
                //visibleChunk.gameObject.SetActive(false);
                visibleChunk.SetVisible(false);
            }
        }

        //largeViewChunk.gameObject.SetActive(true);
        largeViewChunk.SetVisible(true);
    }


    void CheckDistance()
    {
        float distance = Vector3.Distance(transform.position, viewer.position);

        if(distance > coreRadius + surfaceHeight)
        {
            curViewMode = ViewMode.Large;
        }
        else
        {
            curViewMode = ViewMode.Chunks;
        }
    }

    void UpdateVisibleChunks()
    {
        //largeViewChunk.gameObject.SetActive(false);
        largeViewChunk.SetVisible(false);

        foreach(PlanetChunk visibleChunk in lastVisibleChunks)
        {
            //visibleChunk.gameObject.SetActive(false);
            visibleChunk.SetVisible(false);
        }

        lastVisibleChunks.Clear();

        Vector3 positionOffset = -Vector3.one * (chunkSize / 2);
        //Vector3 positionOffset = Vector3.zero;

        for (int zOffset = -chunksVisibleInViewDst; zOffset <= chunksVisibleInViewDst; zOffset++)
        {
            for(int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
            {
                for(int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
                {
                    Vector3 chunkOffset = new Vector3(chunkSize * xOffset, chunkSize * yOffset, chunkSize * zOffset);
                    Vector3 viewedChunkPos = viewer.position + chunkOffset;

                    /*
                    float distToCenter = Vector3.Distance(transform.position, viewedChunkPos);

                    Debug.Log(distToCenter + " " + (coreRadius + surfaceHeight));

                    
                    if(distToCenter + chunkSize * sqrt2 > coreRadius + surfaceHeight)
                    {
                        continue;
                    }
                    */
                        
                    Octree<PlanetChunk> chunkTree = planetChunkTree.LookupTree(viewedChunkPos);

                    float distance = Vector3.Distance(chunkTree.Position, viewer.position);

                    if(distance > maxViewDst)
                    {
                        continue;
                    }

                    if(chunkTree.value != null)
                    {
                        //chunkTree.value.gameObject.SetActive(true);
                        chunkTree.value.SetVisible(true);
                    }
                    else
                    {
                        //PlanetChunk newChunk = CreateNewChunk(chunkSize, chunkTree.Position + positionOffset, chunkResolution);
                        PlanetChunk newChunk = CreateNewChunk(chunkSize, chunkTree.Position, chunkResolution);

                        chunkTree.value = newChunk;
                    }


                    chunkTree.value.LOD = GetLOD(distance);
                    lastVisibleChunks.Add(chunkTree.value);
                }
            }
        }
    }

    private PlanetChunk CreateNewChunk(float size, Vector3 position, int resolution)
    {
        GameObject newObject = new GameObject();
        newObject.transform.parent = transform;
        newObject.transform.position = position;
        newObject.AddComponent<MeshFilter>();
        newObject.AddComponent<MeshRenderer>().material = planetMaterial;
        newObject.AddComponent<MeshCollider>();

        PlanetChunk newPlanetChunk = newObject.AddComponent<PlanetChunk>();
        newPlanetChunk.size = size;
        newPlanetChunk.noiseSettings = noiseSettings;
        newPlanetChunk.coreRadius = coreRadius;
        newPlanetChunk.surfaceHeight = surfaceHeight;
        newPlanetChunk.resolution = resolution;
        newPlanetChunk.marchingCubeShader = marchingCubesShader;
        newPlanetChunk.planetChunkInitShader = planetChunkInitShader;

        return newPlanetChunk;
    }

    private int GetLOD(float distance)
    {
        for(int i = 0; i < lodDistances.Length; i++)
        {
            if(lodDistances[i].distance > distance)
            {
                return lodDistances[i].lod;
            }
        }

        return lodDistances[lodDistances.Length - 1].lod;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, Vector3.one * size);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, coreRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, coreRadius + surfaceHeight);

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(viewer.position, maxViewDst);
    }
}
