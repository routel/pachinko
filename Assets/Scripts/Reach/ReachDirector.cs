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
        if (player == null)
        {
            onPushed?.Invoke();
            return;
        }

        if (seq != null && seq.IsActive()) seq.Kill();
        seq = DOTween.Sequence().SetUpdate(true);

        // ① イントロ表示（リーチ文字など）
        seq.Append(player.PlayReachIntro(isWin));

        // ② 動画取得
        if (!player.TryGetVideoEntry(videoKey, out var entry))
        {
            // 動画が無いなら即PUSH
            seq.AppendCallback(() => ShowPushAndWait(onPushed));
            return;
        }

        // ③ 動画再生
        seq.AppendCallback(() =>
        {
            player.PlayReachVideo(videoKey, entry.videoAboveSlot);
        });

        // ④ 動画を見せる時間（Catalog管理）
        seq.AppendInterval(entry.pushDelay);

        // ⑤ 動画停止
        seq.AppendCallback(() =>
        {
            player.PauseReachVideo(); // ★ 止め絵を残す
        });

        // ⑥ 停止フェード待ち
        if (entry.stopFade > 0f)
            seq.AppendInterval(entry.stopFade);

        // ⑦ PUSH表示 & 待機
        seq.AppendCallback(() =>
        {
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

            player.HideReachVideo();  // フェードアウト
            if (seq != null && seq.IsActive()) seq.Kill();
            onPushed?.Invoke();
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

        if (seq != null && seq.IsActive()) seq.Kill();
        seq = DOTween.Sequence();

        seq.Append(player.PlayResult(isWin));

        seq.OnComplete(() =>
        {
            player.HideAll();
            onFinish?.Invoke();
        });
    }
}
