using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 当たり中の進行管理
/// </summary>
public class HitDirector : MonoBehaviour
{
    public static HitDirector Instance { get; private set; }

    [Header("Gate")]
    [SerializeField] private PrizeGateController prizeGate;

    [Header("Spec (optional)")]
    [SerializeField] private HitSpec spec;

    private int currentRound;
    private bool inHit;

    private Sequence seq;

    public bool IsInHit => inHit;
    public int CurrentRound => currentRound;

    // 現在ラウンドの入賞数
    private int currentRoundInCount = 0;

    // このラウンドの目標入賞数
    private int targetInThisRound = 0;

    // ラウンド終了処理の多重実行防止
    private bool isEndingRound = false;

    // ★追加：タイムアウト終了フラグ
    private bool endedByTimeout = false;

    // ★追加：当たり1回の総払い出し
    private int totalPayoutThisHit = 0;

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (spec == null)
        {
            Debug.LogError("[HitDirector] spec is not set. Create HitSpec and assign it in Inspector.");
            enabled = false;
            return;
        }

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
        totalPayoutThisHit = 0;

        StartNextRound();
    }

    private void StartNextRound()
    {
        currentRound++;

        float iw = Mathf.Max(0.1f, spec.interRoundWait);

        currentRoundInCount = 0;
        isEndingRound = false;
        endedByTimeout = false;

        int min = Mathf.Max(0, spec.minInPerRound);
        int max = Mathf.Max(min, spec.maxInPerRound);
        targetInThisRound = UnityEngine.Random.Range(min, max + 1);

        Debug.Log($"[HitDirector] Round {currentRound}/{spec.roundCount} START targetIn={targetInThisRound} iw={iw} frame={Time.frameCount}");

        if (seq != null && seq.IsActive())
            seq.Kill();

        seq = DOTween.Sequence().SetUpdate(true);

        OnRoundStart?.Invoke(currentRound);
        if (prizeGate != null) prizeGate.StartCycle();

        // 保険タイマー
        if (spec.maxRoundSeconds > 0f)
        {
            float safe = Mathf.Max(0.2f, spec.maxRoundSeconds);
            seq.AppendInterval(safe);
            seq.AppendCallback(() =>
            {
                endedByTimeout = true;
                Debug.Log($"[HitDirector] Round {currentRound} SAFE END (timeout) frame={Time.frameCount}");
                EndCurrentRound(iw);
            });
        }
    }

    private void EndCurrentRound(float iw)
    {
        // すでに次へ進んでたら二重終了しない
        if (!inHit) return;

        if (isEndingRound) return;
        isEndingRound = true;

        // 保険タイマーが動いている可能性があるので止める
        if (seq != null && seq.IsActive())
            seq.Kill();

        Debug.Log($"[HitDirector] Round {currentRound} END in={currentRoundInCount}/{targetInThisRound} timeout={endedByTimeout} frame={Time.frameCount}");


        OnRoundEnd?.Invoke(currentRound);
        if (prizeGate != null) prizeGate.StopCycle();

        if (currentRound < spec.roundCount)
        {
            seq = DOTween.Sequence().SetUpdate(true);
            seq.AppendInterval(iw);
            seq.AppendCallback(StartNextRound);
        }
        else
        {
            EndHit();
        }
    }

    public void OnPrizeIn()
    {
        if (!inHit) return;
        if (isEndingRound) return;

        currentRoundInCount++;

        int add = spec.ballsPerPrizeIn;  // ★賞球はスペックで固定
        if (GameManager.Instance != null && add != 0)
            GameManager.Instance.AddBalls(add);

        totalPayoutThisHit += add;

        Debug.Log($"[HitDirector] Prize IN  {currentRoundInCount}/{targetInThisRound}  balls={add}  frame={Time.frameCount}");

        if (currentRoundInCount >= targetInThisRound)
        {
            if (isEndingRound) return;

            float iw = Mathf.Max(0.1f, spec.interRoundWait);
            EndCurrentRound(iw);
        }
    }
    private void EndHit()
    {
        Debug.Log($"[HitDirector] HIT PAYOUT total={totalPayoutThisHit} balls");


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
