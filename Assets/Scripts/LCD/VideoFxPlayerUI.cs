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
    [SerializeField] private RenderTexture renderTexture; // Žw’è‚µ‚Ä‚¨‚­‚ÆˆÀ’è

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

            videoPlayer.loopPointReached += _ => { /* loopŽž‚Í‰½‚à‚µ‚È‚¢ */ };
        }
    }

    public void Play(VideoClip clip, bool loop, float fadeIn = 0.15f)
    {
        if (clip == null || videoPlayer == null) return;

        // Šù‘¶’âŽ~
        videoPlayer.Stop();

        videoPlayer.clip = clip;
        videoPlayer.isLooping = loop;

        // •\Ž¦ON
        if (rawImage != null) rawImage.enabled = true;

        videoPlayer.Play();
        FadeTo(1f, fadeIn);
    }

    public void Pause()
    {
        if (videoPlayer == null) return;

        // š ÅŒã‚ÌƒtƒŒ[ƒ€‚ÅŽ~‚ß‚éi•\Ž¦‚ÍˆÛŽj
        if (videoPlayer.isPlaying)
            videoPlayer.Pause();

        // š alpha / enabled ‚ÍˆêØG‚ç‚È‚¢
    }

    public void Hide(float fadeOut = 0.12f)
    {
        if (canvasGroup == null) return;

        canvasGroup.DOFade(0f, fadeOut)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (videoPlayer != null)
                    videoPlayer.Stop();   // Š®‘S’âŽ~

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
