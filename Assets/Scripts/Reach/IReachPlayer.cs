using System;
using DG.Tweening;

public interface IReachPlayer
{
    /// <summary>
    /// リーチ演出開始（2つ一致→ムービー/FX）
    /// </summary>
    Tween PlayReachIntro(bool isWin);

    /// <summary>
    /// “押せ”表示→入力待ち。押されたら onPressed を呼ぶ（結果は変えない）
    /// </summary>
    void ShowPush(Action onPressed);

    /// <summary>
    /// 当落演出（押した後）
    /// </summary>
    Tween PlayResult(bool isWin);

    /// <summary>
    /// リーチ中の動画を再生（keyで指定）
    /// </summary>
    void PlayReachVideo(string key, bool videoAboveSlot);
    void PauseReachVideo();
    void HideReachVideo();

    /// <summary>
    /// 後始末（UIを消す）
    /// </summary>
    void HideAll();

    bool TryGetVideoEntry(string key, out VideoFxCatalog.Entry entry);

    void PlayWinVideoFromHold(string key);

    void PlayWinLoop(string winVideoKey);

    void StopWinLoop(float fadeOut = 0.2f);

}
