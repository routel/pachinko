using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ReachFxPlayer : MonoBehaviour, IReachPlayer
{
    [Header("UI")]
    [SerializeField] private CanvasGroup root;    // ReachRootに付けると楽
    [SerializeField] private Image overlay;       // 黒フェード
    [SerializeField] private GameObject reachTextObj; // “リーチ！”など
    [SerializeField] private GameObject pushButtonObj; // “押せ”ボタン（Button付）
    [SerializeField] private Button pushButton;

    [Header("Tuning")]
    [SerializeField] private float fadeIn = 0.15f;
    [SerializeField] private float hold = 0.35f;
    [SerializeField] private float fadeOut = 0.15f;

    [SerializeField] private VideoFxCatalog catalog;
    [SerializeField] private VideoFxPlayerUI videoPlayerUI;
    [SerializeField] private LcdLayerController layer;

    [Header("PUSH FX")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip pushShowSe;   // 表示時
    [SerializeField] private AudioClip pushPressSe;  // ★ 押下時
    [SerializeField] private float pushBlinkAlphaMin = 0.4f;
    [SerializeField] private float pushBlinkDuration = 0.6f;
    [SerializeField] private float pushPulseScale = 1.08f;

    private Tween pushTween;

    public Tween PlayReachIntro(bool isWin)
    {
        HideAll();

        if (root != null)
        {
            root.alpha = 0f;
            root.gameObject.SetActive(true);
        }

        if (overlay != null)
            overlay.enabled = true;

        if (reachTextObj != null)
            reachTextObj.SetActive(true);

        // ※ isWin に応じてテキスト色やエフェクトを変えるならここで分岐

        Sequence s = DOTween.Sequence();

        if (root != null)
        {
            s.Append(root.DOFade(1f, fadeIn));
            s.AppendInterval(hold);
        }
        else
        {
            s.AppendInterval(fadeIn + hold);
        }

        // “押せ”は intro の後に出すので、ここでは出さない
        return s;
    }

    public bool TryGetVideoEntry(string key, out VideoFxCatalog.Entry entry)
    {
        if (catalog != null && catalog.TryGet(key, out entry))
            return true;

        entry = null;
        return false;
    }

    public void ShowPush(Action onPressed)
    {
        if (pushButtonObj != null)
            pushButtonObj.SetActive(true);

        // 表示時SE
        if (seSource != null && pushShowSe != null)
            seSource.PlayOneShot(pushShowSe);

        StartPushFx();

        if (pushButton != null)
        {
            pushButton.onClick.RemoveAllListeners();
            pushButton.onClick.AddListener(() =>
            {
                // ★ 押下時SE
                if (seSource != null && pushPressSe != null)
                    seSource.PlayOneShot(pushPressSe);

                // ★ ここが質問の答え（場所）
                if (overlay != null)
                {
                    overlay.DOKill();
                    overlay.color = new Color(0, 0, 0, 0);
                    overlay.DOFade(0.2f, 0.05f)
                           .SetLoops(2, LoopType.Yoyo);
                }

                StopPushFx();
                onPressed?.Invoke();
            });
        }
    }

    /// <summary>
    /// 点滅＋脈動 Tween
    /// </summary>
    private void StartPushFx()
    {
        StopPushFx();

        if (pushButtonObj == null) return;

        var cg = pushButtonObj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = pushButtonObj.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        pushButtonObj.transform.localScale = Vector3.one;

        Sequence s = DOTween.Sequence().SetUpdate(true);

        // 点滅（alpha）
        s.Join(
            cg.DOFade(pushBlinkAlphaMin, pushBlinkDuration)
              .SetEase(Ease.InOutSine)
              .SetLoops(-1, LoopType.Yoyo)
        );

        // 脈動（scale）
        s.Join(
            pushButtonObj.transform.DOScale(pushPulseScale, pushBlinkDuration)
              .SetEase(Ease.InOutSine)
              .SetLoops(-1, LoopType.Yoyo)
        );

        pushTween = s;
    }

    /// <summary>
    /// 停止処理（PUSH押下時／次へ進む前
    /// </summary>
    private void StopPushFx()
    {
        if (pushTween != null && pushTween.IsActive())
            pushTween.Kill();

        pushTween = null;

        if (pushButtonObj != null)
        {
            var cg = pushButtonObj.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
            pushButtonObj.transform.localScale = Vector3.one;
        }
    }


    public Tween PlayResult(bool isWin)
    {
        // 押せは消す
        if (pushButtonObj != null) pushButtonObj.SetActive(false);

        // ここで当落演出（まずは簡単にフェードアウト）
        Sequence s = DOTween.Sequence();

        // 勝ちならちょい長めに見せる、などはここで調整
        float wait = isWin ? 0.35f : 0.15f;
        s.AppendInterval(wait);

        if (root != null)
            s.Append(root.DOFade(0f, fadeOut));

        return s;
    }

    public void HideAll()
    {
        if (pushButtonObj != null) pushButtonObj.SetActive(false);
        if (reachTextObj != null) reachTextObj.SetActive(false);

        if (overlay != null) overlay.enabled = false;

        if (root != null)
        {
            root.alpha = 0f;
            root.gameObject.SetActive(false);
        }
    }

    public void PlayReachVideo(string key, bool videoAboveSlot)
    {
        Debug.Log($"[ReachFxPlayer] PlayReachVideo key={key}, aboveSlot={videoAboveSlot}");

        layer.SetVideoAboveSlot(videoAboveSlot);

        if (catalog != null && catalog.TryGet(key, out var e))
        {
            Debug.Log($"[ReachFxPlayer] VideoClip found. loop={e.loopByDefault}");
            videoPlayerUI.Play(e.clip, e.loopByDefault, 0.15f);
        }
        else
        {
            Debug.LogWarning($"[ReachFxPlayer] VideoFx not found: {key}");
        }
    }

    public void PauseReachVideo()
    {
        Debug.Log("[ReachFxPlayer] PauseReachVideo");
        videoPlayerUI?.Pause();
    }

    /// <summary>
    /// 「止め絵を残したまま」ではなく、消す（フェードアウトして停止）
    /// </summary>
    /// <param name="fadeOut"></param>
    public void HideReachVideo(float fadeOut = 0.12f)
    {
        Debug.Log($"[ReachFxPlayer] HideReachVideo fadeOut={fadeOut:0.00}");
        videoPlayerUI?.Hide(fadeOut); // ★ Stopではなく Hide
    }

    public void StopReachVideo()
    {
        Debug.Log("[ReachFxPlayer] StopReachVideo -> Hide(0.12)");
        videoPlayerUI?.Hide(0.12f);
    }

    public void PlayWinVideoFromHold(string key)
    {
        if (catalog == null || videoPlayerUI == null)
            return;

        if (!catalog.TryGet(key, out var e) || e.clip == null)
        {
            Debug.LogWarning($"Win video not found: {key}");
            return;
        }

        // ★ 当たりは前面に出す（スマパチ王道）
        if (layer != null)
            layer.SetVideoAboveSlot(true); // 当たりは前面に出すのが王道

        // ★ 止め絵 → 自然に当たり動画
        videoPlayerUI.PlayFromHold(
            nextClip: e.clip,
            loop: e.loopByDefault,
            fadeIn: 0.08f
        );
    }

    /// <summary>
    /// 当たり動画開始（ループ）
    /// </summary>
    /// <param name="key"></param>
    public void PlayWinLoop(string key)
    {
        Debug.Log($"[ReachFxPlayer] PlayWinLoop key={key}");

        if (catalog == null)
        {
            Debug.LogError("[ReachFxPlayer] catalog is null");
            return;
        }
        if (videoPlayerUI == null)
        {
            Debug.LogError("[ReachFxPlayer] videoPlayerUI is null");
            return;
        }
        if (layer == null)
        {
            Debug.LogWarning("[ReachFxPlayer] layer is null (layer control skipped)");
        }

        if (!catalog.TryGet(key, out var e) || e == null || e.clip == null)
        {
            Debug.LogError($"[ReachFxPlayer] Catalog missing or clip null for key={key}");
            return;
        }

        Debug.Log($"[ReachFxPlayer] -> clip={e.clip.name} loop=true aboveSlot={e.videoAboveSlot}");

        if (layer != null) layer.SetVideoAboveSlot(e.videoAboveSlot);

        videoPlayerUI.PlayFromHold(e.clip, loop: true, fadeIn: 0.1f);
    }

    /// <summary>
    /// 賞球終了時の停止
    /// </summary>
    public void StopWinLoop(float fadeOut = 0.2f)
    {
        Debug.Log($"[ReachFxPlayer] StopWinLoop fadeOut={fadeOut:0.00}");

        if (videoPlayerUI == null)
        {
            Debug.LogError("[ReachFxPlayer] videoPlayerUI is null");
            return;
        }

        videoPlayerUI.Hide(fadeOut);
    }

    public void HideReachVideo()
    {
        Debug.Log("[ReachFxPlayer] HideReachVideo -> videoPlayerUI.Hide(0.12)");
        videoPlayerUI?.Hide(0.12f);
    }
    /// <summary>
    /// いま出ている動画を全部片付ける（状態に依存せず安全に呼べる）
    /// </summary>
    public void StopAllVideos(float fadeOut = 0.2f)
    {
        // Reach/Win どちらの経路でも最終的に Hide で消える想定
        if (videoPlayerUI != null)
            videoPlayerUI.Hide(fadeOut);
    }

}
