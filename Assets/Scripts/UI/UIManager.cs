using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private TextMeshProUGUI statusTierText;
    private Button statusSellButton;
    private TextMeshProUGUI statusSellText;
    private bool subscribed;
    private bool upgradeSubscribed;

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
        if (!subscribed && GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged += UpdateGold;
            GameManager.Instance.OnLivesChanged += UpdateLives;
            GameManager.Instance.OnWaveChanged += UpdateWave;
            GameManager.Instance.OnStateChanged += OnStateChanged;
            subscribed = true;
        }

        if (!upgradeSubscribed && UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.OnUpgradeChanged += OnUpgradeChanged;
            upgradeSubscribed = true;
        }
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

        if (upgradeSubscribed && UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradeChanged -= OnUpgradeChanged;
        upgradeSubscribed = false;

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
        WireGameOverButtons();
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

    private void WireGameOverButtons()
    {
        if (gameOverPanel == null) return;

        Button restartButton = FindButtonByName(gameOverPanel, "Button_OK");
        if (restartButton == null) return;

        // Content_Demo (the button container) ships inactive in the scene; the popup
        // relies on it being switched on once its buttons are wired (see UpgradeUI's
        // identical Content_Demo/Button_OK setup for the same convention).
        restartButton.transform.parent.gameObject.SetActive(true);
        restartButton.onClick.AddListener(Restart);

        Button returnTitleButton = FindButtonByName(gameOverPanel, "Button_ReturnTitle");
        if (returnTitleButton != null)
            returnTitleButton.onClick.AddListener(ReturnToTitle);
    }

    private static Button FindButtonByName(GameObject root, string buttonName)
    {
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.name == buttonName)
                return button;
        }

        return null;
    }

    public void Restart()
    {
        GameManager.Instance.Restart();
    }

    public void ReturnToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StandbyScenes");
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
        if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
        if (victoryPanel != null) victoryPanel.SetActive(state == GameState.Victory);
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
        CreateStatusTierText();
        CreateStatusSellButton();

        if (panelStatus != null)
            panelStatus.SetActive(false);
    }

    private void CreateStatusTierText()
    {
        if (statusTierText != null || panelStatus == null || statusTextTitle == null) return;

        statusTierText = Instantiate(statusTextTitle, statusTextTitle.transform.parent);
        statusTierText.name = "StatusTierText";
        statusTierText.alignment = TextAlignmentOptions.TopRight;
        statusTierText.fontSize = statusTextTitle.fontSize;
        statusTierText.raycastTarget = false;

        var titleTransform = statusTextTitle.rectTransform;
        var tierTransform = statusTierText.rectTransform;
        tierTransform.anchorMin = new Vector2(1f, titleTransform.anchorMin.y);
        tierTransform.anchorMax = new Vector2(1f, titleTransform.anchorMax.y);
        tierTransform.pivot = new Vector2(1f, titleTransform.pivot.y);
        tierTransform.anchoredPosition = new Vector2(-24f, titleTransform.anchoredPosition.y);
        tierTransform.sizeDelta = new Vector2(180f, titleTransform.sizeDelta.y);
        tierTransform.SetAsLastSibling();
    }

    private void CreateStatusSellButton()
    {
        if (statusSellButton != null || panelStatus == null || statusTextTitle == null) return;

        // Borrow the panel's own sprite so the button matches the project's UI kit.
        // Resources.GetBuiltinResource("UI/Skin/UISprite.psd") is not available in this Unity
        // version — it logs an error and returns null, leaving the button without any art.
        Sprite panelSprite = FindPanelSprite();

        var sellObject = new GameObject("StatusSellButton", typeof(RectTransform), typeof(Image), typeof(Button));
        sellObject.transform.SetParent(panelStatus.transform, false);

        var sellRect = sellObject.GetComponent<RectTransform>();
        sellRect.anchorMin = new Vector2(0.5f, 0f);
        sellRect.anchorMax = new Vector2(0.5f, 0f);
        sellRect.pivot = new Vector2(0.5f, 0f);
        sellRect.anchoredPosition = new Vector2(0f, 18f);
        sellRect.sizeDelta = new Vector2(230f, 44f);

        Image background = sellObject.GetComponent<Image>();
        if (panelSprite != null)
        {
            background.sprite = panelSprite;
            background.type = Image.Type.Sliced;
        }
        background.color = new Color(0.65f, 0.2f, 0.18f, 0.95f);

        statusSellButton = sellObject.GetComponent<Button>();
        statusSellButton.targetGraphic = background;
        statusSellButton.onClick.AddListener(SellSelectedPiece);
        SFXManager.Instance?.BindButtonClickSound(statusSellButton);

        statusSellText = Instantiate(statusTextTitle, sellObject.transform);
        statusSellText.name = "StatusSellText";
        statusSellText.alignment = TextAlignmentOptions.Center;
        statusSellText.fontSize = statusTextTitle.fontSize;
        statusSellText.color = Color.white;
        statusSellText.raycastTarget = false;

        RectTransform textRect = statusSellText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = Vector2.zero;
    }

    private Sprite FindPanelSprite()
    {
        foreach (Image image in panelStatus.GetComponentsInChildren<Image>(true))
        {
            if (image.sprite != null)
                return image.sprite;
        }

        return null;
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
        RefreshSelectedPieceInfo();
    }

    private void RefreshSelectedPieceInfo()
    {
        if (selectedPiece == null || selectedPiece.Data == null || selectedPiece.Team != Team.Ally) return;

        if (panelStatus == null || statusTextTitle == null || statusText == null) return;

        PieceData data = selectedPiece.Data;
        statusTextTitle.text = data.pieceName;
        UpdateStatusTier(data);
        statusText.text =
            $"공격력: {selectedPiece.GetAttackDamage():0.0}\n" +
            $"사거리: {data.attackRange:0.0}\n" +
            $"공격속도: {selectedPiece.GetAttackCooldown():0.0}초";

        if (statusSellText != null)
            statusSellText.text = $"판매  +{GetSellPrice(selectedPiece)}G";

        panelStatus.SetActive(true);
    }

    private void SellSelectedPiece()
    {
        if (selectedPiece == null || selectedPiece.Data == null || selectedPiece.IsDead) return;

        int sellPrice = GetSellPrice(selectedPiece);
        Piece pieceToSell = selectedPiece;
        HideSelectedPieceInfo();

        if (GameManager.Instance != null)
            GameManager.Instance.AddGold(sellPrice);

        Destroy(pieceToSell.gameObject);
    }

    private static int GetSellPrice(Piece piece)
    {
        if (piece == null || piece.Data == null) return 0;

        PieceData data = piece.Data;
        GameDatabase database = GameManager.Instance != null ? GameManager.Instance.Database : null;
        if (database != null)
        {
            TierStatRecord stat = database.GetTierStat(GetPieceId(data.pieceName), data.tier);
            if (stat != null && stat.sell > 0)
                return stat.sell;
        }

        return Mathf.Max(1, Mathf.CeilToInt(data.cost * 0.5f));
    }

    private static int GetPieceId(string pieceName)
    {
        if (string.IsNullOrEmpty(pieceName)) return 0;

        switch (pieceName.ToLowerInvariant())
        {
            case "bishop": return 10001;
            case "knight": return 10002;
            case "rook": return 10003;
            case "pawn": return 10111;
            case "queen": return 10112;
            case "king": return 10113;
            default: return 0;
        }
    }

    private void OnUpgradeChanged(PieceUpgradeType type)
    {
        if (selectedPiece == null || selectedPiece.Data == null || panelStatus == null || !panelStatus.activeSelf) return;

        if (UpgradeManager.TryGetType(selectedPiece.Data.pieceName, out PieceUpgradeType selectedType) && selectedType == type)
            RefreshSelectedPieceInfo();
    }

    private void UpdateStatusTier(PieceData data)
    {
        if (statusTierText == null || data == null) return;

        bool isBasicUnit = UpgradeManager.TryGetType(data.pieceName, out _);
        statusTierText.gameObject.SetActive(isBasicUnit);

        if (!isBasicUnit) return;

        statusTierText.text = $"TIER {data.tier}";
        statusTierText.color = PieceData.TierColor(data.tier);
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
