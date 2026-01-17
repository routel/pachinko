using System.Collections;
using UnityEngine;

public class LotteryManager : MonoBehaviour
{
    public static LotteryManager Instance { get; private set; }

    [Header("Lottery")]
    [Range(0f, 1f)]
    [SerializeField] private float winRate = 0.05f;

    [Header("Win State")]
    [SerializeField] private float winDuration = 8.0f; // 当たり中（アタッカー開放時間）

    [Header("Gates")]
    [SerializeField] private HoleTrigger[] prizeGates; // Prize穴（開閉対象）をここに登録

    public bool IsWin { get; private set; }

    private Coroutine winRoutine;

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
        // 起動時は閉じる
        SetPrizeGates(false);
        IsWin = false;
    }

    public void DoLottery()
    {
        bool win = (Random.value < winRate);
        Debug.Log(win ? "LOTTERY: WIN" : "LOTTERY: LOSE");

        if (win)
            StartWin();
    }

    private void StartWin()
    {
        // すでに当たり中なら延長（リセット）する
        if (winRoutine != null)
            StopCoroutine(winRoutine);

        winRoutine = StartCoroutine(WinSequence());
    }

    private IEnumerator WinSequence()
    {
        IsWin = true;

        // ★ WIN開始と同時に IsHit をON（表示条件に使っているならここ）
        GameManager.Instance.BeginHit(winDuration);

        SetPrizeGates(true);
        Debug.Log("WIN STATE: START (gates open)");

        yield return new WaitForSeconds(winDuration);

        SetPrizeGates(false);
        IsWin = false;
        winRoutine = null;
        Debug.Log("WIN STATE: END (gates closed)");
    }


    private void SetPrizeGates(bool open)
    {
        if (prizeGates == null) return;

        foreach (var g in prizeGates)
        {
            if (g != null) g.SetGateOpen(open);
        }
    }
}
