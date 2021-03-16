using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetChunk))]
public class PlanetChunkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PlanetChunk planetChunk = (PlanetChunk)target;

        if(DrawDefaultInspector())
        {
            planetChunk.InitChunk();
        }

        if (GUILayout.Button("Init Points"))
        {
            planetChunk.InitChunk();
        }
    }
}
