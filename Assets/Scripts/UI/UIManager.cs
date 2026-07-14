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


    [Header("Panels")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("Option")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Slider efxSound;
    [SerializeField] private Slider bgSound;
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private AudioSource[] effectSoundSources;

    [Header("Selected Ally Status")]
    [SerializeField] private GameObject panelStatus;
    [SerializeField] private TextMeshProUGUI statusTextTitle;
    [SerializeField] private TextMeshProUGUI statusText;

    private PieceManager pieceManager;
    private Piece selectedPiece;
    private bool subscribed;

    private const string EffectsVolumeKey = "EffectsVolume";
    private const string BackgroundVolumeKey = "BackgroundVolume";

    private void OnEnable()
    {
        TrySubscribe();
        PieceDragHandler.OnAllyPieceSelected += ToggleSelectedPieceInfo;
        PieceDragHandler.OnAllyPieceDeselected += HideSelectedPieceInfo;
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged -= UpdateGold;
            GameManager.Instance.OnLivesChanged -= UpdateLives;
            GameManager.Instance.OnWaveChanged -= UpdateWave;
            GameManager.Instance.OnStateChanged -= OnStateChanged;
        }
        subscribed = false;
        PieceDragHandler.OnAllyPieceSelected -= ToggleSelectedPieceInfo;
        PieceDragHandler.OnAllyPieceDeselected -= HideSelectedPieceInfo;
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
        InitializeSoundControls();
        InitializeSelectedPieceStatus();

        if (pieceManager != null)
            pieceManager.OnPiecePulled += OnPiecePulled;
    }

    private void OnDestroy()
    {
        if (pieceManager != null)
            pieceManager.OnPiecePulled -= OnPiecePulled;

        if (efxSound != null)
            efxSound.onValueChanged.RemoveListener(SetEffectsVolume);
        if (bgSound != null)
            bgSound.onValueChanged.RemoveListener(SetBackgroundVolume);

        PieceDragHandler.OnAllyPieceSelected -= ToggleSelectedPieceInfo;
        PieceDragHandler.OnAllyPieceDeselected -= HideSelectedPieceInfo;
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

    private void InitializeSoundControls()
    {
        float effectsVolume = PlayerPrefs.GetFloat(EffectsVolumeKey, 1f);
        float backgroundVolume = PlayerPrefs.GetFloat(BackgroundVolumeKey, 1f);

        if (efxSound != null)
        {
            efxSound.SetValueWithoutNotify(effectsVolume);
            efxSound.onValueChanged.AddListener(SetEffectsVolume);
        }

        if (bgSound != null)
        {
            bgSound.SetValueWithoutNotify(backgroundVolume);
            bgSound.onValueChanged.AddListener(SetBackgroundVolume);
        }

        ApplyEffectsVolume(effectsVolume);
        ApplyBackgroundVolume(backgroundVolume);
    }

    public void SetEffectsVolume(float volume)
    {
        ApplyEffectsVolume(volume);
        PlayerPrefs.SetFloat(EffectsVolumeKey, volume);
        PlayerPrefs.Save();
    }

    public void SetBackgroundVolume(float volume)
    {
        ApplyBackgroundVolume(volume);
        PlayerPrefs.SetFloat(BackgroundVolumeKey, volume);
        PlayerPrefs.Save();
    }

    private void ApplyEffectsVolume(float volume)
    {
        if (effectSoundSources == null) return;

        foreach (AudioSource effectSource in effectSoundSources)
        {
            if (effectSource != null)
                effectSource.volume = volume;
        }
    }

    private void ApplyBackgroundVolume(float volume)
    {
        if (backgroundMusic != null)
            backgroundMusic.volume = volume;
    }

    private void InitializeSelectedPieceStatus()
    {
        if (panelStatus != null)
            panelStatus.SetActive(false);
    }

    private void ToggleSelectedPieceInfo(Piece piece)
    {
        if (piece == null || piece.Data == null || piece.Team != Team.Ally) return;

        if (selectedPiece == piece)
        {
            selectedPiece = null;
            HideSelectedPieceInfo();
            return;
        }

        selectedPiece = piece;

        if (panelStatus == null || statusTextTitle == null || statusText == null) return;

        PieceData data = piece.Data;
        statusTextTitle.text = data.pieceName;
        statusText.text =
            $"공격력: {data.attackDamage:0.#}\n" +
            $"사거리: {data.attackRange:0.#}\n" +
            $"공격속도: {data.attackCooldown:0.##}초";
        panelStatus.SetActive(true);
    }

    private void HideSelectedPieceInfo()
    {
        selectedPiece = null;
        if (panelStatus != null)
            panelStatus.SetActive(false);
    }

    public void OpenOptionWindow()
    {
        if (optionPanel != null)
        {
            optionPanel.SetActive(true);
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
