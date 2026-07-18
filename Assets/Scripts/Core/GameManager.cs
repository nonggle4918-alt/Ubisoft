using System;
using UnityEngine;

public enum GameState
{
    Ready,
    WaveInProgress,
    GameOver,
    Victory
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

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

    public void AddGold(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

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

    public void EndWave()
    {
        if (IsFinalStage)
        {
            SetState(GameState.Victory);
            return;
        }

        CurrentWave++;
        OnWaveChanged?.Invoke(CurrentWave);
        SetState(GameState.Ready);
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

        Gold = startGold;
        Lives = startLives;
        CurrentWave = 1;
        OnGoldChanged?.Invoke(Gold);
        OnLivesChanged?.Invoke(Lives);
        OnWaveChanged?.Invoke(CurrentWave);
        SetState(GameState.Ready);

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.ResetLevels();

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
