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
    private bool spinning;

    // 「cells[0]（最上段）」に来ている絵柄 index（0..8）
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

        // 上基準で統一
        SetTopAnchors(window);
        SetTopAnchors(content);
        SetTopAnchors((RectTransform)itemTemplate.transform);

        // 高さ（テンプレRect）
        var rtT = (RectTransform)itemTemplate.transform;
        itemH = rtT.rect.height;
        if (itemH <= 0.01f) itemH = 100f;

        BuildFixedCells();

        // 初期位置は必ず 0
        content.anchoredPosition = Vector2.zero;

        // 初期絵柄反映
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
        int cellCount = visible + 2; // 上下バッファ

        // テンプレ以外を削除
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var c = content.GetChild(i);
            if (c == itemTemplate.transform) continue;
            Destroy(c.gameObject);
        }

        cells = new Image[cellCount];

        // Cell_00
        itemTemplate.name = "Cell_00";
        itemTemplate.preserveAspect = true;

        var rt0 = (RectTransform)itemTemplate.transform;
        SetTopAnchors(rt0);
        rt0.anchoredPosition = new Vector2(0f, 0f);
        cells[0] = itemTemplate;

        // 残り生成（位置固定）
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

        // Layoutは使わない。サイズだけ
        content.sizeDelta = new Vector2(content.sizeDelta.x, cellCount * itemH);

        // resultCellIndex を範囲内に
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
    public void StartLoop()
    {
        KillTweens();
        spinning = true;

        // ★絶対に 0 → -itemH → 0 に戻す（ズレが蓄積しない）
        content.anchoredPosition = Vector2.zero;

        loopTween = DOTween.Sequence()
            .Append(content.DOAnchorPosY(-itemH, stepDuration).SetEase(stepEase)) // 絶対値で -itemH
            .AppendCallback(StepOneSnap)                                         // 0に戻して絵柄送り
            .SetLoops(-1, LoopType.Restart);
    }

    /// <summary>
    /// targetIndex(0..8) に停止。extraStepsは演出用。
    /// 結果セル（resultCellIndex）が targetIndex になるように止める
    /// </summary>
    public Tween StopAt(int targetIndex, int extraSteps = -1, Action onComplete = null)
    {
        if (extraSteps < 0) extraSteps = defaultExtraSteps;
        targetIndex = Mathf.Clamp(targetIndex, 0, symbolCount - 1);

        if (loopTween != null && loopTween.IsActive()) loopTween.Kill();

        // ★現在の結果セルの値
        int currentResult = (topSymbolIndex + resultCellIndex) % symbolCount;

        // extraSteps後の値
        int afterExtra = (currentResult + (extraSteps % symbolCount)) % symbolCount;

        // targetへ必要な追加ステップ
        int need = targetIndex - afterExtra;
        if (need < 0) need += symbolCount;

        int totalSteps = extraSteps + need;

        var seq = DOTween.Sequence();

        // 念のため開始位置を0に固定
        seq.AppendCallback(() => content.anchoredPosition = Vector2.zero);

        // 途中は一定速度
        for (int i = 0; i < Mathf.Max(0, totalSteps); i++)
        {
            seq.Append(content.DOAnchorPosY(-itemH, stepDuration).SetEase(stepEase));
            seq.AppendCallback(StepOneSnap);
        }

        // 最後の1ステップだけ減速
        seq.Append(content.DOAnchorPosY(-itemH, stopStepDuration).SetEase(stopEase));
        seq.AppendCallback(() =>
        {
            StepOneSnap();
            spinning = false;
            onComplete?.Invoke();
        });

        return seq;
    }

    /// <summary>
    /// 1ステップ進める：必ず 0 にスナップしてから topSymbolIndex を進め、絵柄を更新
    /// </summary>
    private void StepOneSnap()
    {
        // ここが肝：毎回 0 に戻す（浮動小数のズレを蓄積させない）
        content.anchoredPosition = Vector2.zero;

        // 次の絵柄へ進める
        topSymbolIndex = (topSymbolIndex + 1) % symbolCount;

        // 表示を確定
        ApplySpritesFromTopIndex();
    }

    /// <summary>結果の数字(1..9)</summary>
    public int GetResultNumber1to9()
    {
        int idx = (topSymbolIndex + resultCellIndex) % symbolCount;
        return idx + 1;
    }

    private void KillTweens()
    {
        if (loopTween != null && loopTween.IsActive()) loopTween.Kill();
        DOTween.Kill(content);
        spinning = false;
    }

    private void OnDisable() => KillTweens();
}
