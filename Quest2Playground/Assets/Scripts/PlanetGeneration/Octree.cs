using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Octree<T>
{
    public enum SubTreeIndex {
        BottomLeftFront  = 0, //000
        BottomLeftBack   = 1, //001
        BottomRightFront = 2, //010
        BottomRightBack  = 3, //011
        TopLeftFront     = 4, //100
        TopLeftBack      = 5, //101
        TopRightFront    = 6, //110
        TopRightBack     = 7  //111
    }

    public Octree<T>[] subTrees = new Octree<T>[8];
    public int depth = 0;
    public T value;

    Vector3 position;
    float size;

    public Vector3 Position
    {
        get
        {
            return position;
        }
    }

    public float Size
    {
        get
        {
            return size;
        }
    }

    public Octree(int depth, Vector3 position, float size)
    {
        this.depth = depth;
        this.position = position;
        this.size = size;
    }

    public Octree<T> LookupTree(Vector3 lookupPos)
    {
        if(depth == 0)
        {
            return this;
        }

        int index = 0;
        index |= lookupPos.z > position.z ? 1 : 0;
        index |= lookupPos.x > position.x ? 2 : 0;
        index |= lookupPos.y > position.y ? 4 : 0;


        if(subTrees[index] == null)
        {
            CreateSubTree(index);
        }

        return subTrees[index].LookupTree(lookupPos);
    }

    private void CreateSubTree(int index)
    {
        float rad2 = size / 4;

        float z = position.z + ((index & 1) == 1 ? rad2 : -rad2);
        float x = position.x + ((index & 2) == 2 ? rad2 : -rad2);
        float y = position.y + ((index & 4) == 4 ? rad2 : -rad2);

        subTrees[index] = new Octree<T>(depth - 1, new Vector3(x, y, z), size / 2);
    }
}
