using DG.Tweening;
using System;
using UnityEngine;

public class LotteryManager : MonoBehaviour
{
    public static LotteryManager Instance { get; private set; }

    [Header("Lottery")]
    [Range(0f, 1f)]
    [SerializeField] private float winRate = 0.05f;

    [Header("Hit (Prize Gate Open Time)")]
    [SerializeField] private float hitDuration = 8.0f;

    [Header("UI")]
    [SerializeField] private SlotMachineTweenUI slotUI;
    [SerializeField] private ReachDirector reachDirector;

    [SerializeField] private float loseHoldSeconds = 2.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PlayFromStartHole()
    {
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
                // リーチ時だけPUSH待ち（この間、中央は回り続ける）
                if (isReach && reachDirector != null)
                {
                    reachDirector.PlayReachUntilPush(isWin, () =>
                    {
                        resume(); // PUSH後に中央停止へ
                    });
                }
                else
                {
                    // リーチじゃないなら即続行
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
                        DOVirtual.DelayedCall(loseHoldSeconds, () => slotUI.Hide());
                    }
                    else
                    {
                        slotUI.Hide();
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
