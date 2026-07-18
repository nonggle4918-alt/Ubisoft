using System.Collections;
using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public enum TimerState { Ready, Stage, Finished }

    [Header("Time Settings")]
    [SerializeField] private float readyDuration = 5f;
    [SerializeField] private float stageDuration = 30f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    public TimerState CurrentState { get; private set; } = TimerState.Ready;
    public float StageDuration => stageDuration;

    private EnemyManager enemyManager;
    private Coroutine gameLoop;

    private void Start()
    {
        enemyManager = FindFirstObjectByType<EnemyManager>();
        RestartTimer();
    }

    public void RestartTimer()
    {
        if (gameLoop != null)
            StopCoroutine(gameLoop);

        gameLoop = StartCoroutine(GameLoop());
    }

    private IEnumerator GameLoop()
    {
        while (GameManager.Instance != null && IsRunning())
        {
            CurrentState = TimerState.Ready;
            yield return CountDown(readyDuration, "Ready");

            if (GameManager.Instance.State != GameState.Ready)
                continue;

            GameManager.Instance.StartWave();
            CurrentState = TimerState.Stage;

            // EnemyManager distributes this stage's DB units across this fixed duration.
            yield return CountDown(stageDuration, "Stage");

            // A boss stage refuses to advance while its boss is still on the board; the
            // stage only resolves when the boss dies, or ends the run once it has used up
            // its lap allowance (Enemy.OnBossEscaped).
            yield return WaitForBoss();

            if (GameManager.Instance.State == GameState.WaveInProgress)
                GameManager.Instance.EndWave();
        }

        CurrentState = TimerState.Finished;
    }

    private IEnumerator WaitForBoss()
    {
        if (enemyManager == null || !enemyManager.StageHasBoss) yield break;

        while (IsRunning() && GameManager.Instance.State == GameState.WaveInProgress && enemyManager.IsBossAlive())
        {
            if (timerText != null)
                timerText.text = "Boss";
            yield return null;
        }
    }

    private static bool IsRunning()
    {
        GameState state = GameManager.Instance.State;
        return state != GameState.GameOver && state != GameState.Victory;
    }

    private IEnumerator CountDown(float duration, string label)
    {
        float timeLeft = duration;
        while (timeLeft > 0f && GameManager.Instance != null && IsRunning())
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(timeLeft / 60f);
                int seconds = Mathf.CeilToInt(timeLeft % 60f);
                timerText.text = $"{label} {minutes}:{seconds:D2}";
            }

            timeLeft -= Time.deltaTime;
            yield return null;
        }
    }
}
