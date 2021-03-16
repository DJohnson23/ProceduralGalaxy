using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ForceLightning))]
public class ForceLightningEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ForceLightning forceLightning = (ForceLightning)target;

        if(DrawDefaultInspector())
        {
            forceLightning.RefreshBolts();
        }
    }
}
