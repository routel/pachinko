using UnityEngine;

[CreateAssetMenu(menuName = "Pachi/HitSpec", fileName = "HitSpec")]
public class HitSpec : ScriptableObject
{
    [Header("Rounds")]
    public int roundCount = 10;

    [Header("Payout")]
    public int ballsPerPrizeIn = 10; // 1入賞あたりの賞球

    [Header("In per Round (random range)")]
    public int minInPerRound = 8;
    public int maxInPerRound = 12;

    [Header("Timing")]
    public float interRoundWait = 0.6f;

    [Tooltip("0なら無効。詰まり保険でラウンドを強制終了する秒数")]
    public float maxRoundSeconds = 12f;
}
