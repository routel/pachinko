using UnityEngine;
using DG.Tweening;

public class HoleTrigger : MonoBehaviour
{
    [Header("Entity Data")]
    [SerializeField] private HoleConfig config;

    [Header("Ball Tag")]
    [SerializeField] private string ballTag = "Ball";

    [SerializeField] private PrizeGateController prizeGate; // Inspectorで割り当て

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(ballTag)) return;

        if (config == null)
        {
            Debug.LogError("[HoleTrigger] config is not set.");
            return;
        }

        // Prize は当たり中だけ有効（論理ロック）
        // ※ 物理ロック（開閉）は PrizeGateController 側の Collider で行う
        if (config.type == HoleType.Prize)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsHit)
                return;

            // ★塞がり中は無効（吸い込みも賞球もしない）
            if (prizeGate != null && !prizeGate.IsOpen)
                return;
        }

        // 多重判定防止
        if (other.GetComponent<BallHoleGuard>() != null) return;
        other.gameObject.AddComponent<BallHoleGuard>();

        SuctionAndApplyTween(other.gameObject);
    }

    private void SuctionAndApplyTween(GameObject ball)
    {
        // 吸い込み開始前に“機能”を確定
        ApplyLogic();

        var rb = ball.GetComponent<Rigidbody2D>();
        var col = ball.GetComponent<Collider2D>();

        // 以降は演出専用にして物理を止める
        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.isKinematic = true;
        }

        Transform bt = ball.transform;

        // その玉に紐づくTweenだけ消す（他の玉に影響しない）
        bt.DOKill();

        float duration = Mathf.Max(0.01f, config.suctionTime);

        Vector3 startScale = bt.localScale;
        Vector3 targetScale = startScale * config.endScale;

        Vector3 targetPos = transform.position;

        DOTween.Sequence()
            .Join(bt.DOMove(targetPos, duration).SetEase(Ease.InQuad))
            .Join(bt.DOScale(targetScale, duration).SetEase(Ease.InQuad))
            .SetLink(ball, LinkBehaviour.KillOnDestroy)
            .OnComplete(() =>
            {
                if (ball != null)
                    Destroy(ball);
            });
    }

    private void ApplyLogic()
    {
        // INカウント
        if (GameManager.Instance != null && config.addInCount != 0)
            GameManager.Instance.AddInCount(config.addInCount);

        switch (config.type)
        {
            case HoleType.Start:
                if (LotteryManager.Instance != null)
                    LotteryManager.Instance.PlayFromStartHole();
                break;

            case HoleType.Prize:
                // Prize は当たり中＆ゲート開のみ OnTriggerEnter2D で通している
                if (HitDirector.Instance != null)
                {
                    HitDirector.Instance.OnPrizeIn();
                }
                else
                {
                    // フォールバック（保険）
                    if (GameManager.Instance != null && config.prizeBalls != 0)
                        GameManager.Instance.AddBalls(config.prizeBalls);
                }
                break;

            case HoleType.Out:
                break;
        }
    }

    // 多重判定防止用
    private class BallHoleGuard : MonoBehaviour { }
}
