using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        SpinningLR,
        WaitingPush,
        StoppingCenter,
    }

    private State state = State.Idle;
    public bool IsBusy => state != State.Idle;

    private struct SpinRequest
    {
        public int tL, tC, tR;               // 1..9
        public Action<Action> onBeforeStopCenter; // リーチ時：PUSH待ちを表示し、resumeを受け取る
        public Action onFinished;
    }

    private readonly Queue<SpinRequest> queue = new Queue<SpinRequest>();

    private Coroutine spinRoutine;
    private int token = 0;
    private bool centerStopRequested;

    private void Awake()
    {
        if (slotPanel != null) slotPanel.SetActive(false);
    }

    public void StartSpinWithTargets(int tL, int tC, int tR, Action onFinished)
        => StartSpinWithTargets(tL, tC, tR, null, onFinished);

    public void StartSpinWithTargets(int tL, int tC, int tR, Action<Action> onBeforeStopCenter, Action onFinished)
    {
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

        // 既存ルーチン停止
        token++;
        if (spinRoutine != null)
        {
            StopCoroutine(spinRoutine);
            spinRoutine = null;
        }

        centerStopRequested = false;
        state = State.SpinningLR;

        int myToken = token;
        spinRoutine = StartCoroutine(SpinFlowRoutine(
            myToken, iL, iC, iR, tL, tC, tR, onBeforeStopCenter, onFinished));
    }

    private IEnumerator SpinFlowRoutine(
        int myToken,
        int iL, int iC, int iR,
        int tL, int tC, int tR,
        Action<Action> onBeforeStopCenter,
        Action onFinished)
    {
        // UIが落ち着くまで待つ（SetActive直後のズレ対策）
        yield return new WaitForEndOfFrame();
        if (myToken != token) yield break;

        // ★ここで3つ同時に回転開始（同時性はここで保証）
        reelL.StartLoop(true);
        reelC.StartLoop(true);
        reelR.StartLoop(true);

        // 回転している時間
        yield return WaitSecondsRealtime(preSpinTime);
        if (myToken != token) yield break;

        // ★左停止（呼ぶのはこの瞬間だけ）
        yield return StopReelAndWait(reelL, iL);
        if (myToken != token) yield break;

        yield return WaitSecondsRealtime(stopGap);
        if (myToken != token) yield break;

        // ★右停止
        yield return StopReelAndWait(reelR, iR);
        if (myToken != token) yield break;

        yield return WaitSecondsRealtime(stopGap);
        if (myToken != token) yield break;

        // 右停止後：リーチならPUSH待ち、無ければ中央停止
        if (onBeforeStopCenter != null)
        {
            state = State.WaitingPush;
            Debug.Log("[SLOT] WAIT PUSH (center keeps spinning)");

            bool resumed = false;
            onBeforeStopCenter.Invoke(() => resumed = true);

            // PUSHされるまで待つ（中央は回り続ける）
            while (!resumed)
            {
                if (myToken != token) yield break;
                yield return null;
            }
        }

        // 中央停止
        yield return StopCenterAndFinish(myToken, reelC, iC, tL, tC, tR, onFinished);
    }

    private IEnumerator StopReelAndWait(ReelControllerTween reel, int targetIndex)
    {
        bool done = false;
        reel.StopAt(targetIndex, extraSteps, () => done = true);
        while (!done) yield return null;
    }

    private IEnumerator StopCenterAndFinish(int myToken, ReelControllerTween reel, int targetIndexC, int tL, int tC, int tR, Action onFinished)
    {
        if (centerStopRequested) yield break;
        centerStopRequested = true;

        state = State.StoppingCenter;

        bool done = false;
        reel.StopAt(targetIndexC, extraSteps, () => done = true);

        while (!done)
        {
            if (myToken != token) yield break;
            yield return null;
        }

        Debug.Log($"[SLOT FINISH] RESULT L={tL} C={tC} R={tR}");

        state = State.Idle;
        spinRoutine = null;

        onFinished?.Invoke();
        TryDequeueAndStartNext();
    }

    private static IEnumerator WaitSecondsRealtime(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void TryDequeueAndStartNext()
    {
        if (IsBusy) return;
        if (queue.Count <= 0) return;

        var req = queue.Dequeue();
        Debug.Log($"[SLOT] dequeue next. remain={queue.Count}");

        StartSpinInternal(req.tL, req.tC, req.tR, req.onBeforeStopCenter, req.onFinished);
    }

    public void Hide()
    {
        token++;

        if (spinRoutine != null)
        {
            StopCoroutine(spinRoutine);
            spinRoutine = null;
        }

        state = State.Idle;
        queue.Clear();
        centerStopRequested = false;

        if (slotPanel != null) slotPanel.SetActive(false);
    }
}
