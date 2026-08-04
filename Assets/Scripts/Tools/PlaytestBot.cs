using System.Collections.Generic;
using UnityEngine;

// Dev-only tool: plays a simple heuristic strategy (pull until the board fills up, spend
// the rest on round-robin upgrades, always buy whatever the merchant offers if affordable)
// across many auto-restarted runs, to find where an average/mediocre run tends to die.
// Not wired into any real gameplay UI — driven via script-execute during balance passes.
public class PlaytestBot : MonoBehaviour
{
    private const int TargetBoardSize = 20;
    // In-game seconds (Time.deltaTime, i.e. scaled) between bot actions. Using unscaled
    // time here was a real bug: TimerManager's Ready/Stage durations are scaled by
    // Time.timeScale, so throttling the bot by *real* time gave it proportionally fewer
    // decisions per in-game second the higher the simulation speed was pushed — the bot
    // got weaker purely from running the test faster, which isn't a fairness-neutral read.
    // 0.5s (not smaller): at high Time.timeScale, a too-frequent interval calls
    // FindObjectsByType every few real milliseconds, which tanked frame rate badly enough
    // that individual frames' scaled deltaTime got huge — large enough for enemies to
    // Vector3.MoveTowards straight past every piece's attack range in a single frame. That
    // presented as instant wave-1 deaths and wasn't a real balance signal at all.
    private const float DecisionInterval = 0.5f;

    private PieceManager pieceManager;
    private PieceUpgradeType nextUpgradeType = PieceUpgradeType.Bishop;
    private int trialsRemaining;
    private float appliedTimeScale;
    private bool running;
    private float decisionTimer;

    public List<TrialResult> Results { get; } = new List<TrialResult>();
    public bool IsRunning => running;

    public struct TrialResult
    {
        public int wave;
        public bool won;
    }

    public void RunTrials(int count, float timeScale)
    {
        pieceManager = FindFirstObjectByType<PieceManager>();
        Results.Clear();
        trialsRemaining = count;
        appliedTimeScale = timeScale;
        Time.timeScale = timeScale;
        running = true;
        GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        if (!running) return;
        if (state != GameState.GameOver && state != GameState.Victory) return;

        Results.Add(new TrialResult { wave = GameManager.Instance.CurrentWave, won = state == GameState.Victory });
        trialsRemaining--;

        if (trialsRemaining <= 0)
        {
            running = false;
            Time.timeScale = 1f;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
        else
        {
            GameManager.Instance.Restart();
        }
    }

    private void Update()
    {
        if (!running || GameManager.Instance == null) return;

        decisionTimer += Time.deltaTime;
        if (decisionTimer < DecisionInterval) return;
        decisionTimer = 0f;

        BuyFromMerchantIfOpen();
        SpendGold();
    }

    private void BuyFromMerchantIfOpen()
    {
        if (MerchantManager.Instance == null || !MerchantManager.Instance.IsShopOpen) return;

        foreach (MerchantOffer offer in MerchantManager.Instance.CurrentOffers)
        {
            if (!offer.sold && offer.price <= GameManager.Instance.Gold)
                MerchantManager.Instance.TryPurchase(offer);
        }
        MerchantManager.Instance.RequestStart();
    }

    private void SpendGold()
    {
        if (pieceManager == null) return;

        int pieceCount = FindObjectsByType<Piece>(FindObjectsSortMode.None).Length;
        if (pieceCount < TargetBoardSize && GameManager.Instance.Gold >= pieceManager.CurrentPullCost)
        {
            pieceManager.PullPiece();
            return;
        }

        if (UpgradeManager.Instance == null) return;

        // Try each family once starting from nextUpgradeType, then always rotate the
        // starting point afterward so no single family gets starved indefinitely.
        for (int i = 0; i < 3; i++)
        {
            var type = (PieceUpgradeType)(((int)nextUpgradeType + i) % 3);
            if (UpgradeManager.Instance.TryUpgrade(type))
                break;
        }
        nextUpgradeType = (PieceUpgradeType)(((int)nextUpgradeType + 1) % 3);
    }
}
