using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class EditableTerrain : MonoBehaviour
{
    MeshFilter meshFilter;

    [SerializeField]
    float terrainWidth = 100;
    [SerializeField]
    float terrainLength = 100;

    [SerializeField]
    int widthResolution = 100;
    [SerializeField]
    int lengthResolution = 100;

    [SerializeField]
    float edgeMin = 0.5f;
    [SerializeField]
    float edgeMax = 2f;

    EditableTerrainMesh terrainMesh;

    private void Awake()
    {
        RefreshTerrain();
    }

    public void RefreshTerrain()
    {
        if(meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }

        terrainMesh = new EditableTerrainMesh(terrainWidth, terrainLength, widthResolution, lengthResolution, edgeMin, edgeMax);
        meshFilter.mesh = terrainMesh.GetMesh();
    }

    /*
    public void Dig(Vector3 position, Vector3 direction, float radius, float digStrength)
    {
        direction.Normalize();

        for(int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            float distance = Vector3.Distance(position, v);
            float dot = Vector3.Dot(direction, normals[i]);

            if(distance < radius && dot < Mathf.Epsilon)
            {
                float amt = distance / radius;
                amt = 1 - (amt * amt);

                vertices[i] = v + direction * amt * digStrength;
            }
        }

        GenerateNormals();
        GenerateUV();
        UpdateMesh();
    }
    */

}
