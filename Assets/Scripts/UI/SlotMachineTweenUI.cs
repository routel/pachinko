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

    /// <summary>
    /// tL,tC,tR は「1～9」を想定（slot_1..slot_9）
    /// 停止順：左→右→中央
    /// </summary>
    public void StartSpinWithTargets(int tL, int tC, int tR, System.Action onFinished)
    {
        if (reelL == null || reelC == null || reelR == null)
        {
            Debug.LogError("SlotMachineTweenUI: Reel references are not set.");
            onFinished?.Invoke();
            return;
        }

        // パネル表示
        if (slotPanel != null) slotPanel.SetActive(true);

        // 既存シーケンス停止
        if (seq != null && seq.IsActive()) seq.Kill();
        seq = null;

        // 1..9 → 0..8
        int iL = Mathf.Clamp(tL - 1, 0, 8);
        int iC = Mathf.Clamp(tC - 1, 0, 8);
        int iR = Mathf.Clamp(tR - 1, 0, 8);
        Debug.Log($"[SLOT RESULT ] 左:{iL + 1} 中:{iC + 1} 右:{iR + 1}");
        // ループ開始
        reelL.StartLoop();
        reelC.StartLoop();
        reelR.StartLoop();

        // 停止シーケンス（フィールド seq だけを使う）
        seq = DOTween.Sequence();
        seq.AppendInterval(preSpinTime);

        // 左停止
        seq.Append(reelL.StopAt(iL, extraSteps, null));
        seq.AppendInterval(stopGap);

        // 右停止
        seq.Append(reelR.StopAt(iR, extraSteps, null));
        seq.AppendInterval(stopGap);

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
