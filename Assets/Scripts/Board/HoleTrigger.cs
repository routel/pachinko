using System.Collections;
using UnityEngine;

public class HoleTrigger : MonoBehaviour
{

    [Header("Gate (for Prize)")]
    [SerializeField] private bool isGateControlled = false; // アタッカーとして開閉するか
    [SerializeField] private Collider2D gateCollider;       // 開閉するCollider（未指定なら自分のCollider）


    [Header("Entity Data")]
    [SerializeField] private HoleConfig config;

    [Header("Ball Tag")]
    [SerializeField] private string ballTag = "Ball";

    [Header("Visibility (for Prize)")]
    [SerializeField] private bool isVisibilityControlled = false; // 当たり中だけ表示するか
    [SerializeField] private Renderer[] renderersToToggle;         // 未指定なら自分のRenderer


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
        // ★ 初回は必ず反映させるため、わざと逆を入れておく
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(ballTag)) return;
        if (config == null)
        {
            Debug.LogError("HoleTrigger: config is not set.");
            return;
        }

        // ★ アタッカー（Prize）は当たり中だけ有効
        if (config.type == HoleType.Prize)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsHit)
                return;
        }

        // 多重判定防止
        if (other.GetComponent<BallHoleGuard>() != null) return;
        other.gameObject.AddComponent<BallHoleGuard>();

        StartCoroutine(SuctionAndApply(other.gameObject));
    }


    public void SetGateOpen(bool open)
    {
        // Colliderだけでなく、可視も含めて統合
        SetActiveForGate(open);
    }


    private IEnumerator SuctionAndApply(GameObject ball)
    {
        // 吸い込み開始前に “機能” を確定（演出は後）
        ApplyLogic();

        // 吸い込み（既存の仕組みと同等）
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

        Vector3 startPos = ball.transform.position;
        Vector3 targetPos = transform.position;
        Vector3 startScale = ball.transform.localScale;
        Vector3 targetScale = startScale * config.endScale;

        float t = 0f;
        float duration = Mathf.Max(0.01f, config.suctionTime);

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            ball.transform.position = Vector3.Lerp(startPos, targetPos, p);
            ball.transform.localScale = Vector3.Lerp(startScale, targetScale, p);

            yield return null;
        }

        Destroy(ball);
    }

    private void ApplyLogic()
    {
        // INカウント（必要なら）
        if (GameManager.Instance != null && config.addInCount != 0)
            GameManager.Instance.AddInCount(config.addInCount);

        // 種別ごとの処理
        switch (config.type)
        {
            case HoleType.Start:
                // 抽選を回す（賞球は基本0でOK。必要なら config.prizeBalls を設定）
                if (LotteryManager.Instance != null)
                    LotteryManager.Instance.DoLottery();

                if (GameManager.Instance != null && config.prizeBalls != 0)
                    GameManager.Instance.AddBalls(config.prizeBalls);
                break;

            case HoleType.Prize:
                // 賞球（アタッカー）
                if (GameManager.Instance != null && config.prizeBalls != 0)
                    GameManager.Instance.AddBalls(config.prizeBalls);
                break;

            case HoleType.Out:
                // Outは吸い込み後に消えるだけ（賞球なし）
                break;
        }
    }

    // 多重判定防止用
    private class BallHoleGuard : MonoBehaviour { }
}
