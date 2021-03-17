using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NoiseSettings
{
    [SerializeField]
    public float scale;
    [SerializeField]
    public int numLayers = 6;
    [SerializeField]
    public float multiplier = 200;
    [SerializeField]
    public float lacunarity = 1.9f;
    [SerializeField]
    public float persistence = 0.7f;
}
