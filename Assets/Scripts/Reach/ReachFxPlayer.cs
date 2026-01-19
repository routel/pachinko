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

    public void ShowPush(Action onPressed)
    {
        if (pushButtonObj != null)
        {
            pushButtonObj.SetActive(true);
        }

            if (pushButton != null)
        {
            pushButton.onClick.RemoveAllListeners();
            pushButton.onClick.AddListener(() => onPressed?.Invoke());
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
        layer.SetVideoAboveSlot(videoAboveSlot);

        if (catalog != null && catalog.TryGet(key, out var e))
        {
            videoPlayerUI.Play(e.clip, e.loopByDefault, 0.15f);
        }
        else
        {
            Debug.LogWarning($"VideoFx not found: {key}");
        }
    }

    public void StopReachVideo()
    {
        videoPlayerUI.Stop(0.12f);
    }


}
