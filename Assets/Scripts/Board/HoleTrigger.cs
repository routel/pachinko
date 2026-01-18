using UnityEngine;
using DG.Tweening;

public class HoleTrigger : MonoBehaviour
{
    [Header("Gate (for Prize)")]
    [SerializeField] private bool isGateControlled = false;
    [SerializeField] private Collider2D gateCollider;

    [Header("Entity Data")]
    [SerializeField] private HoleConfig config;

    [Header("Ball Tag")]
    [SerializeField] private string ballTag = "Ball";

    [Header("Visibility (for Prize)")]
    [SerializeField] private bool isVisibilityControlled = false;
    [SerializeField] private Renderer[] renderersToToggle;

    private bool lastGateActive;

    private void Awake()
    {
        if (gateCollider == null)
            gateCollider = GetComponent<Collider2D>();

        if (renderersToToggle == null || renderersToToggle.Length == 0)
        {
            var r = GetComponent<Renderer>();
            if (r != null) renderersToToggle = new[] { r };
        }
    }

    private void Start()
    {
        lastGateActive = !GetDesiredActive();
        ApplyGateVisual(force: true);
    }

    private void Update()
    {
        ApplyGateVisual(force: false);
    }

    private bool GetDesiredActive()
    {
        if (config == null || config.type != HoleType.Prize) return true; // Prize以外は常に表示
        return GameManager.Instance != null && GameManager.Instance.IsHit;
    }

    private void ApplyGateVisual(bool force)
    {
        if (config == null || config.type != HoleType.Prize) return;

        bool active = GetDesiredActive();
        if (!force && active == lastGateActive) return;

        lastGateActive = active;
        SetActiveForGate(active);
    }

    public void SetActiveForGate(bool active)
    {
        // Collider（判定）
        if (isGateControlled && gateCollider != null)
            gateCollider.enabled = active;

        // 見た目（表示）
        if (isVisibilityControlled && renderersToToggle != null)
        {
            foreach (var r in renderersToToggle)
                if (r != null) r.enabled = active;
        }
    }

    public void SetGateOpen(bool open)
    {
        SetActiveForGate(open);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(ballTag)) return;

        if (config == null)
        {
            Debug.LogError("HoleTrigger: config is not set.");
            return;
        }

        // Prizeは当たり中だけ有効
        if (config.type == HoleType.Prize)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsHit)
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

        Sequence seq = DOTween.Sequence()
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
                if (GameManager.Instance != null && config.prizeBalls != 0)
                    GameManager.Instance.AddBalls(config.prizeBalls);
                break;

            case HoleType.Out:
                break;
        }
    }

    // 多重判定防止用
    private class BallHoleGuard : MonoBehaviour { }
}
