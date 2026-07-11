using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI livesText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI buyButtonText;

    [Header("Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    private void OnEnable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGoldChanged += UpdateGold;
        GameManager.Instance.OnLivesChanged += UpdateLives;
        GameManager.Instance.OnWaveChanged += UpdateWave;
        GameManager.Instance.OnStateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnGoldChanged -= UpdateGold;
        GameManager.Instance.OnLivesChanged -= UpdateLives;
        GameManager.Instance.OnWaveChanged -= UpdateWave;
        GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void Start()
    {
        UpdateGold(GameManager.Instance.Gold);
        UpdateLives(GameManager.Instance.Lives);
        UpdateWave(GameManager.Instance.CurrentWave);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        UpdateBuyButtonText();
        WireRestartButton();
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
        if (countdownText == null) return;
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
        var pm = FindFirstObjectByType<PieceManager>();
        if (buyButtonText != null && pm != null)
        {
            var pd = pm.GetCurrentPieceData();
            if (pd != null)
                buyButtonText.text = $"Buy {pd.pieceName} ({pd.cost}G)";
        }
    }
}
