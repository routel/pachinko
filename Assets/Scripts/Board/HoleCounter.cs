using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoleCounter : MonoBehaviour
{
    [Header("Count")]
    [SerializeField] private int addCount = 1;
    [SerializeField] private string ballTag = "Ball";

    [Header("Suction")]
    [SerializeField] private float suctionTime = 0.2f;     // 吸い込み時間
    [SerializeField] private float endScale = 0.15f;       // 最後に小さくする比率
    [SerializeField] private bool disableGravityDuringSuction = true;

    [Header("Prize")]
    [SerializeField] private int prizeBalls = 5; // 入賞で増える玉数（仮）


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(ballTag)) return;

        // 多重判定防止：同じ玉が複数回入った扱いにならないようにする
        //（Holeが複数/Triggerが重なるケースにも強い）
        if (other.TryGetComponent<BallSuctionGuard>(out _)) return;
        other.gameObject.AddComponent<BallSuctionGuard>();

        StartCoroutine(SuctionAndCount(other.gameObject));
    }

    private IEnumerator SuctionAndCount(GameObject ball)
    {
        // ここでカウント（演出前でも後でもOK。今回は即反映）
        GameManager.Instance?.AddInCount(addCount);
        GameManager.Instance?.AddBalls(prizeBalls);



        var rb = ball.GetComponent<Rigidbody2D>();
        var col = ball.GetComponent<Collider2D>();

        // 物理を止めて、吸い込み中に変な跳ね返りが起きないようにする
        if (col != null) col.enabled = false;

        float prevGravity = 0f;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;

            if (disableGravityDuringSuction)
            {
                prevGravity = rb.gravityScale;
                rb.gravityScale = 0f;
            }

            // 位置をスクリプトで動かすので、物理の影響を弱める
            rb.isKinematic = true; // Unityのバージョンによっては warning が出る場合あり（動作はOK）
        }

        Vector3 startPos = ball.transform.position;
        Vector3 targetPos = transform.position; // 穴の中心
        Vector3 startScale = ball.transform.localScale;
        Vector3 targetScale = startScale * endScale;

        float t = 0f;
        while (t < suctionTime)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / suctionTime);

            // 位置とサイズを補間して「吸い込まれる感」
            ball.transform.position = Vector3.Lerp(startPos, targetPos, p);
            ball.transform.localScale = Vector3.Lerp(startScale, targetScale, p);

            yield return null;
        }

        Destroy(ball);
    }

    // 多重判定防止用の目印（中身は空でOK）
    private class BallSuctionGuard : MonoBehaviour { }
}
