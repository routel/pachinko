using UnityEngine;

public enum HoleType
{
    Start,
    Prize,
    Out
}

[CreateAssetMenu(menuName = "Data/HoleConfig", fileName = "HoleConfig_")]
public class HoleConfig : ScriptableObject
{
    public HoleType type = HoleType.Start;

    [Header("Count / Prize")]
    public int addInCount = 1;      // IN表示を増やす（不要なら0）
    public int prizeBalls = 0;      // 賞球（Startは0でOK）

    [Header("Suction")]
    public float suctionTime = 0.2f;
    public float endScale = 0.15f;
}
