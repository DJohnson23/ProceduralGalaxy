using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SimplexNoiseSettings
{
    [SerializeField]
    public int octaves = 6;
    [SerializeField]
    public int multiplier = 200;
    [SerializeField]
    public float amplitude = 100f;
    [SerializeField]
    public float lacunarity = 1.9f;
    [SerializeField]
    public float persistence = 0.7f;
    [SerializeField]
    public int[] seed = { 0x16, 0x38, 0x32, 0x2c, 0x0d, 0x13, 0x07, 0x2a };
}
