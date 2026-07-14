using System.Collections;
using TMPro;
using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public enum TimerState { Ready, Stage, Finished }

    [Header("Time Settings")]
    [SerializeField] private float readyDuration = 5f;
    [SerializeField] private float stageDuration = 40f;

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
        while (GameManager.Instance != null && GameManager.Instance.State != GameState.GameOver)
        {
            CurrentState = TimerState.Ready;
            yield return CountDown(readyDuration, "Ready");

            if (GameManager.Instance.State != GameState.Ready)
                continue;

            GameManager.Instance.StartWave();
            CurrentState = TimerState.Stage;

            // EnemyManager distributes this stage's DB units across this fixed duration.
            yield return CountDown(stageDuration, "Stage");

            if (GameManager.Instance.State == GameState.WaveInProgress)
                GameManager.Instance.EndWave();
        }

        CurrentState = TimerState.Finished;
    }

    private IEnumerator CountDown(float duration, string label)
    {
        float timeLeft = duration;
        while (timeLeft > 0f && GameManager.Instance != null && GameManager.Instance.State != GameState.GameOver)
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
