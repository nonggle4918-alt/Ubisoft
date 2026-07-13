using UnityEngine;
using UnityEngine.UI; // UI Canvas(Button 등)를 사용할 경우 필요

public class UiManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject optionPanel; // 게임 옵션창 오브젝트
    [SerializeField] private Button openButton;      // 옵션창을 열 버튼
    [SerializeField] private Button closeButton;     // 옵션창을 닫을 버튼

    private void Awake()
    {
        // 1. 게임 시작 시 옵션창은 기본적으로 숨김 처리
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
        }

        /* 2. 버튼 클릭 리스너(이벤트) 연결
        if (openButton != null)
        {
            openButton.onClick.AddListener(OpenOptionWindow);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseOptionWindow);
        }*/
    }

    // 옵션창 띄우기 함수
    public void OpenOptionWindow()
    {  
        if (optionPanel != null)
        {
            optionPanel.SetActive(true);
            Debug.Log("button!!");
            // 필요 시 옵션창이 열렸을 때 게임을 일시정지하고 싶다면 아래 주석을 해제하세요.
            Time.timeScale = 0f; 
        }
    }

    // 옵션창 닫기 함수
    public void CloseOptionWindow()
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);

            // 일시정지를 해제하고 게임을 다시 진행합니다.
            // Time.timeScale = 1f; 
        }
    }
}
