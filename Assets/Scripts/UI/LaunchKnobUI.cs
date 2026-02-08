using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LaunchKnobUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI")]
    [SerializeField] private RectTransform knobRotator; // 回転させる画像（つまみ本体）
    [SerializeField] private Button centerButton;       // 中央ボタン（ON/OFF）

    [Header("Angle Range")]
    [SerializeField] private float minAngle = -120f;    // 左端
    [SerializeField] private float maxAngle = 120f;     // 右端

    [Header("Hold Rotation")]
    [Tooltip("長押し中に回る速度（度/秒）")]
    [SerializeField] private float degreesPerSecond = 180f;

    [Header("State")]
    [SerializeField] private bool fireEnabled = false;

    [Header("Launcher")]
    [SerializeField] private DragPlungerLauncher launcher;

    [Header("STOP Visual")]
    [SerializeField] private Graphic centerButtonBg; // Button背景
    [SerializeField] private Color fireOnColor = Color.cyan; // 青
    [SerializeField] private Color fireOffColor = Color.red;  // 赤

    private float currentAngle = 0f;

    // 長押し回転状態
    private bool holding;
    private int holdDir; // +1: 右回転, -1: 左回転

    // OnPowerChangedの連打防止
    private float lastPull01 = -1f;

    public bool FireEnabled => fireEnabled;

    public event Action<bool> OnFireEnabledChanged;
    public event Action<float> OnPowerChanged;

    private void Awake()
    {
        fireEnabled = false; // ★必ず停止開始

        if (centerButton != null)
        {
            centerButton.onClick.RemoveAllListeners();
            centerButton.onClick.AddListener(ToggleFireEnabled);
        }

        if (centerButtonBg == null && centerButton != null)
            centerButtonBg = centerButton.targetGraphic;

        ApplyVisual();
        ApplyStopVisual();
        lastPull01 = Pull01;
    }

    private void Update()
    {
        if (!holding || holdDir == 0) return;

        float delta = holdDir * degreesPerSecond * Time.unscaledDeltaTime;
        RotateBy(delta);
    }

    private void ApplyStopVisual()
    {
        if (centerButtonBg != null)
            centerButtonBg.color = fireEnabled ? fireOnColor : fireOffColor;
    }

    /// <summary>0..1 の発射強さ（右ほど強い/左ほど弱いの定義はこの式で固定）</summary>
    public float Pull01
    {
        get
        {
            float t = Mathf.InverseLerp(minAngle, maxAngle, currentAngle);
            t = Mathf.Clamp01(t);
            return 1f - t;   // ★反転（あなたの現状に合わせて維持）
        }
    }

    private void ToggleFireEnabled()
    {
        fireEnabled = !fireEnabled;
        OnFireEnabledChanged?.Invoke(fireEnabled);

        if (launcher != null)
            launcher.SetAutoFire(fireEnabled);

        ApplyStopVisual();
    }

    // ===== 長押し回転（つまみ部分） =====

    public void OnPointerDown(PointerEventData eventData)
    {
        // 左長押し：右回転 / 右長押し：左回転（現状仕様を維持）
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            holding = true;
            holdDir = +1;
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            holding = true;
            holdDir = -1;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // どちらのボタンでも離したら止める（シンプル運用）
        holding = false;
        holdDir = 0;
    }

    private void RotateBy(float delta)
    {
        float before = currentAngle;
        currentAngle = Mathf.Clamp(currentAngle + delta, minAngle, maxAngle);

        // 変化がない（端に当たってる）なら処理しない
        if (Mathf.Approximately(before, currentAngle))
            return;

        ApplyVisual();

        // pull01が十分変わったときだけ通知
        float p = Pull01;
        if (Mathf.Abs(p - lastPull01) >= 0.001f)
        {
            lastPull01 = p;
            OnPowerChanged?.Invoke(p);
        }
    }

    private void ApplyVisual()
    {
        if (knobRotator != null)
            knobRotator.localEulerAngles = new Vector3(0f, 0f, currentAngle);
    }
}
