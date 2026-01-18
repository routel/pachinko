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
    [SerializeField] private float preSpinTime = 0.6f;   // 回し始めてから最初に止めるまで
    [SerializeField] private float stopGap = 0.25f;      // 左→右→中央の停止間隔

    private int extraSteps = 20;

    private Sequence seq;
    public bool IsSpinning => seq != null && seq.IsActive();

    private void Awake()
    {
        if (slotPanel != null) slotPanel.SetActive(false);
    }

    // 互換：待機なし（従来どおり）
    public void StartSpinWithTargets(int tL, int tC, int tR, System.Action onFinished)
    {
        StartSpinWithTargets(tL, tC, tR, null, onFinished);
    }

    /// <summary>
    /// 右停止後～中央停止前に onBeforeStopCenter(resume) を呼ぶ。
    /// PUSHが押されたら resume() を呼ぶと中央停止へ進む。
    /// </summary>
    public void StartSpinWithTargets(
        int tL, int tC, int tR,
        System.Action<System.Action> onBeforeStopCenter,
        System.Action onFinished)
    {
        if (reelL == null || reelC == null || reelR == null)
        {
            Debug.LogError("SlotMachineTweenUI: Reel references are not set.");
            onFinished?.Invoke();
            return;
        }

        if (slotPanel != null) slotPanel.SetActive(true);

        if (seq != null && seq.IsActive()) seq.Kill();
        seq = null;

        // 1..9 → 0..8
        int iL = Mathf.Clamp(tL - 1, 0, 8);
        int iC = Mathf.Clamp(tC - 1, 0, 8);
        int iR = Mathf.Clamp(tR - 1, 0, 8);

        Debug.Log($"[SLOT TARGET] L={iL + 1} C={iC + 1} R={iR + 1}");

        // ★ 3つとも回し始める（中央はPUSHまで回り続ける）
        reelL.StartLoop();
        reelC.StartLoop();
        reelR.StartLoop();

        seq = DOTween.Sequence();
        seq.AppendInterval(preSpinTime);

        // 左停止
        seq.Append(reelL.StopAt(iL, extraSteps, null));
        seq.AppendInterval(stopGap);

        // 右停止
        seq.Append(reelR.StopAt(iR, extraSteps, null));
        seq.AppendInterval(stopGap);

        Debug.Log("CENTER spinning=" + reelC.IsSpinning);

        // ★ 右停止後に「待機」を挟む（ここでseqを止める）
        if (onBeforeStopCenter != null)
        {
            seq.AppendCallback(() =>
            {
                Debug.Log("[Slot] BEFORE CENTER STOP -> pause (center should keep looping)");

                // 念のため中央ループが動いていることを保証（StartLoopが二重起動でまずいならReel側でガード）
                reelC.StartLoop();

                // ここで停止 → resume() で再開
                seq.Pause();

                onBeforeStopCenter.Invoke(() =>
                {
                    Debug.Log("[Slot] RESUME -> center stop");
                    if (seq != null && seq.IsActive()) seq.Play();
                });
            });

            // Pause位置確保
            seq.AppendInterval(0f);
        }

        // 中央停止（最後に完了通知）
        seq.Append(reelC.StopAt(iC, extraSteps, onFinished));
    }

    public void Hide()
    {
        if (seq != null && seq.IsActive()) seq.Kill();
        seq = null;
        if (slotPanel != null) slotPanel.SetActive(false);
    }
}
