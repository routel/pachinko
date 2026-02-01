using DG.Tweening;
using System;
using UnityEngine;

public class LotteryManager : MonoBehaviour
{
    public static LotteryManager Instance { get; private set; }

    [Header("Hold (Start Queue)")]
    [SerializeField] private int maxHold = 4; // 可変。0なら保留なし
    public int MaxHold => maxHold;

    private int holdCount = 0;
    public int HoldCount => holdCount;

    [Header("Lottery")]
    [Range(0f, 1f)]
    [SerializeField] private float winRate = 0.05f;

    [Header("Hit (Prize Gate Open Time)")]
    [SerializeField] private float hitDuration = 8.0f;

    [Header("UI")]
    [SerializeField] private SlotMachineTweenUI slotUI;
    [SerializeField] private ReachDirector reachDirector;

    [SerializeField] private float loseHoldSeconds = 2.0f;

    // Busyフラグ（外部から切り替える）
    public bool IsBusy { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public bool TryRequestStart()
    {
        // 当たり中は絶対に回さない（保留は貯める）
        if (GameManager.Instance != null && GameManager.Instance.IsHit)
        {
            return TryEnqueueHold();
        }

        // リーチ/スロットなどでBusy中なら保留へ
        if (IsBusy)
        {
            return TryEnqueueHold();
        }

        // すぐ開始できる
        PlayFromStartHole();
        return true;
    }

    private bool TryEnqueueHold()
    {
        if (maxHold <= 0) return false; // 保留機能なし＝無効
        if (holdCount >= maxHold) return false; // 上限超え＝無効

        holdCount++;
        // HUDにも出したいならイベント化してもOK（今はログでも可）
        Debug.Log($"[Lottery] HOLD +1 => {holdCount}/{maxHold}");
        return true;
    }

    public void SetBusy(bool busy)
    {
        IsBusy = busy;

        // Busy解除されたタイミングで保留があれば消化
        if (!IsBusy)
        {
            ConsumeHoldIfAny();
        }
    }

    private void ConsumeHoldIfAny()
    {
        if (holdCount <= 0) return;

        // 当たり中はまだ消化しない（当たり終了後にまとめて）
        if (GameManager.Instance != null && GameManager.Instance.IsHit) return;

        // 1件だけ消化（実機っぽく）
        holdCount--;
        Debug.Log($"[Lottery] HOLD CONSUME => {holdCount}/{maxHold}");

        PlayFromStartHole();
    }



    private void PlayFromStartHole()
    {
        SetBusy(true);
        bool isWin = (UnityEngine.Random.value < winRate);

        int L = UnityEngine.Random.Range(1, 10);
        int C = UnityEngine.Random.Range(1, 10);
        int R = UnityEngine.Random.Range(1, 10);

        if (isWin)
        {
            int v = UnityEngine.Random.Range(1, 10);
            L = C = R = v;
        }

        bool isReach = (L == R); // 今回は「左右一致」をリーチ条件にする

        Debug.Log($"[LOTTERY] isWin={isWin} reach={isReach} TARGET(L,C,R)=({L},{C},{R})");

        if (slotUI == null)
        {
            Debug.LogError("LotteryManager: slotUI is not set.");
            return;
        }


        // ★ 中央停止直前で止める
        slotUI.StartSpinWithTargets(
            L, C, R,
            onBeforeStopCenter: (resume) =>
            {
                if (isReach && reachDirector != null)
                {
                    // ★ここで動画演出を決定して渡す
                    string videoKey = isWin ? "reach.strong" : "reach.normal";

                    reachDirector.PlayReachUntilPush(
                        isWin,
                        onPushed: () =>
                        {
                            resume(); // PUSH後に中央停止へ
                        },
                        videoKey: videoKey
                    );
                }
                else
                {
                    resume();
                }
            },
            onFinished: () =>
            {
                void FinishAndMaybeHide()
                {
                    if (isWin && GameManager.Instance != null)
                        GameManager.Instance.BeginHit(hitDuration);

                    if (!isWin && loseHoldSeconds > 0f)
                    {
                        DOVirtual.DelayedCall(loseHoldSeconds, () => 
                        {
                            slotUI.Hide();
                            SetBusy(false);
                        });
                    }
                    else
                    {
                        slotUI.Hide();
                        SetBusy(false);
                    }
                }

                if (isReach && reachDirector != null)
                    reachDirector.PlayResultOnly(isWin, FinishAndMaybeHide);
                else
                    FinishAndMaybeHide();
            }
        );

    }
}
