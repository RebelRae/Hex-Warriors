using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Save {
    public float saveTime = 0.0f;
    // Note : Vector3Int is not serializable
    public List<int> cellsX = new List<int>();
    public List<int> cellsY = new List<int>();
    public List<float> moveRegens = new List<float>();
}