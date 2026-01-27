using System;
using UnityEngine;
using DG.Tweening;

public class ReachDirector : MonoBehaviour
{
    public static ReachDirector Instance { get; private set; }

    [Header("Player (FX or Movie)")]
    [SerializeField] private MonoBehaviour reachPlayerBehaviour;
    private IReachPlayer player;

    [Header("Timings")]
    [SerializeField] private float afterIntroWait = 0.05f;

    private Sequence seq;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        player = reachPlayerBehaviour as IReachPlayer;
        if (player == null)
            Debug.LogError("ReachDirector: reachPlayerBehaviour must implement IReachPlayer.");

        player?.HideAll();
    }

    /// <summary>
    /// リーチ導入→PUSH待ち。押されたら onPushed を呼ぶ（ここでは結果演出しない）
    /// </summary>
    public void PlayReachUntilPush(
        bool isWin,
        Action onPushed,
        string videoKey
    )
    {
        Debug.Log($"[ReachDirector] PlayReachUntilPush start. isWin={isWin}, videoKey={videoKey}");

        if (player == null)
        {
            Debug.LogWarning("[ReachDirector] player is null -> resume immediately");
            onPushed?.Invoke();
            return;
        }

        if (seq != null && seq.IsActive()) seq.Kill();
        seq = DOTween.Sequence().SetUpdate(true);

        // ① イントロ
        Debug.Log("[ReachDirector] Intro start");
        seq.Append(player.PlayReachIntro(isWin));

        // ② 動画取得
        if (!player.TryGetVideoEntry(videoKey, out var entry))
        {
            Debug.LogWarning($"[ReachDirector] Video not found: {videoKey}");
            seq.AppendCallback(() => ShowPushAndWait(onPushed));
            return;
        }

        // ③ 動画再生
        seq.AppendCallback(() =>
        {
            Debug.Log($"[ReachDirector] PlayReachVideo key={videoKey}");
            player.PlayReachVideo(videoKey, entry.videoAboveSlot);
        });

        // ④ 再生待ち
        Debug.Log($"[ReachDirector] Wait pushDelay={entry.pushDelay}");
        seq.AppendInterval(entry.pushDelay);

        // ⑤ 動画停止（止め絵）
        seq.AppendCallback(() =>
        {
            Debug.Log("[ReachDirector] PauseReachVideo (hold frame)");
            player.PauseReachVideo();
        });

        if (entry.stopFade > 0f)
            seq.AppendInterval(entry.stopFade);

        // ⑥ PUSH表示
        seq.AppendCallback(() =>
        {
            Debug.Log("[ReachDirector] Show PUSH");
            ShowPushAndWait(onPushed);
        });
    }

    private void ShowPushAndWait(Action onPushed)
    {
        bool pressed = false;

        // ★ ここで必ずUIを生かす
        player.ShowPush(() =>
        {
            if (pressed) return;
            pressed = true;

            // ★ ReachDirector では動画に触らない
            if (seq != null && seq.IsActive()) seq.Kill();

            onPushed?.Invoke(); // 「PUSHされた」だけ通知
        });
    }

    /// <summary>
    /// 結果演出だけ（中央停止の後に呼ぶ）
    /// </summary>
    public void PlayResultOnly(bool isWin, Action onFinish)
    {
        if (player == null)
        {
            onFinish?.Invoke();
            return;
        }

        // ★ 当たりなら、止め絵 → 当たり動画
        if (isWin)
        {
            player.PlayWinVideoFromHold("result.win");
        }
        else
        {
            player.HideReachVideo();
        }

        if (seq != null && seq.IsActive()) seq.Kill();
        seq = DOTween.Sequence();

        seq.Append(player.PlayResult(isWin));

        seq.OnComplete(() =>
        {
            player.HideAll();
            onFinish?.Invoke();
        });
    }

    public void StartWinLoop()
    {
        player?.PlayWinLoop("result.win");
    }

    public void EndWinLoop()
    {
        player?.StopWinLoop();
    }

    private void OnEnable()
    {
        Debug.Log("[ReachDirector] OnEnable subscribe HitStarted/HitEnded");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HitStarted += OnHitStarted;
            GameManager.Instance.HitEnded += OnHitEnded;
        }
        else
        {
            Debug.LogWarning("[ReachDirector] GameManager.Instance is null on OnEnable");
        }
    }

    private void OnDisable()
    {
        Debug.Log("[ReachDirector] OnDisable unsubscribe HitStarted/HitEnded");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HitStarted -= OnHitStarted;
            GameManager.Instance.HitEnded -= OnHitEnded;
        }
    }

    private void OnHitStarted()
    {
        Debug.Log("[ReachDirector] OnHitStarted -> PlayWinLoop(result.win)");
        player?.PlayWinLoop("result.win");
    }

    private void OnHitEnded()
    {
        Debug.Log("[ReachDirector] OnHitEnded -> StopWinLoop(0.2)");
        player?.StopWinLoop(0.2f);
    }


}
