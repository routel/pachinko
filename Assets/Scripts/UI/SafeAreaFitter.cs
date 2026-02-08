using UnityEngine;

[ExecuteAlways]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform rt;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        Apply();
    }

    private void OnEnable() => Apply();
    private void Update()
    {
#if UNITY_EDITOR
        Apply(); // エディタで常時追従
#endif
    }

    public void Apply()
    {
        if (rt == null) rt = GetComponent<RectTransform>();

        Rect safe = Screen.safeArea;

        Vector2 min = safe.position;
        Vector2 max = safe.position + safe.size;

        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rt.anchorMin = min;
        rt.anchorMax = max;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
