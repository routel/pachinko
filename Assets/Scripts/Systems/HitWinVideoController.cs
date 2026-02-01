using UnityEngine;

public class HitWinVideoController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HitDirector hitDirector;
    [SerializeField] private ReachFxPlayer reachFx;

    [Header("Catalog Key")]
    [SerializeField] private string winLoopKey = "win_loop"; // VideoFxCatalogのキーに合わせる

    [Header("Behavior")]
    [SerializeField] private bool restartEveryRound = true;
    [SerializeField] private float fadeOutOnEnd = 0.2f;

    private void Awake()
    {
        if (hitDirector == null) hitDirector = HitDirector.Instance;
    }

    private void OnEnable()
    {
        if (hitDirector == null) return;
        hitDirector.OnRoundStart += OnRoundStart;
        hitDirector.OnHitEnd += OnHitEnd;
    }

    private void OnDisable()
    {
        if (hitDirector == null) return;
        hitDirector.OnRoundStart -= OnRoundStart;
        hitDirector.OnHitEnd -= OnHitEnd;
    }

    private void OnRoundStart(int round)
    {
        if (reachFx == null) return;

        // 重要：当たり中の動画が終わってしまう問題を潰す
        // → loop=trueで再生し続ける（PlayWinLoopは内部で loop=true）
        if (restartEveryRound || round == 1)
        {
            reachFx.PlayWinLoop(winLoopKey);
        }
    }

    private void OnHitEnd()
    {
        if (reachFx == null) return;

        // 当たり終了後に止まった絵が残る問題を潰す
        reachFx.StopAllVideos(fadeOutOnEnd);
    }
}
