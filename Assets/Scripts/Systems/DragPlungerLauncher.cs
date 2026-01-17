using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragPlungerLauncher : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject ballPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Plunger")]
    [SerializeField] private float maxPullDistance = 2.0f;
    [SerializeField] private float power = 12f;
    [SerializeField] private Vector2 shootDirection = Vector2.up;

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
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragStartWorld = GetMouseWorld();
            pull01 = 0f;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            Vector3 now = GetMouseWorld();

            float pull = Mathf.Max(0f, (dragStartWorld.y - now.y)); // 下に引いた分だけ
            pull = Mathf.Min(pull, maxPullDistance);

            pull01 = pull / maxPullDistance;
        }

        if (isDragging && Input.GetMouseButtonUp(0))
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
            Debug.Log($"TryConsumeBall -> {ok}, BallsNow={GameManager.Instance.Balls}");

            if (!ok)
            {
                Debug.Log("No balls left! (shoot canceled)");
                return;
            }
        }

        var go = Instantiate(ballPrefab, spawnPoint.position, Quaternion.identity);

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;

        Vector2 dir = shootDirection.normalized;
        float force = power * Mathf.Lerp(0.2f, 1.0f, pull01);

        rb.AddForce(dir * force, ForceMode2D.Impulse);
        Debug.Log($"Shoot! pull={pull01:0.00} force={force:0.0}");
    }

    private Vector3 GetMouseWorld()
    {
        Vector3 p = Input.mousePosition;
        p.z = -cam.transform.position.z; // 2Dカメラ前提
        return cam.ScreenToWorldPoint(p);
    }
}
