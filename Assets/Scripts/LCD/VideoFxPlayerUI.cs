using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DG.Tweening;

public class VideoFxPlayerUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("RenderTexture (optional)")]
    [SerializeField] private RenderTexture renderTexture; // 指定しておくと安定

    private Tween fadeTween;
    public bool IsPlaying => videoPlayer != null && videoPlayer.isPlaying;

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            if (renderTexture != null)
                videoPlayer.targetTexture = renderTexture;

            if (rawImage != null && renderTexture != null)
                rawImage.texture = renderTexture;

            videoPlayer.loopPointReached += _ => { /* loop時は何もしない */ };
        }
    }

    public void Play(VideoClip clip, bool loop, float fadeIn = 0.15f)
    {
        if (clip == null || videoPlayer == null) return;

        // 既存停止
        videoPlayer.Stop();

        videoPlayer.clip = clip;
        videoPlayer.isLooping = loop;

        // 表示ON
        if (rawImage != null) rawImage.enabled = true;

        videoPlayer.Play();
        FadeTo(1f, fadeIn);
    }

    public void PlayNext(VideoClip nextClip, bool loop, float flashAlpha = 0.6f, float flashTime = 0.08f)
    {
        if (videoPlayer == null || nextClip == null)
            return;

        // 念のため再生停止（表示は残す）
        videoPlayer.Stop();

        // フラッシュ（白く一瞬）
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.DOFade(flashAlpha, flashTime)
                .SetLoops(2, LoopType.Yoyo)
                .SetUpdate(true);
        }

        // クリップ差し替え
        videoPlayer.clip = nextClip;
        videoPlayer.isLooping = loop;

        // 次フレームで再生（確実）
        DOVirtual.DelayedCall(0f, () =>
        {
            videoPlayer.Play();
        }).SetUpdate(true);
    }

    public void PlayFromHold(VideoClip nextClip, bool loop, float fadeIn = 0.1f)
    {
        if (videoPlayer == null || rawImage == null) return;

        // 止め絵状態から差し替え
        videoPlayer.Stop();
        videoPlayer.clip = nextClip;
        videoPlayer.isLooping = loop;
        videoPlayer.Play();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, fadeIn);
        }
    }


    public void Pause()
    {
        if (videoPlayer == null) return;

        // ★ 最後のフレームで止める（表示は維持）
        if (videoPlayer.isPlaying)
            videoPlayer.Pause();

        // ★ alpha / enabled は一切触らない
    }

    public void Hide(float fadeOut = 0.12f)
    {
        if (canvasGroup == null) return;

        canvasGroup.DOFade(0f, fadeOut)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (videoPlayer != null)
                    videoPlayer.Stop();   // 完全停止

                if (rawImage != null)
                    rawImage.enabled = false;
            });
    }

    public void ForceStop()
    {
        if (fadeTween != null && fadeTween.IsActive()) fadeTween.Kill();
        if (videoPlayer != null) videoPlayer.Stop();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (rawImage != null) rawImage.enabled = false;
    }

    private void FadeTo(float a, float duration, Action onComplete = null)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (fadeTween != null && fadeTween.IsActive()) fadeTween.Kill();

        fadeTween = canvasGroup.DOFade(a, duration)
            .SetUpdate(true)
            .OnComplete(() => onComplete?.Invoke());
    }
}
