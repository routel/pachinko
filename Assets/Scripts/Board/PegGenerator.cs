using System;
using UnityEngine;

public class PegGenerator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform pegsRoot;      // Board/Pegs
    [SerializeField] private GameObject pegPrefab;    // Peg.prefab

    [Header("Entity Data")]
    [SerializeField] private PegLayoutConfig layout;


    [ContextMenu("Rebuild Pegs")]
    public void Rebuild()
    {
        if (pegsRoot == null || pegPrefab == null || layout == null)
        {
            Debug.LogError("PegGenerator: pegsRoot / pegPrefab / layout is not set.");
            return;
        }

        Clear();

        // ‡@ Žè“®•Û‘¶iÄ‚«ž‚Ýj‚ª‚ ‚é‚È‚çA‚»‚ê‚ð—Dæ‚µ‚Ä•œŒ³
        if (layout.useBakedPositions && layout.bakedPositions != null && layout.bakedPositions.Count > 0)
        {
            foreach (var pos in layout.bakedPositions)
                Instantiate(pegPrefab, pos, Quaternion.identity, pegsRoot);

            Debug.Log($"Pegs rebuilt from baked positions: {layout.bakedPositions.Count}");
            return;
        }

        // ‡A –³‚¯‚ê‚Î seed ‚©‚ç¶¬
        var rnd = new System.Random(layout.seed);

        int row = 0;
        for (float y = layout.areaMax.y; y >= layout.areaMin.y; y -= layout.spacingY, row++)
        {
            bool offsetRow = (row % 2 == 1);
            float startX = layout.areaMin.x + (offsetRow ? layout.spacingX * 0.5f : 0f);

            for (float x = startX; x <= layout.areaMax.x; x += layout.spacingX)
            {
                if (x < layout.areaMin.x + 0.1f || x > layout.areaMax.x - 0.1f) continue;

                float jx = ((float)rnd.NextDouble() * 2 - 1) * layout.jitter;
                float jy = ((float)rnd.NextDouble() * 2 - 1) * layout.jitter;

                Vector2 pos = new Vector2(x + jx, y + jy);
                Instantiate(pegPrefab, pos, Quaternion.identity, pegsRoot);
            }
        }

        Debug.Log("Pegs rebuilt from seed layout.");
    }

    [ContextMenu("Clear Pegs")]
    public void Clear()
    {
        if (pegsRoot == null) return;

        for (int i = pegsRoot.childCount - 1; i >= 0; i--)
            DestroyImmediate(pegsRoot.GetChild(i).gameObject);
    }

    [ContextMenu("Bake Current Pegs To Layout (Manual Save)")]
    public void BakeCurrentPegsToLayout()
    {
        if (pegsRoot == null || layout == null)
        {
            Debug.LogError("PegGenerator: pegsRoot / layout is not set.");
            return;
        }

        layout.bakedPositions.Clear();
        for (int i = 0; i < pegsRoot.childCount; i++)
            layout.bakedPositions.Add(pegsRoot.GetChild(i).position);

        layout.useBakedPositions = true;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(layout);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif

        Debug.Log($"Baked {layout.bakedPositions.Count} pegs into LayoutAsset: {layout.name}");
    }

    [ContextMenu("Unbake (Return to Seed Generation)")]
    public void Unbake()
    {
        if (layout == null) return;

        layout.useBakedPositions = false;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(layout);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
#endif

        Debug.Log($"Unbaked LayoutAsset: {layout.name}");
    }

    private void OnDrawGizmosSelected()
    {
        if (layout == null) return;

        Gizmos.color = Color.cyan;
        Vector3 a = new Vector3(layout.areaMin.x, layout.areaMin.y, 0);
        Vector3 b = new Vector3(layout.areaMax.x, layout.areaMin.y, 0);
        Vector3 c = new Vector3(layout.areaMax.x, layout.areaMax.y, 0);
        Vector3 d = new Vector3(layout.areaMin.x, layout.areaMax.y, 0);
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
}
