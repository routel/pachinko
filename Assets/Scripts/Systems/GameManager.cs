using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action HitStarted;
    public event Action HitEnded;

    [Header("UI (optional)")]
    [SerializeField] private TMP_Text inCounterText;   // IN: ‚Ì•\Ž¦
    [SerializeField] private TMP_Text ballsText;       // BALLS: ‚Ì•\Ž¦

    [Header("Game Values")]
    [SerializeField] private int startBalls = 100;

    public int InCount { get; private set; }
    public int Balls { get; private set; }
    public bool IsHit { get; private set; }


    private Coroutine hitCo;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Balls = startBalls;
        RefreshUI();
    }

    public bool TryConsumeBall(int amount = 1)
    {
        if (Balls < amount) return false;
        Balls -= amount;
        RefreshUI();
        return true;
    }

    public void AddBalls(int amount)
    {
        Balls += amount;
        RefreshUI();
    }

    public void AddInCount(int add)
    {
        InCount += add;
        RefreshUI();
        //Debug.Log($"Ball In! InCount = {InCount}");
    }

    public void BeginHit(float hitTimeSec)
    {
        if (IsHit) return;

        Debug.Log($"[HIT] BeginHit start. duration={hitTimeSec:0.00}s");
        IsHit = true;
        RefreshUI();

        HitStarted?.Invoke();

        DOVirtual.DelayedCall(hitTimeSec, () =>
        {
            Debug.Log("[HIT] BeginHit end.");
            IsHit = false;
            RefreshUI();
            HitEnded?.Invoke();
        }).SetUpdate(true);
    }

    private IEnumerator CoHit(float hitTimeSec)
    {
        IsHit = true;
        RefreshUI();
        HitStarted?.Invoke();

        yield return new WaitForSeconds(hitTimeSec);

        IsHit = false;
        RefreshUI();
        HitEnded?.Invoke();

        hitCo = null;
    }
    private void RefreshUI()
    {
        if (inCounterText != null) inCounterText.text = $"IN: {InCount}";
        if (ballsText != null) ballsText.text = $"BALLS: {Balls}";
    }


}