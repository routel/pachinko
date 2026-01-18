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
    /// 後始末（UIを消す）
    /// </summary>
    void HideAll();
}
