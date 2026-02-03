using UnityEngine;

public class GameSfx : MonoBehaviour
{
    public static GameSfx Instance { get; private set; }

    [Header("Audio Source")]
    [SerializeField] private AudioSource seSource;

    [Header("Clips")]
    [SerializeField] private AudioClip launchSe;      // 発射
    [SerializeField] private AudioClip payoutJaraSe;  // ジャラジャラ（払い出し）

    [Header("Tuning")]
    [SerializeField] private Vector2 launchVolumeRange = new Vector2(0.25f, 0.7f); // pull01で変える
    [SerializeField] private float payoutVolume = 0.85f;
    [SerializeField] private float payoutCooldown = 0.08f; // 多重鳴り防止

    private float _lastPayoutTime = -999f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PlayLaunch(float pull01)
    {
        if (seSource == null || launchSe == null) return;
        float v = Mathf.Lerp(launchVolumeRange.x, launchVolumeRange.y, Mathf.Clamp01(pull01));
        seSource.PlayOneShot(launchSe, v);
    }

    /// <summary>賞球（払い出し）音。短時間に何度も呼ばれても1回に抑える。</summary>
    public void PlayPayoutJara()
    {
        if (seSource == null || payoutJaraSe == null) return;

        float now = Time.unscaledTime;
        if (now - _lastPayoutTime < payoutCooldown) return;
        _lastPayoutTime = now;

        seSource.PlayOneShot(payoutJaraSe, payoutVolume);
    }
}
