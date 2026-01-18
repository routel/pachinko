using System.Collections;
using UnityEngine;

public class LotteryManager : MonoBehaviour
{
    public static LotteryManager Instance { get; private set; }


    [SerializeField] private SlotMachineTweenUI slotUI;

    [Header("Lottery")]
    [Range(0f, 1f)]
    [SerializeField] private float winRate = 0.05f;


    [Header("Gates")]
    [SerializeField] private HoleTrigger[] prizeGates; // Prize穴（開閉対象）をここに登録

    [Header("Win Setting")]
    [SerializeField] private float hitTimeSec = 10f;

    [Header("Symbols")]
    [SerializeField] private int symbolCount = 6;
    [SerializeField] private int winSymbolIndex = 0; // 例：7の位置

    [Header("Win")]
    [SerializeField] private int winPercent = 15;
    [SerializeField] private float winDuration = 10f; // 当たり中（アタッカー開放時間）

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
        if (slotUI == null)
        {
            Debug.LogError("LotteryManager: slotUI not set.");
            return;
        }
        if (slotUI.IsSpinning) return;

        bool win = Random.Range(0, 100) < winPercent;

        int ToIndex(int number1to9) => Mathf.Clamp(number1to9, 1, 9) - 1;

        int winNumber = 7;
        int tL, tC, tR;

        if (win)
        {
            tL = ToIndex(winNumber);
            tC = ToIndex(winNumber);
            tR = ToIndex(winNumber);
        }
        else
        {
            int nL = Random.Range(1, 10);
            int nR = Random.Range(1, 10);
            int nC = Random.Range(1, 10);
            if (nL == nR && nR == nC) nC = (nC % 9) + 1;

            tL = ToIndex(nL);
            tR = ToIndex(nR);
            tC = ToIndex(nC);
        }

        slotUI.StartSpinWithTargets(tL, tC, tR, () =>
        {
            if (win) GameManager.Instance?.BeginHit(winDuration);
        });

    }

    private void HideSlot()
    {
        slotUI.Hide();
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
