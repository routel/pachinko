using UnityEngine;
using UnityEngine.UI;

public class UILayoutDebugTint : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private bool showDebugTint = true;

    [Header("Targets")]
    [SerializeField] private Image headerBg;
    [SerializeField] private Image playBg;
    [SerializeField] private Image footerBg;

    [Header("Colors")]
    [SerializeField] private Color headerColor = new Color(0f, 0.6f, 1f, 0.18f);
    [SerializeField] private Color playColor = new Color(0.2f, 1f, 0.2f, 0.10f);
    [SerializeField] private Color footerColor = new Color(1f, 0.2f, 0.2f, 0.18f);

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    public void Apply()
    {
        ApplyOne(headerBg, headerColor);
        ApplyOne(playBg, playColor);
        ApplyOne(footerBg, footerColor);
    }

    private void ApplyOne(Image img, Color c)
    {
        if (img == null) return;

        img.raycastTarget = false; // “ü—Í‚ðŽ×–‚‚µ‚È‚¢
        img.enabled = showDebugTint;
        if (showDebugTint) img.color = c;
    }
}
