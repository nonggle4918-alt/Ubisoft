using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // UI 텍스트에 시간을 표시할 경우 필요

public class TimerManager : MonoBehaviour
{
    // 타이머의 상태 정의
    public enum TimerState { Ready, Stage, Finished }
    public TimerState currentState = TimerState.Ready;

    [Header("시간 설정 (초 단위)")]
    [SerializeField] private float readyDuration = 10f;
    [SerializeField] private float stageDuration = 40f;

    [Header("UI 연결 (선택사항)")]
    [SerializeField] private TextMeshProUGUI timerText; // 화면에 남은 시간을 보여줄 텍스트 컴포넌트

    private bool isPaused = false;

    private void Start()
    {
        // 게임이 시작되면 타이머 루프 시퀀스 시작
        StartCoroutine(TimerSequence());
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private IEnumerator TimerSequence()
    {
        // 1. 준비 시간 단계
        currentState = TimerState.Ready;
        Debug.Log("준비 시간 시작!");
        yield return StartCoroutine(CountDown(readyDuration, "Ready "));

        // 2. 스테이지 시간 단계
        currentState = TimerState.Stage;
        Debug.Log("스테이지 시간 시작!");
        yield return StartCoroutine(CountDown(stageDuration, "Stage "));

        // 3. 모든 타이머 종료
        currentState = TimerState.Finished;
        Debug.Log("모든 타이머 종료!");
        if (timerText != null) timerText.text = "종료!";

        // 여기에 타이머 종료 후 실행할 함수(예: 다음 스테이지 이동)를 넣으시면 됩니다.
    }

    // 실제로 매 초마다 숫자를 줄여주는 공용 카운트다운 함수
    private IEnumerator CountDown(float duration, string prefix)
    {
        float timeLeft = duration;

        while (timeLeft > 0)
        {
            // 콘솔창에 남은 시간 출력 (소수점 첫째 자리까지)
            // Debug.Log($"{prefix}{timeLeft:F1}s");

            // UI 텍스트가 연결되어 있다면 화면에 표시
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(timeLeft / 60f);
                int seconds = Mathf.FloorToInt(timeLeft % 60f);

                timerText.text = $"{prefix}{minutes}:{seconds:D2}";
            }

            // 다음 프레임까지 대기하며 흐른 시간만큼 차감
            timeLeft -= Time.deltaTime;
            yield return null;
        }
    }
}