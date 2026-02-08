using UnityEngine;

[ExecuteAlways]
public class BoardCameraVerticalFitter : MonoBehaviour
{
    [Header("Target Camera")]
    [SerializeField] private Camera boardCamera;

    [Header("Board Root (World)")]
    [Tooltip("盤面一式の親（Pegs/Walls/Holes/Launcher等を含む）")]
    [SerializeField] private Transform boardRoot;

    [Header("Fit")]
    [Tooltip("上下に少し余白を足す（ワールド単位）")]
    [SerializeField] private float verticalPadding = 0.3f;

    [Tooltip("盤面中心にカメラを合わせる")]
    [SerializeField] private bool centerToBoard = true;

    [Tooltip("エディタ/実機で画面サイズ変化に追従したい場合ON")]
    [SerializeField] private bool autoUpdate = true;

    private Vector2Int lastScreen;

    private void Reset()
    {
        boardCamera = GetComponent<Camera>();
    }

    private void Awake()
    {
        if (boardCamera == null) boardCamera = GetComponent<Camera>();
        Apply();
    }

    private void OnEnable() => Apply();

    private void Update()
    {
#if UNITY_EDITOR
        if (!autoUpdate) return;
        // エディタでのリサイズやレイアウト調整に追従
        if (Screen.width != lastScreen.x || Screen.height != lastScreen.y)
        {
            Apply();
        }
#else
        if (!autoUpdate) return;
        // Web/実機でもリサイズ追従したい場合
        if (Screen.width != lastScreen.x || Screen.height != lastScreen.y)
        {
            Apply();
        }
#endif
    }

    public void Apply()
    {
        lastScreen = new Vector2Int(Screen.width, Screen.height);

        if (boardCamera == null || boardRoot == null) return;
        if (!boardCamera.orthographic)
        {
            Debug.LogWarning("[BoardCameraVerticalFitter] Camera is not Orthographic.");
            return;
        }

        // 盤面のBoundsを取得（Renderer優先、無ければCollider2D）
        if (!TryGetBoardBounds(boardRoot, out var b))
        {
            Debug.LogWarning("[BoardCameraVerticalFitter] No Renderer/Collider2D found under boardRoot.");
            return;
        }

        // 縦優先：高さが入るように orthographicSize を決める
        float halfH = b.extents.y + Mathf.Max(0f, verticalPadding);
        boardCamera.orthographicSize = Mathf.Max(0.01f, halfH);

        // 中心合わせ
        if (centerToBoard)
        {
            var t = boardCamera.transform;
            t.position = new Vector3(b.center.x, b.center.y, t.position.z);
        }
    }

    private bool TryGetBoardBounds(Transform root, out Bounds bounds)
    {
        // Renderer
        var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
        bool has = false;
        bounds = new Bounds();

        foreach (var r in renderers)
        {
            if (!has)
            {
                bounds = r.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        if (has) return true;

        // Collider2D（Rendererが無い場合）
        var cols = root.GetComponentsInChildren<Collider2D>(includeInactive: true);
        foreach (var c in cols)
        {
            if (!has)
            {
                bounds = c.bounds;
                has = true;
            }
            else
            {
                bounds.Encapsulate(c.bounds);
            }
        }

        return has;
    }
}
