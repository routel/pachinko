using UnityEngine;

public class HitHudPresenter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private HitDirector hitDirector;
    [SerializeField] private HitSpec hitSpec;       // HitDirectorに設定済みなら不要だが、確実に出す用
    [SerializeField] private HitHudView view;

    [Header("Options")]
    [SerializeField] private bool showOnlyDuringHit = false;
    [SerializeField] private float refreshInterval = 0.1f;

    [SerializeField] private LotteryManager lotteryManager;

    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    private bool visible = true;

    private float _t;

    private void Reset()
    {
        view = GetComponent<HitHudView>();
    }

    private void Awake()
    {
        if (hitDirector == null) hitDirector = HitDirector.Instance;
        if (lotteryManager == null) lotteryManager = LotteryManager.Instance;
    }

    private void OnEnable()
    {
        if (hitDirector != null)
        {
            hitDirector.OnRoundStart += HandleRoundStart;
            hitDirector.OnRoundEnd += HandleRoundEnd;
            hitDirector.OnHitEnd += HandleHitEnd;
        }
    }

    private void OnDisable()
    {
        if (hitDirector != null)
        {
            hitDirector.OnRoundStart -= HandleRoundStart;
            hitDirector.OnRoundEnd -= HandleRoundEnd;
            hitDirector.OnHitEnd -= HandleHitEnd;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            visible = !visible;

        if (view != null) view.SetVisible(visible);

        _t += Time.unscaledDeltaTime;
        if (_t < refreshInterval) return;
        _t = 0f;

        RefreshAll();
    }

    private void RefreshAll()
    {
        if (view == null || hitDirector == null) return;

        bool inHit = hitDirector.IsInHit;
        if (showOnlyDuringHit) view.SetVisible(inHit);
        else view.SetVisible(true);

        view.SetHitState(inHit);

        int totalRounds = (hitSpec != null) ? hitSpec.roundCount : 0;
        view.SetRound(hitDirector.CurrentRound, totalRounds);

        view.SetInCount(hitDirector.CurrentRoundInCount, hitDirector.TargetInThisRound);
        view.SetPayout(hitDirector.TotalPayoutThisHit);

        if (GameManager.Instance != null)
        {
             view.SetBalls(GameManager.Instance.Balls);
        }
        if (lotteryManager != null)
        {
            view.SetHold(lotteryManager.HoldCount, lotteryManager.MaxHold);
        }

        if (lotteryManager != null)
        {
            view.SetBusy(lotteryManager.IsBusy);
        }

    }

    private void HandleRoundStart(int _)
    {
        RefreshAll();
    }

    private void HandleRoundEnd(int _)
    {
        RefreshAll();
    }

    private void HandleHitEnd()
    {
        RefreshAll();
    }
}
