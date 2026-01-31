using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 当たり中の進行管理
/// </summary>
public class HitDirector : MonoBehaviour
{
    public static HitDirector Instance { get; private set; }

    [Header("Hit Plan")]
    [SerializeField] private int roundCount = 10;        // 10R
    [SerializeField] private float roundDuration = 5.0f; // 1Rの長さ
    [SerializeField] private float interRoundWait = 0.6f;

    [Header("Gate")]
    [SerializeField] private PrizeGateController prizeGate;

    private int currentRound;
    private bool inHit;

    private Sequence seq;

    public bool IsInHit => inHit;
    public int CurrentRound => currentRound;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 大当たり開始（LotteryManager から呼ぶ）
    /// </summary>
    public void BeginHit()
    {
        if (inHit)
        {
            Debug.LogWarning("[HitDirector] Already in hit.");
            return;
        }

        Debug.Log("[HitDirector] HIT START");

        inHit = true;
        currentRound = 0;

        StartNextRound();
    }

    private void StartNextRound()
    {
        currentRound++;

        float rd = Mathf.Max(0.2f, roundDuration);
        float iw = Mathf.Max(0.1f, interRoundWait);

        Debug.Log($"[HitDirector] Round {currentRound}/{roundCount} START rd={roundDuration}->{rd} iw={interRoundWait}->{iw} frame={Time.frameCount}");

        if (seq != null && seq.IsActive())
            seq.Kill();

        seq = DOTween.Sequence().SetUpdate(true);

        OnRoundStart?.Invoke(currentRound);
        if (prizeGate != null) prizeGate.StartCycle();

        seq.AppendInterval(rd);

        seq.AppendCallback(() =>
        {
            Debug.Log($"[HitDirector] Round {currentRound} END frame={Time.frameCount}");
            OnRoundEnd?.Invoke(currentRound);
            if (prizeGate != null) prizeGate.StopCycle();

            if (currentRound < roundCount)
            {
                seq = DOTween.Sequence().SetUpdate(true);
                seq.AppendInterval(iw);
                seq.AppendCallback(StartNextRound);
            }
            else
            {
                EndHit();
            }
        });
    }

    private void EndHit()
    {
        Debug.Log("[HitDirector] HIT END");

        inHit = false;
        currentRound = 0;

        OnHitEnd?.Invoke();

        // ★ GameManager に終了を通知
        if (GameManager.Instance != null)
            GameManager.Instance.EndHitFromDirector();
    }

    // ===== イベント（後で PrizeGate / 動画が購読） =====

    public event Action<int> OnRoundStart;
    public event Action<int> OnRoundEnd;
    public event Action OnHitEnd;
}
