using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrizeGateController : MonoBehaviour
{
    [Header("Cap Visual (Gray Circle)")]
    [SerializeField] private Renderer capRenderer; // ClosePrize の SpriteRenderer

    [Header("Open/Close Cycle (per round)")]
    [SerializeField] private float openTime = 0.30f;
    [SerializeField] private float closeTime = 0.50f;

    private Tween cycleTween;
    private bool isOpen;
    private bool cycling;

    public bool IsOpen => isOpen;   // isOpen は Open/Close で切り替えてるやつ

    private void Awake()
    {
        // 初期は閉（灰色フタ表示）
        SetCapVisible(true);
        isOpen = false;
    }

    public void StartCycle()
    {
        if (cycling) return;
        cycling = true;

        StopCycleInternal(killOnly: true);

        float ot = Mathf.Max(0.05f, openTime);
        float ct = Mathf.Max(0.05f, closeTime);

        Debug.Log($"[Gate] StartCycle id={GetInstanceID()} openTime={openTime} closeTime={closeTime} (clamped {ot}/{ct}) frame={Time.frameCount}");

        cycleTween = DOTween.Sequence()
            .SetUpdate(true)
            .AppendCallback(Open)
            .AppendInterval(ot)
            .AppendCallback(Close)
            .AppendInterval(ct)
            .SetLoops(-1);
    }

    public void StopCycle()
    {
        StopCycleInternal(killOnly: false);
    }

    private void StopCycleInternal(bool killOnly)
    {
        if (cycleTween != null && cycleTween.IsActive()) cycleTween.Kill();
        cycleTween = null;

        if (!killOnly)
        {
            cycling = false;
            Close();
            Debug.Log("[Gate] StopCycle");
        }
    }

    public void Open()
    {
        if (isOpen) return;       // ★既に開なら何もしない
        isOpen = true;
        SetCapVisible(false);
        Debug.Log("[Gate] OPEN (cap off)");
    }

    public void Close()
    {
        if (!isOpen) return;      // ★既に閉なら何もしない
        isOpen = false;
        SetCapVisible(true);
        Debug.Log("[Gate] CLOSE (cap on)");
    }

    private void SetCapVisible(bool visible)
    {
        if (capRenderer != null)
            capRenderer.enabled = visible;
    }

    private void OnDisable()
    {
        if (cycleTween != null && cycleTween.IsActive()) cycleTween.Kill();
        cycleTween = null;
    }
}