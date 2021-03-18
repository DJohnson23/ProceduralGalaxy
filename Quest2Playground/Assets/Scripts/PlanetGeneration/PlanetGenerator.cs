using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetGenerator : MonoBehaviour
{
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
    public float maxDistanceCheck = 100;
    public int maxNonVisibleChunks = 10;

    PlanetChunkGrid largeViewChunk;

    [System.Serializable]
    public struct LodDistance
    {
        [SerializeField]
        public int lod;
        [SerializeField]
        public float distance;
    }

    [System.Serializable]
    public struct ViewDistance
    {
        [SerializeField]
        public float maxViewDistance;
        [SerializeField]
        public float distanceFromCenter;
    }

    public LodDistance[] lodDistances;
    public ViewDistance[] viewDistances;

    float chunkSize;

    Octree<PlanetChunk> planetChunkTree;
    List<PlanetChunk> lastVisibleChunks;
    float distanceToPlayer;

    List<PlanetChunk> initializingChunks;

    int currentInitializing = 0;
    object initLock = new object();

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
        largeViewChunk = new PlanetChunkGrid(size, largeViewDepth, this, largeViewResolution, transform.position);
        curViewMode = ViewMode.Large;
        planetChunkTree = new Octree<PlanetChunk>(depth, transform.position, size);
        chunkSize = size / Mathf.Pow(2, depth);
        lastVisibleChunks = new List<PlanetChunk>();
        initializingChunks = new List<PlanetChunk>();
    }

    public void Update()
    {
        CheckDistance();

        UpdateVisibleChunks();
        UpdateLargeView();
    }

    public void UpdateLargeView()
    {
        if(curViewMode != ViewMode.Large)
        {
            return;
        }

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
        distanceToPlayer = Vector3.Distance(transform.position, viewer.position);

        if(distanceToPlayer > coreRadius + surfaceHeight)
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
        if(distanceToPlayer > maxDistanceCheck)
        {
            return;
        }

        foreach(PlanetChunk visibleChunk in lastVisibleChunks)
        {
            //visibleChunk.gameObject.SetActive(false);
            visibleChunk.SetVisible(false);
        }

        lastVisibleChunks.Clear();

        /*
        for(int i = 0; i < initializingChunks.Count; i++)
        {
            if(initializingChunks[i].initialized)
            {
                initializingChunks.RemoveAt(i);
                i--;
            }
        }
        */

        Vector3 positionOffset = -Vector3.one * (chunkSize / 2);
        //Vector3 positionOffset = Vector3.zero;

        float maxViewDst = GetMaxViewDst();
        int chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        for (int zOffset = -chunksVisibleInViewDst; zOffset <= chunksVisibleInViewDst; zOffset++)
        {
            for(int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++)
            {
                for(int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++)
                {
                    Vector3 chunkOffset = new Vector3(chunkSize * xOffset, chunkSize * yOffset, chunkSize * zOffset);
                    Vector3 viewedChunkPos = viewer.position + chunkOffset;

                    //Debug.Log(distToCenter + " " + (coreRadius + surfaceHeight));
                    
                        
                    Octree<PlanetChunk> chunkTree = planetChunkTree.LookupTree(viewedChunkPos);


                    float distToViewer = Vector3.Distance(chunkTree.Position, viewer.position);
                    float distToCenter = Vector3.Distance(chunkTree.Position, viewedChunkPos);

                    if(distToCenter + chunkSize * 2 > coreRadius + surfaceHeight)
                    {
                        continue;
                    }

                    if(distToViewer > maxViewDst)
                    {
                        continue;
                    }

                    if(chunkTree.value != null)
                    {
                        if(curViewMode == ViewMode.Chunks)
                        {
                            //chunkTree.value.gameObject.SetActive(true);
                            chunkTree.value.LOD = GetLOD(distToViewer);
                            chunkTree.value.SetVisible(true);
                            lastVisibleChunks.Add(chunkTree.value);
                        }
                    }
                    else
                    {
                        if(curViewMode == ViewMode.Chunks)
                        {
                            //PlanetChunk newChunk = CreateNewChunk(chunkSize, chunkTree.Position + positionOffset, chunkResolution);
                            PlanetChunk newChunk = CreateNewChunk(chunkSize, chunkTree.Position, chunkResolution);
                            chunkTree.value = newChunk;

                            lastVisibleChunks.Add(newChunk);
                        }
                        else
                        {
                            lock(initLock)
                            {
                                if (currentInitializing < maxNonVisibleChunks)
                                {
                                    PlanetChunk newChunk = CreateNewChunk(chunkSize, chunkTree.Position, chunkResolution);
                                    chunkTree.value = newChunk;
                                    newChunk.SetVisible(false);
                                    newChunk.initCallback = InitializeCallback;
                                    //initializingChunks.Add(newChunk);
                                    currentInitializing++;
                                }
                            }
                        }
                    }
                }
            }
        }

        //largeViewChunk.gameObject.SetActive(false);
        if(curViewMode == ViewMode.Chunks)
        {
            largeViewChunk.SetVisible(false);
        }
    }

    void InitializeCallback()
    {
        lock(initLock)
        {
            currentInitializing--;
        }
    }

    float GetMaxViewDst()
    {
        float distance = Vector3.Distance(viewer.position, transform.position);
        float surfaceRadius = coreRadius + surfaceHeight / 2;

        return Mathf.Sqrt(distance * distance - surfaceRadius * surfaceRadius);
        /*
        for(int i = 0; i < viewDistances.Length; i++)
        {
            ViewDistance curDistance = viewDistances[i];

            if(distance < curDistance.distanceFromCenter)
            {
                if(i == 0)
                {
                    return curDistance.maxViewDistance;
                }

                float prevDst = viewDistances[i - 1].distanceFromCenter;
                float nextDst = viewDistances[i].distanceFromCenter;

                float t = (distance - prevDst) / (nextDst - prevDst);

                return Mathf.Lerp(viewDistances[i - 1].maxViewDistance, curDistance.maxViewDistance, t);
            }
        }

        return viewDistances[viewDistances.Length - 1].maxViewDistance;
        */
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
        Gizmos.DrawWireSphere(viewer.position, GetMaxViewDst());
    }

    public class PlanetChunkGrid
    {
        Octree<PlanetChunk> childChunks;
        List<PlanetChunk> allChunks;

        public PlanetChunkGrid(float size, int depth, PlanetGenerator parent, int chunkResolution, Vector3 position)
        {
            childChunks = new Octree<PlanetChunk>(depth, position, size);
            allChunks = new List<PlanetChunk>();

            int chunksPerRow = (int)Mathf.Pow(2, depth);
            float chunkSize = size / chunksPerRow;
            float halfChunk = chunkSize / 2;
            float halfSize = size / 2;

            for(int x = 0; x < chunksPerRow; x++)
            {
                for(int y = 0; y < chunksPerRow; y++)
                {
                    for(int z = 0; z < chunksPerRow; z++)
                    {
                        Vector3 pos = position + new Vector3(chunkSize * x + halfChunk - halfSize, chunkSize * y + halfChunk - halfSize, chunkSize * z + halfChunk - halfSize);

                        Octree<PlanetChunk> chunkTree = childChunks.LookupTree(pos);
                        chunkTree.value = CreateNewChunk(chunkSize, pos, chunkResolution, parent);
                        allChunks.Add(chunkTree.value);
                    }
                }
            }

        }

        private PlanetChunk CreateNewChunk(float size, Vector3 position, int resolution, PlanetGenerator parent)
        {
            GameObject newObject = new GameObject();
            newObject.transform.parent = parent.transform;
            newObject.transform.position = position;
            newObject.AddComponent<MeshFilter>();
            newObject.AddComponent<MeshRenderer>().material = parent.planetMaterial;
            newObject.AddComponent<MeshCollider>();

            PlanetChunk newPlanetChunk = newObject.AddComponent<PlanetChunk>();
            newPlanetChunk.size = size;
            newPlanetChunk.noiseSettings = parent.noiseSettings;
            newPlanetChunk.coreRadius = parent.coreRadius;
            newPlanetChunk.surfaceHeight = parent.surfaceHeight;
            newPlanetChunk.resolution = resolution;
            newPlanetChunk.marchingCubeShader = parent.marchingCubesShader;
            newPlanetChunk.planetChunkInitShader = parent.planetChunkInitShader;

            return newPlanetChunk;
        }

        public void SetVisible(bool visible)
        {
            foreach(PlanetChunk chunk in allChunks)
            {
                chunk.SetVisible(visible);
            }
        }

        public void SetLOD(int lod)
        {
            foreach(PlanetChunk chunk in allChunks)
            {
                chunk.LOD = lod;
            }
        }
    }
}
