using UnityEngine;

[ExecuteAlways]
public class CameraFitToUIRect : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RectTransform playArea; // UIのPlayArea
    [SerializeField] private Canvas rootCanvas;      // UIRootCanvas

    [Header("Options")]
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool updateEveryFrame = true;

    private void Reset()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void Awake()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        Apply();
    }

    private void OnEnable() => Apply();

    private void Update()
    {
#if UNITY_EDITOR
        if (updateEveryFrame) Apply();
#endif
        // 実機でもリサイズ対応したいなら有効化
        if (!Application.isEditor && updateEveryFrame) Apply();
    }

    public void Apply()
    {
        if (targetCamera == null || playArea == null) return;

        if (rootCanvas == null)
            rootCanvas = playArea.GetComponentInParent<Canvas>();

        // PlayArea の四隅をスクリーン座標へ
        Vector3[] corners = new Vector3[4];
        playArea.GetWorldCorners(corners);

        Camera uiCam = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = rootCanvas.worldCamera;

        Vector2 bl = RectTransformUtility.WorldToScreenPoint(uiCam, corners[0]); // bottom-left
        Vector2 tr = RectTransformUtility.WorldToScreenPoint(uiCam, corners[2]); // top-right

        // Screen基準の正規化Rectへ変換
        float x = Mathf.Clamp01(bl.x / Screen.width);
        float y = Mathf.Clamp01(bl.y / Screen.height);
        float w = Mathf.Clamp01((tr.x - bl.x) / Screen.width);
        float h = Mathf.Clamp01((tr.y - bl.y) / Screen.height);

        // 万一の負値防止
        w = Mathf.Max(0f, w);
        h = Mathf.Max(0f, h);

        targetCamera.rect = new Rect(x, y, w, h);
    }
}
