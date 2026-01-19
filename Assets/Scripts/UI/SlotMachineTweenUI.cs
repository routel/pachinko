using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SlotMachineTweenUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject slotPanel;

    [Header("Reels")]
    [SerializeField] private ReelControllerTween reelL;
    [SerializeField] private ReelControllerTween reelC;
    [SerializeField] private ReelControllerTween reelR;

    [Header("Timing")]
    [SerializeField] private float preSpinTime = 0.6f;
    [SerializeField] private float stopGap = 0.25f;

    [Header("Stop Steps")]
    [SerializeField] private int extraSteps = 20;

    private enum State
    {
        Idle,
        SpinningLR,     // 左右停止まで進行
        WaitingPush,    // PUSH待ち（中央は回り続ける）
        StoppingCenter, // 中央停止中
    }

    private State state = State.Idle;
    public bool IsBusy => state != State.Idle;

    private Sequence seq;

    // “保留（最小版）”
    private struct SpinRequest
    {
        public int tL, tC, tR; // 1..9
        public Action<Action> onBeforeStopCenter;
        public Action onFinished;
    }
    private readonly Queue<SpinRequest> queue = new Queue<SpinRequest>();

    // 中央停止の多重発火防止
    private bool centerStopRequested;

    private void Awake()
    {
        if (slotPanel != null) slotPanel.SetActive(false);
    }

    public void StartSpinWithTargets(int tL, int tC, int tR, Action onFinished)
    {
        StartSpinWithTargets(tL, tC, tR, null, onFinished);
    }

    public void StartSpinWithTargets(int tL, int tC, int tR, Action<Action> onBeforeStopCenter, Action onFinished)
    {
        // 忙しい間は保留へ（待機中に左右が回りだすのを防ぐ）
        if (IsBusy)
        {
            queue.Enqueue(new SpinRequest
            {
                tL = tL,
                tC = tC,
                tR = tR,
                onBeforeStopCenter = onBeforeStopCenter,
                onFinished = onFinished
            });
            Debug.Log($"[SLOT] queued. count={queue.Count} state={state}");
            return;
        }

        StartSpinInternal(tL, tC, tR, onBeforeStopCenter, onFinished);
    }

    private void StartSpinInternal(int tL, int tC, int tR, Action<Action> onBeforeStopCenter, Action onFinished)
    {
        if (reelL == null || reelC == null || reelR == null)
        {
            Debug.LogError("SlotMachineTweenUI: Reel references are not set.");
            onFinished?.Invoke();
            return;
        }

        // 1..9 に正規化
        tL = Mathf.Clamp(tL, 1, 9);
        tC = Mathf.Clamp(tC, 1, 9);
        tR = Mathf.Clamp(tR, 1, 9);

        int iL = tL - 1;
        int iC = tC - 1;
        int iR = tR - 1;

        Debug.Log($"[SLOT START] TARGET L={tL} C={tC} R={tR}");

        if (slotPanel != null) slotPanel.SetActive(true);

        KillSeqOnly();

        // ★同フレームで3つ同時スタート
        reelL.StartLoop();
        reelC.StartLoop();
        reelR.StartLoop();

        centerStopRequested = false;
        state = State.SpinningLR;

        // 左右だけ Sequence で止める
        seq = DOTween.Sequence().SetUpdate(true);

        seq.AppendInterval(preSpinTime);

        // 左停止
        seq.Append(reelL.StopAt(iL, extraSteps));
        seq.AppendInterval(stopGap);

        // 右停止
        seq.Append(reelR.StopAt(iR, extraSteps));
        seq.AppendInterval(stopGap);

        // 右停止後：リーチならPUSH待ち、無ければ即中央停止へ
        seq.AppendCallback(() =>
        {
            // 念のため中央が回っていることを保証
            reelC.StartLoop();

            if (onBeforeStopCenter != null)
            {
                state = State.WaitingPush;
                Debug.Log("[SLOT] WAIT PUSH (center keeps spinning)");

                // 外部に resume を渡す（PUSHで resume() が呼ばれたら中央停止）
                onBeforeStopCenter.Invoke(() =>
                {
                    StopCenter(iC, tL, tC, tR, onFinished);
                });
            }
            else
            {
                // リーチ無し＝即中央停止
                StopCenter(iC, tL, tC, tR, onFinished);
            }
        });

        // Sequence はここで役目終了（中央停止は別系統）
        seq.OnComplete(() =>
        {
            // ここでは何もしない：中央停止が終わったら onFinished などを呼ぶ
        });
    }

    private void StopCenter(int targetIndexC, int tL, int tC, int tR, Action onFinished)
    {
        if (centerStopRequested) return; // 多重防止
        centerStopRequested = true;

        state = State.StoppingCenter;

        // ★ここで初めて StopAt を呼ぶ（＝ここまで中央は止まらない）
        reelC.StopAt(targetIndexC, extraSteps, () =>
        {
            Debug.Log($"[SLOT FINISH] RESULT L={tL} C={tC} R={tR}");

            state = State.Idle;

            onFinished?.Invoke();

            TryDequeueAndStartNext();
        });
    }

    private void TryDequeueAndStartNext()
    {
        if (IsBusy) return;
        if (queue.Count <= 0) return;

        var req = queue.Dequeue();
        Debug.Log($"[SLOT] dequeue next. remain={queue.Count}");

        StartSpinInternal(req.tL, req.tC, req.tR, req.onBeforeStopCenter, req.onFinished);
    }

    private void KillSeqOnly()
    {
        if (seq != null)
        {
            if (seq.IsActive()) seq.Kill();
            seq = null;
        }
    }

    public void Hide()
    {
        KillSeqOnly();
        state = State.Idle;
        queue.Clear();
        centerStopRequested = false;

        if (slotPanel != null) slotPanel.SetActive(false);
    }
}
