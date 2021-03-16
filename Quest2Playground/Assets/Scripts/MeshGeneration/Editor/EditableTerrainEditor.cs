using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EditableTerrain))]
public class EditableTerrainEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditableTerrain terrain = (EditableTerrain)target;

        if(DrawDefaultInspector())
        {
            terrain.RefreshTerrain();
        }

        if(GUILayout.Button("Refresh Terrain"))
        {
            terrain.RefreshTerrain();
        }
    }
}
