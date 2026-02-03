using UnityEngine;

public class DragPlungerLauncher : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Plunger")]
    [SerializeField] private float maxPullDistance = 2.0f;

    [Tooltip("発射方向（上方向が基本。微調整用）")]
    [SerializeField] private Vector2 shootDirection = Vector2.up;

    [Header("Launch Speed (Velocity)")]
    [Tooltip("最小初速（pull01=0でも少しだけ飛ばす）")]
    [SerializeField] private float minSpeed = 6.0f;

    [Tooltip("最大初速（pull01=1のとき）")]
    [SerializeField] private float maxSpeed = 14.0f;

    [Tooltip("初速に対して回転も少し付ける（じゃらじゃら感に寄与）")]
    [SerializeField] private float randomAngularVel = 240f;

    [Header("Legacy (unused)")]
    [SerializeField] private float power = 12f; // 互換のため残す（未使用）

    private Camera cam;
    private bool isDragging;
    private Vector3 dragStartWorld;
    private float pull01;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isDragging = true;
            dragStartWorld = GetMouseWorld();
            pull01 = 0f;
        }

        if (isDragging && Input.GetMouseButton(1))
        {
            Vector3 now = GetMouseWorld();

            float pull = Mathf.Max(0f, (dragStartWorld.y - now.y)); // 下に引いた分だけ
            pull = Mathf.Min(pull, maxPullDistance);

            pull01 = (maxPullDistance <= 0f) ? 0f : (pull / maxPullDistance);
        }

        if (isDragging && Input.GetMouseButtonUp(1))
        {
            isDragging = false;
            Shoot(pull01);
        }
    }

    private void Shoot(float pull01)
    {
        if (ballPrefab == null || spawnPoint == null) return;

        // ★ここで必ず消費する（成功したら弾を出す）
        if (GameManager.Instance != null)
        {
            bool ok = GameManager.Instance.TryConsumeBall(1);
            if (!ok)
            {
                Debug.Log("No balls left! (shoot canceled)");
                return;
            }
        }

        if (GameSfx.Instance != null)
            GameSfx.Instance.PlayLaunch(pull01);

        var go = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // 念のため「物理で動ける状態」に戻す（吸い込み等で触った可能性がある）
        rb.isKinematic = false;
        if (rb.gravityScale <= 0f) rb.gravityScale = 3.0f; // 好みで（既に設定済みなら不要）

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Vector2 dir = shootDirection.sqrMagnitude > 0.0001f ? shootDirection.normalized : Vector2.up;

        // 初速で決める
        float speed = Mathf.Lerp(minSpeed, maxSpeed, Mathf.Clamp01(pull01));

        rb.velocity = dir * speed;

        // 少し回転も付与（見た目/音に効く）
        if (randomAngularVel > 0f)
            rb.angularVelocity = Random.Range(-randomAngularVel, randomAngularVel);
    }

    private Vector3 GetMouseWorld()
    {
        Vector3 p = Input.mousePosition;
        p.z = -cam.transform.position.z; // 2Dカメラ前提
        return cam.ScreenToWorldPoint(p);
    }
}
