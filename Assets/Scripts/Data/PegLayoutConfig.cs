using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/PegLayoutConfig", fileName = "PegLayoutConfig_")]
public class PegLayoutConfig : ScriptableObject
{
    public int seed = 12345;
    public Vector2 areaMin = new Vector2(-3.5f, -1.5f);
    public Vector2 areaMax = new Vector2(3.5f, 4.5f);
    public float spacingX = 0.6f;
    public float spacingY = 0.6f;
    public float jitter = 0.15f;

    public bool useBakedPositions = false;
    public List<Vector2> bakedPositions = new List<Vector2>();
}
