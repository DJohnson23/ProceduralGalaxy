using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(AudioSource))]
public class ForceLightning : MonoBehaviour
{
    enum ForceLightningState {
        Active,
        Inactive
    }

    ForceLightningState currentState = ForceLightningState.Active;


    [SerializeField]
    int numBolts = 8;
    public float boltSpeed = 5f;
    public float boltRange = 0.6f;
    public float endSpread = 0.5f;
    public float startSpread = 0f;
    public bool useWorldSpace = true;
    public Vector3 endPos = new Vector3(0, 0, 5);

    [SerializeField]
    Material boltMaterial;
    [SerializeField]
    int boltPositionCount = 8;
    [SerializeField]
    float boltStartWidth = 0.5f;
    [SerializeField]
    float boltEndWidth = 0.2f;

    public int BoltPositionCount
    {
        get
        {
            return boltPositionCount;
        }

        set
        {
            boltPositionCount = value;
            needsRefresh = true;
        }
    }

    public float BoltEndWidth
    {
        get
        {
            return boltEndWidth;
        }

        set
        {
            boltEndWidth = value;
            needsRefresh = true;
        }
    }

    public float BoltStartWidth
    {
        get
        {
            return boltStartWidth;
        }

        set
        {
            boltStartWidth = value;
            needsRefresh = true;
        }
    }

    public int NumBolts
    {
        get
        {
            return numBolts;
        }

        set
        {
            numBolts = value;
            needsRefresh = true;
        }
    }

    public Material BoltMaterial
    {
        get
        {
            return boltMaterial;
        }

        set
        {
            boltMaterial = value;
            needsRefresh = true;
        }
    }

    List<LineRenderer> bolts;
    bool needsRefresh;
    float nextRandomize;
    int nextRanIndex;

    AudioSource audioSource;

    // Update is called once per frame
    void Update()
    {
        if(needsRefresh)
        {
            RefreshBolts();
        }

        if(Time.time >= nextRandomize)
        {
            RandomizeNext();
        }
    }

    private void OnEnable()
    {
        if(currentState == ForceLightningState.Active)
        {
            RefreshBolts();
        }

        audioSource = GetComponent<AudioSource>();
        audioSource.Play();
    }

    private void OnDisable()
    {
        bolts = new List<LineRenderer>();
        LineRenderer[] boltsInChildren = GetComponentsInChildren<LineRenderer>();

        foreach (LineRenderer bolt in boltsInChildren)
        {
            DestroyImmediate(bolt.gameObject);
        }

        audioSource.Stop();
    }

    public void RefreshBolts()
    {
        if(!enabled)
        {
            return;
        }

        LineRenderer[] boltsInChildren = GetComponentsInChildren<LineRenderer>();

        if(bolts == null || bolts.Count != boltsInChildren.Length || bolts.Count != numBolts)
        {
            bolts = new List<LineRenderer>();

            foreach (LineRenderer bolt in boltsInChildren)
            {
                DestroyImmediate(bolt.gameObject);
            }
        }
        

        if (bolts.Count > numBolts)
        {
            do
            {
                DestroyImmediate(bolts[bolts.Count - 1].gameObject);
                bolts.RemoveAt(bolts.Count - 1);
            } 
            while (bolts.Count > numBolts);
        }
        else if (bolts.Count < numBolts)
        {
            do
            {
                CreateBolt(bolts.Count);
            }
            while (bolts.Count < numBolts);
        }

        for(int i = 0; i < bolts.Count; i++)
        {
            LineRenderer bolt = bolts[i];

            if(bolt == null)
            {
                CreateBolt(i);
            }

            bolt.positionCount = boltPositionCount;
            bolt.material = boltMaterial;
            bolt.startWidth = boltStartWidth;
            bolt.endWidth = 0f;

        }

        needsRefresh = false;
    }

    void CreateBolt(int index)
    {
        GameObject obj = new GameObject();
        obj.transform.parent = transform;
        obj.transform.position = transform.position;
        obj.name = string.Format("Bolt_{0}", index);
        LineRenderer bolt = obj.AddComponent<LineRenderer>();
        bolts.Insert(index, bolt);
        bolt.positionCount = boltPositionCount;
        bolt.useWorldSpace = true;
        bolt.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        RandomizeBolt(bolt);
    }

    void RandomizeBolt(LineRenderer bolt)
    {
        Vector3 end = RandomVec3(endSpread);
        end += useWorldSpace ? endPos : transform.TransformPoint(endPos);

        Vector3 start = transform.TransformPoint(RandomVec3(startSpread));

        for (int i = 1; i < bolt.positionCount - 1; i++)
        {
            float x = Random.Range(-boltRange, boltRange);
            float y = Random.Range(-boltRange, boltRange);
            float z = Random.Range(-boltRange, boltRange);

            Vector3 delta = new Vector3(x, y, z);
            Vector3 basePos = Vector3.Lerp(start, end, i / (float)(bolt.positionCount - 1));

            Vector3 pos = basePos + delta;

            bolt.SetPosition(i, pos);
        }

        bolt.SetPosition(0, start);
        bolt.SetPosition(bolt.positionCount - 1, end);
    }

    public void Randomize()
    {
        for(int i = 0; i < bolts.Count; i++)
        {
            RandomizeBolt(bolts[i]);
        }
    }

    public void RandomizeNext()
    {
        if(bolts.Count == 0)
        {
            return;
        }

        if(nextRanIndex > bolts.Count)
        {
            nextRanIndex = 0;
        }

        RandomizeBolt(bolts[nextRanIndex]);
        nextRanIndex = (nextRanIndex + 1) % bolts.Count;

        nextRandomize = 1 / (boltSpeed * numBolts);
    }

    Vector3 RandomVec3(float range)
    {
        float x = Random.Range(-range, range);
        float y = Random.Range(-range, range);
        float z = Random.Range(-range, range);

        return new Vector3(x, y, z);
    }
}
