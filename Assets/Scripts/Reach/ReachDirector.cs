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
    public void PlayReachUntilPush(bool isWin, Action onPushed)
    {
        if (player == null)
        {
            onPushed?.Invoke();
            return;
        }

        if (seq != null && seq.IsActive()) seq.Kill();
        seq = DOTween.Sequence();

        seq.Append(player.PlayReachIntro(isWin));

        if (afterIntroWait > 0f)
            seq.AppendInterval(afterIntroWait);

        seq.AppendCallback(() =>
        {
            bool pressed = false;

            player.ShowPush(() =>
            {
                if (pressed) return;
                pressed = true;
                Debug.Log("[Reach] PUSH pressed");

                // Intro側のシーケンスはここで終了させる（次へ）
                if (seq != null && seq.IsActive()) seq.Kill();
                onPushed?.Invoke();
            });

            Debug.Log("[Reach] waiting for PUSH...");
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
