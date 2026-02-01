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
    }

    public bool TryConsumeBall(int amount = 1)
    {
        if (Balls < amount) return false;
        Balls -= amount;
        return true;
    }

    public void AddBalls(int amount)
    {
        Balls += amount;
    }

    public void AddInCount(int add)
    {
        InCount += add;
        //Debug.Log($"Ball In! InCount = {InCount}");
    }

    public void BeginHit(float hitTimeSec)
    {
        if (IsHit) return;

        Debug.Log("[HIT] BeginHit start.");
        IsHit = true;

        HitStarted?.Invoke();

        // ★ 実際の進行は HitDirector に任せる
        HitDirector.Instance.BeginHit();
    }

    public void EndHitFromDirector()
    {
        if (!IsHit) return;

        Debug.Log("[HIT] EndHit (from HitDirector)");
        IsHit = false;

        HitEnded?.Invoke();

        // 当たりが終わったので、保留があれば消化できる
        if (LotteryManager.Instance != null)
            LotteryManager.Instance.SetBusy(false);

    }

    private IEnumerator CoHit(float hitTimeSec)
    {
        IsHit = true;
        HitStarted?.Invoke();

        yield return new WaitForSeconds(hitTimeSec);

        IsHit = false;
        HitEnded?.Invoke();

        hitCo = null;
    }


}