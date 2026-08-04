using System;
using UnityEngine;

public enum GameState
{
    Ready,
    WaveInProgress,
    GameOver,
    Victory
}

public enum GameMode
{
    Normal,
    Infinite
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Set by ScencsChane before loading InGameScens; consumed once in Awake.
    public static GameMode RequestedMode = GameMode.Normal;

    [Header("Game Settings")]
    [SerializeField] private int startGold = 100;
    [SerializeField] private int startLives = 20;
    [SerializeField] private float firstWaveDelay = 3f;
    [SerializeField] private float betweenWaveDelay = 5f;

    public int Gold { get; private set; }
    public int Lives { get; private set; }
    public int CurrentWave { get; private set; } = 1;
    public GameState State { get; private set; } = GameState.Ready;
    public GameDatabase Database { get; private set; }
    public GameMode Mode { get; private set; } = GameMode.Normal;

    public event Action<int> OnGoldChanged;
    public event Action<int> OnLivesChanged;
    public event Action<int> OnWaveChanged;
    public event Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Database = GameDatabase.Load();
        Mode = RequestedMode;
        Gold = startGold;
        Lives = startLives;
    }

    private void OnEnable() => Enemy.OnBossEscaped += TriggerBossEscapeGameOver;
    private void OnDisable() => Enemy.OnBossEscaped -= TriggerBossEscapeGameOver;

    public void StartWave()
    {
        if (State != GameState.Ready) return;
        SetState(GameState.WaveInProgress);
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    // Plain gold gain — no relic multiplier. Used by selling pieces and anything else that
    // isn't a combat reward; the gold-gain relic is specifically about kill rewards.
    public void AddGold(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    // Enemy kill rewards only — this is the one path the gold-gain relic amplifies.
    public void AddKillGold(int amount)
    {
        float relicMult = RelicManager.Instance != null ? RelicManager.Instance.GoldMultiplier : 1f;
        Gold += Mathf.RoundToInt(amount * relicMult);
        OnGoldChanged?.Invoke(Gold);
    }

    // Returns exactly what was spent on a failed purchase — plain AddGold already skips
    // the relic multiplier, but this name makes the refund intent explicit at call sites.
    public void RefundGold(int amount) => AddGold(amount);

    public void LoseLife(int amount = 1)
    {
        Lives -= amount;
        OnLivesChanged?.Invoke(Lives);
        if (Lives <= 0)
            SetState(GameState.GameOver);
    }

    // Clearing this stage finishes the run.
    public const int FinalStage = 75;

    public bool IsFinalStage => CurrentWave >= FinalStage;

    // Wave 20, 30, 40, ... — read at the Ready-countdown point in TimerManager.GameLoop,
    // where CurrentWave already means "the wave about to start" (EndWave increments it
    // before returning to Ready).
    public const int MerchantStartWave = 20;
    public const int MerchantInterval = 10;
    public bool IsMerchantWave => CurrentWave >= MerchantStartWave && (CurrentWave - MerchantStartWave) % MerchantInterval == 0;

    public void EndWave()
    {
        if (Mode == GameMode.Normal && IsFinalStage)
        {
            SetState(GameState.Victory);
            return;
        }

        CurrentWave++;
        OnWaveChanged?.Invoke(CurrentWave);
        SetState(GameState.Ready);
    }

    // Called from the Victory panel's "continue" button to keep playing past the final stage.
    // TimerManager's GameLoop coroutine already exited when State became Victory (see
    // TimerManager.IsRunning), so it must be restarted explicitly, same as Restart() does.
    public void ContinueToInfinite()
    {
        Mode = GameMode.Infinite;
        CurrentWave++;
        OnWaveChanged?.Invoke(CurrentWave);
        SetState(GameState.Ready);

        var timerManager = FindFirstObjectByType<TimerManager>();
        if (timerManager != null)
            timerManager.RestartTimer();
    }

    // A boss that survives its lap allowance ends the run regardless of remaining lives.
    public void TriggerBossEscapeGameOver()
    {
        if (State == GameState.GameOver || State == GameState.Victory) return;
        SetState(GameState.GameOver);
    }

    public void Restart()
    {
        CancelInvoke();

        var enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var e in enemies) Destroy(e.gameObject);

        var pieces = FindObjectsByType<Piece>(FindObjectsSortMode.None);
        foreach (var p in pieces) Destroy(p.gameObject);

        var gm = FindFirstObjectByType<GridManager>();
        if (gm != null) gm.ClearGrid();

        Mode = RequestedMode;
        Gold = startGold;
        Lives = startLives;
        CurrentWave = 1;
        OnGoldChanged?.Invoke(Gold);
        OnLivesChanged?.Invoke(Lives);
        OnWaveChanged?.Invoke(CurrentWave);
        SetState(GameState.Ready);

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.ResetLevels();

        if (RelicManager.Instance != null)
            RelicManager.Instance.ResetRelics();

        MerchantManager.Instance?.CloseShop();

        var timerManager = FindFirstObjectByType<TimerManager>();
        if (timerManager != null)
            timerManager.RestartTimer();
    }

    private void SetState(GameState newState)
    {
        State = newState;
        OnStateChanged?.Invoke(newState);
    }
}
