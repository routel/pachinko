using UnityEngine;

public class LcdLayerController : MonoBehaviour
{
    [Header("Canvases (Overlay recommended)")]
    [SerializeField] private Canvas bgCanvas;
    [SerializeField] private Canvas videoCanvas;
    [SerializeField] private Canvas uiCanvas;

    [Header("Base Orders")]
    [SerializeField] private int bgOrder = 0;
    [SerializeField] private int videoOrder = 10;
    [SerializeField] private int uiOrder = 20;

    private void Awake()
    {
        ApplyOrders(bgOrder, videoOrder, uiOrder);
    }

    public void SetVideoAboveSlot(bool videoAbove)
    {
        // videoAbove=true Ç»ÇÁ VIDEO Ç™ UI ÇÊÇËè„
        if (videoAbove)
            ApplyOrders(bgOrder, uiOrder, videoOrder); // UIÇ∆VIDEOÇì¸ÇÍë÷Ç¶
        else
            ApplyOrders(bgOrder, videoOrder, uiOrder);
    }

    private void ApplyOrders(int bg, int video, int ui)
    {
        if (bgCanvas) bgCanvas.sortingOrder = bg;
        if (videoCanvas) videoCanvas.sortingOrder = video;
        if (uiCanvas) uiCanvas.sortingOrder = ui;
    }
}
