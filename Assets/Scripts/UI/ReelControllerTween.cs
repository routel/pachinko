using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class ReelControllerTween : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform window;      // RectMask2D付き
    [SerializeField] private RectTransform content;     // Windowの子（Strip）
    [SerializeField] private Image itemTemplate;        // テンプレ（Cell_00）

    [Header("Symbols (slot_1..slot_9)")]
    [SerializeField] private Sprite[] symbols;          // 0..8 = 1..9

    [Header("Result row")]
    [Tooltip("結果として見ている段。0=最上段, 1=2段目, 2=3段目...（3段表示なら通常1）")]
    [SerializeField] private int resultCellIndex = 1;

    [Header("Spin")]
    [SerializeField] private float loopSpeed = 1600f;

    [Header("Stop Step")]
    [SerializeField] private float stepDuration = 0.03f;
    [SerializeField] private Ease stepEase = Ease.Linear;

    [Header("Stop")]
    [SerializeField] private int defaultExtraSteps = 20;
    [SerializeField] private float stopStepDuration = 0.12f; // 最後の1ステップだけゆっくり
    [SerializeField] private Ease stopEase = Ease.OutCubic;

    private Image[] cells;
    private float itemH;
    private int symbolCount;

    private Tween loopTween;
    private Tween stopTween;   // ★StopAtで作ったSequenceを保持
    private bool spinning;

    private float loopY;
    private int topSymbolIndex = 0;

    public bool IsSpinning => spinning;

    private void Awake()
    {
        if (!window || !content || !itemTemplate)
        {
            Debug.LogError("ReelControllerTween: refs not set");
            return;
        }
        if (symbols == null || symbols.Length == 0)
        {
            Debug.LogError("ReelControllerTween: symbols not set");
            return;
        }

        symbolCount = symbols.Length;
        if (symbolCount < 2)
        {
            Debug.LogError("ReelControllerTween: symbols length too small");
            return;
        }

        SetTopAnchors(window);
        SetTopAnchors(content);
        SetTopAnchors((RectTransform)itemTemplate.transform);

        var rtT = (RectTransform)itemTemplate.transform;
        itemH = rtT.rect.height;
        if (itemH <= 0.01f) itemH = 100f;

        BuildFixedCells();

        loopY = 0f;
        content.anchoredPosition = Vector2.zero;

        ApplySpritesFromTopIndex();
    }

    private void SetTopAnchors(RectTransform rt)
    {
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
    }

    private void BuildFixedCells()
    {
        float winH = window.rect.height;
        int visible = Mathf.Max(3, Mathf.CeilToInt(winH / itemH));
        int cellCount = visible + 2;

        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var c = content.GetChild(i);
            if (c == itemTemplate.transform) continue;
            Destroy(c.gameObject);
        }

        cells = new Image[cellCount];

        itemTemplate.name = "Cell_00";
        itemTemplate.preserveAspect = true;

        var rt0 = (RectTransform)itemTemplate.transform;
        SetTopAnchors(rt0);
        rt0.anchoredPosition = new Vector2(0f, 0f);
        cells[0] = itemTemplate;

        for (int i = 1; i < cellCount; i++)
        {
            var img = Instantiate(itemTemplate, content);
            img.name = $"Cell_{i:00}";
            img.preserveAspect = true;

            var rt = (RectTransform)img.transform;
            SetTopAnchors(rt);
            rt.anchoredPosition = new Vector2(0f, -i * itemH);

            cells[i] = img;
        }

        content.sizeDelta = new Vector2(content.sizeDelta.x, cellCount * itemH);
        resultCellIndex = Mathf.Clamp(resultCellIndex, 0, cells.Length - 1);
    }

    private void ApplySpritesFromTopIndex()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            int s = (topSymbolIndex + i) % symbolCount;
            cells[i].sprite = symbols[s];
        }
    }

    /// <summary>回し始め（無限ループ）</summary>
    public void StartLoop(bool forceRestart = false)
    {
        if (spinning && !forceRestart) return;

        KillTweens(); // ★loop/stop両方を必ず殺す
        spinning = true;

        loopY = 0f;
        content.anchoredPosition = Vector2.zero;

        // ★targetをcontentにする（Kill(content)が効く）
        loopTween = DOTween.To(() => 0f, _ => { }, 1f, 9999f)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .SetTarget(content)
            .OnUpdate(() =>
            {
                loopY -= loopSpeed * Time.unscaledDeltaTime;

                while (loopY <= -itemH)
                {
                    loopY += itemH;
                    topSymbolIndex = (topSymbolIndex + 1) % symbolCount;
                    ApplySpritesFromTopIndex();
                }

                content.anchoredPosition = new Vector2(0f, loopY);
            });
    }

    /// <summary>
    /// targetIndex(0..8) に停止。extraStepsは演出用。
    /// 結果セル（resultCellIndex）が targetIndex になるように止める
    /// </summary>
    public Tween StopAt(int targetIndex, int extraSteps = -1, Action onComplete = null)
    {
        if (extraSteps < 0) extraSteps = defaultExtraSteps;
        targetIndex = Mathf.Clamp(targetIndex, 0, symbolCount - 1);

        // ★停止Tweenが残ってたら殺す（競合防止）
        if (stopTween != null && stopTween.IsActive()) stopTween.Kill();
        stopTween = null;

        // ★ループを止める
        if (loopTween != null && loopTween.IsActive()) loopTween.Kill();
        loopTween = null;

        int currentResult = (topSymbolIndex + resultCellIndex) % symbolCount;
        int afterExtra = (currentResult + (extraSteps % symbolCount)) % symbolCount;

        int need = targetIndex - afterExtra;
        if (need < 0) need += symbolCount;

        int totalSteps = extraSteps + need;

        var s = DOTween.Sequence().SetUpdate(true).SetTarget(content);
        stopTween = s;

        float y = loopY;

        for (int i = 0; i < Mathf.Max(0, totalSteps); i++)
        {
            s.Append(
                DOTween.To(() => y, v => y = v, y - itemH, Mathf.Max(0.001f, stepDuration))
                    .SetEase(stepEase)
                    .SetUpdate(true)
                    .SetTarget(content)
                    .OnUpdate(() => content.anchoredPosition = new Vector2(0f, y))
            );

            s.AppendCallback(() =>
            {
                y += itemH;
                topSymbolIndex = (topSymbolIndex + 1) % symbolCount;
                ApplySpritesFromTopIndex();
                content.anchoredPosition = new Vector2(0f, y);
            });
        }

        s.Append(
            DOTween.To(() => y, v => y = v, y - itemH, Mathf.Max(0.001f, stopStepDuration))
                .SetEase(stopEase)
                .SetUpdate(true)
                .SetTarget(content)
                .OnUpdate(() => content.anchoredPosition = new Vector2(0f, y))
        );

        s.AppendCallback(() =>
        {
            y += itemH;
            topSymbolIndex = (topSymbolIndex + 1) % symbolCount;
            ApplySpritesFromTopIndex();

            loopY = 0f;
            content.anchoredPosition = Vector2.zero;

            spinning = false;
            stopTween = null;
            onComplete?.Invoke();
        });

        return s;
    }

    private void KillTweens()
    {
        if (loopTween != null && loopTween.IsActive()) loopTween.Kill();
        loopTween = null;

        if (stopTween != null && stopTween.IsActive()) stopTween.Kill();
        stopTween = null;

        DOTween.Kill(content); // SetTargetしたので保険として効く
        spinning = false;

        loopY = 0f;
        if (content) content.anchoredPosition = Vector2.zero;
    }

    private void OnDisable() => KillTweens();
}
