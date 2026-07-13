using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [SerializeField] private TextMeshProUGUI pullResultText;
    [SerializeField] private GameObject optionPanel;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    private PieceManager pieceManager;
    private bool subscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGoldChanged += UpdateGold;
        GameManager.Instance.OnLivesChanged += UpdateLives;
        GameManager.Instance.OnWaveChanged += UpdateWave;
        GameManager.Instance.OnStateChanged += OnStateChanged;
        subscribed = true;
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGoldChanged -= UpdateGold;
        GameManager.Instance.OnLivesChanged -= UpdateLives;
        GameManager.Instance.OnWaveChanged -= UpdateWave;
        GameManager.Instance.OnStateChanged -= OnStateChanged;
        subscribed = false;
    }

    private void Start()
    {
        TrySubscribe();
        pieceManager = FindFirstObjectByType<PieceManager>();
        UpdateGold(GameManager.Instance.Gold);
        UpdateLives(GameManager.Instance.Lives);
        UpdateWave(GameManager.Instance.CurrentWave);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(false);
        UpdateBuyButtonText();
        WireRestartButton();

        if (pieceManager != null)
            pieceManager.OnPiecePulled += OnPiecePulled;
    }

    private void OnDestroy()
    {
        if (pieceManager != null)
            pieceManager.OnPiecePulled -= OnPiecePulled;
    }

    private void WireRestartButton()
    {
        if (gameOverPanel == null) return;
        var btn = gameOverPanel.GetComponentInChildren<UnityEngine.UI.Button>();
        if (btn != null)
            btn.onClick.AddListener(Restart);
    }

    public void Restart()
    {
        GameManager.Instance.Restart();
    }

    private void Update()
    {
        if (countdownText == null || GameManager.Instance == null) return;
        if (GameManager.Instance.State == GameState.Ready)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = $"Wave {GameManager.Instance.CurrentWave} starts soon...";
        }
        else
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void UpdateGold(int gold) { if (goldText != null) goldText.text = $"Gold: {gold}"; }
    private void UpdateLives(int lives) { if (livesText != null) livesText.text = $"Lives: {lives}"; }
    private void UpdateWave(int wave) { if (waveText != null) waveText.text = $"Wave: {wave}"; }

    private void OnStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
        }
        else if (state == GameState.Victory)
        {
            if (victoryPanel != null) victoryPanel.SetActive(true);
        }
    }

    private void UpdateBuyButtonText()
    {
        if (buyButtonText != null)
            buyButtonText.text = "Pull (50G)";
    }

    private void OnPiecePulled(PieceData data)
    {
        if (pullResultText != null)
        {
            pullResultText.gameObject.SetActive(true);
            pullResultText.text = $"Got {data.pieceName}!";
            CancelInvoke(nameof(HidePullResult));
            Invoke(nameof(HidePullResult), 2f);
        }
    }

    private void HidePullResult()
    {
        if (pullResultText != null)
            pullResultText.gameObject.SetActive(false);
    }
    public void OpenOptionWindow()
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(true);
            Debug.Log("button!!");
            Time.timeScale = 0f;
        }
    }

    public void CloseOptionWindow()
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(false);
            Time.timeScale = 1f; 
        }
    }
}