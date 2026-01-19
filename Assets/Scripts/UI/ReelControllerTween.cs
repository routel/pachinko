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
    private bool spinning;

    // 連続移動の累積
    private float loopY;

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
    public void StartLoop()
    {
        if (spinning) return;

        KillTweens();
        spinning = true;

        loopY = 0f;
        content.anchoredPosition = Vector2.zero;

        loopTween = DOTween.To(() => 0f, _ => { }, 1f, 9999f)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
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

        // ★止める時点でループを止める（左右はここで止まってOK）
        if (loopTween != null && loopTween.IsActive()) loopTween.Kill();
        loopTween = null;

        // 現在の結果セル値
        int currentResult = (topSymbolIndex + resultCellIndex) % symbolCount;

        // extraSteps後
        int afterExtra = (currentResult + (extraSteps % symbolCount)) % symbolCount;

        // targetへ必要な追加ステップ
        int need = targetIndex - afterExtra;
        if (need < 0) need += symbolCount;

        int totalSteps = extraSteps + need;

        var s = DOTween.Sequence();
        s.SetUpdate(true);

        // ループ中の位置を引き継ぐ
        float y = loopY;

        // 途中：一定速度
        for (int i = 0; i < Mathf.Max(0, totalSteps); i++)
        {
            s.Append(DOTween.To(() => y, v => y = v, y - itemH, Mathf.Max(0.001f, stepDuration))
                .SetEase(stepEase)
                .SetUpdate(true)
                .OnUpdate(() => content.anchoredPosition = new Vector2(0f, y)));

            s.AppendCallback(() =>
            {
                // 1段進んだので巻き戻し＋絵柄更新
                y += itemH;
                topSymbolIndex = (topSymbolIndex + 1) % symbolCount;
                ApplySpritesFromTopIndex();

                // ★確定
                content.anchoredPosition = new Vector2(0f, y);
            });
        }

        // 最後だけ減速
        s.Append(DOTween.To(() => y, v => y = v, y - itemH, Mathf.Max(0.001f, stopStepDuration))
            .SetEase(stopEase)
            .SetUpdate(true)
            .OnUpdate(() => content.anchoredPosition = new Vector2(0f, y)));

        s.AppendCallback(() =>
        {
            y += itemH;
            topSymbolIndex = (topSymbolIndex + 1) % symbolCount;
            ApplySpritesFromTopIndex();

            // ★停止後は必ず基準(0)に戻す（マスク内停止の安定化）
            loopY = 0f;
            content.anchoredPosition = Vector2.zero;

            spinning = false;
            onComplete?.Invoke();
        });

        return s;
    }

    private void KillTweens()
    {
        if (loopTween != null && loopTween.IsActive()) loopTween.Kill();
        DOTween.Kill(content);
        loopTween = null;
        spinning = false;
        loopY = 0f;
        if (content) content.anchoredPosition = Vector2.zero;
    }

    private void OnDisable() => KillTweens();
}
