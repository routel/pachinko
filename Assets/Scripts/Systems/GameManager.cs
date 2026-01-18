using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI (optional)")]
    [SerializeField] private TMP_Text inCounterText;   // IN: ‚Ì•\Ž¦
    [SerializeField] private TMP_Text ballsText;       // BALLS: ‚Ì•\Ž¦

    [Header("Game Values")]
    [SerializeField] private int startBalls = 100;

    public int InCount { get; private set; }
    public int Balls { get; private set; }
    public bool IsHit { get; private set; }


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

    private void RefreshUI()
    {
        if (inCounterText != null) inCounterText.text = $"IN: {InCount}";
        if (ballsText != null) ballsText.text = $"BALLS: {Balls}";
    }

    public void BeginHit(float hitTimeSec)
    {
        if (IsHit) return;
        StartCoroutine(CoHit(hitTimeSec));
    }

    private IEnumerator CoHit(float hitTimeSec)
    {
        IsHit = true;
        RefreshUI(); // ‚ ‚é‚È‚ç
        yield return new WaitForSeconds(hitTimeSec);
        IsHit = false;
        RefreshUI(); // ‚ ‚é‚È‚ç
    }


}