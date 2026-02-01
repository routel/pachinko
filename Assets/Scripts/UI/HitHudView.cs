using TMPro;
using UnityEngine;

public class HitHudView : MonoBehaviour
{
    [Header("Text (TMP)")]
    [SerializeField] private TextMeshProUGUI hitStateText;
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI inText;
    [SerializeField] private TextMeshProUGUI payoutText;
    [SerializeField] private TextMeshProUGUI ballsText; // 任意（未使用なら空でOK）
    [SerializeField] private TextMeshProUGUI holdText;
    [SerializeField] private TextMeshProUGUI busyText;
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    public void SetHitState(bool inHit)
    {
        if (hitStateText != null)
            hitStateText.text = inHit ? "HIT" : "IDLE";
    }

    public void SetRound(int current, int total)
    {
        if (roundText != null)
            roundText.text = $"ROUND: {current}/{total}";
    }

    public void SetInCount(int current, int target)
    {
        if (inText != null)
            inText.text = $"IN: {current}/{target}";
    }

    public void SetPayout(int payout)
    {
        if (payoutText != null)
            payoutText.text = $"PAYOUT: {payout}";
    }

    public void SetBalls(int balls)
    {
        if (ballsText != null)
            ballsText.text = $"BALLS: {balls}";
    }
    public void SetHold(int current, int max)
    {
        if (holdText == null) return;

        if (max > 0)
            holdText.text = $"HOLD: {current}/{max}";
        else
            holdText.text = "HOLD: -";
    }

    public void SetBusy(bool busy)
    {
        if (busyText == null) return;
        busyText.text = busy ? "BUSY" : "READY";
    }

}
