using UnityEngine;

/// <summary>
/// 盤面の「基準枠(BoardBounds)」を提供するだけのコンポーネント。
/// 物理には一切関与しない。壁・peg・hole・camera などはここを参照して動く。
/// </summary>
public class BoardBoundsProvider : MonoBehaviour
{
    [SerializeField] private SpriteRenderer boundsRenderer;

    public Bounds Bounds
    {
        get
        {
            if (!boundsRenderer) return new Bounds(Vector3.zero, Vector3.zero);
            return boundsRenderer.bounds;
        }
    }

    public Vector2 Min => new Vector2(Bounds.min.x, Bounds.min.y);
    public Vector2 Max => new Vector2(Bounds.max.x, Bounds.max.y);
    public Vector2 Size => new Vector2(Bounds.size.x, Bounds.size.y);
    public Vector2 Center => new Vector2(Bounds.center.x, Bounds.center.y);

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!boundsRenderer)
            boundsRenderer = GetComponent<SpriteRenderer>();
    }
#endif
}
