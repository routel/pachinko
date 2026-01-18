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

    // 吸い込み中のTweenを持っておく（必要ならキャンセルできる）
    private Tween currentTween;

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
        if (isGateControlled && gateCollider != null)
            gateCollider.enabled = active;

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

        // Prize は当たり中だけ有効
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
        // もし同一ボールにTweenが残っていたら止める
        // （同じballが二重に入った等の事故防止。基本ここには来ないが安全策）
        if (currentTween != null && currentTween.IsActive())
            currentTween.Kill();

        // ★ 吸い込み開始前に“機能”を確定
        ApplyLogic();

        var rb = ball.GetComponent<Rigidbody2D>();
        var col = ball.GetComponent<Collider2D>();

        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
            rb.isKinematic = true;
        }

        float duration = Mathf.Max(0.01f, config.suctionTime);
        Vector3 targetPos = transform.position;

        Vector3 startScale = ball.transform.localScale;
        Vector3 targetScale = startScale * config.endScale;

        // 演出が安定するように、Transformを明示的に掴む
        Transform bt = ball.transform;

        // DOTween：位置＋スケールを同時に
        Sequence seq = DOTween.Sequence();

        seq.Join(bt.DOMove(targetPos, duration).SetEase(Ease.InQuad));
        seq.Join(bt.DOScale(targetScale, duration).SetEase(Ease.InQuad));

        // ballが途中でDestroyされたら自動Kill（事故防止）
        seq.SetLink(ball, LinkBehaviour.KillOnDestroy);

        seq.OnComplete(() =>
        {
            if (ball != null)
                Destroy(ball);
        });

        currentTween = seq;
    }

    private void ApplyLogic()
    {
        if (GameManager.Instance != null && config.addInCount != 0)
            GameManager.Instance.AddInCount(config.addInCount);

        switch (config.type)
        {
            case HoleType.Start:
                if (LotteryManager.Instance != null)
                    LotteryManager.Instance.DoLottery();

                if (GameManager.Instance != null && config.prizeBalls != 0)
                    GameManager.Instance.AddBalls(config.prizeBalls);
                break;

            case HoleType.Prize:
                if (GameManager.Instance != null && config.prizeBalls != 0)
                    GameManager.Instance.AddBalls(config.prizeBalls);
                break;

            case HoleType.Out:
                break;
        }
    }

    private class BallHoleGuard : MonoBehaviour { }
}
