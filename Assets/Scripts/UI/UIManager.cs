using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
    private Button speedButton;
    private TextMeshProUGUI speedButtonText;
    private TextMeshProUGUI shortcutHelpText;

    // Chosen play speed. Kept separate from Time.timeScale because opening the options
    // window parks the scale at 0, and closing it has to restore this rather than 1x.
    private float gameSpeed = 1f;
    private TextMeshProUGUI statusTierText;
    private Button statusSellButton;
    private TextMeshProUGUI statusSellText;
    private bool subscribed;
    private bool upgradeSubscribed;

    private const string EffectsVolumeKey = SFXManager.EffectsVolumeKey;
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
        WireVictoryButtons();
        CreateSpeedToggleButton();
        CreateShortcutHelp();
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

        PlayerPrefs.Save();
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

    // Clearing the final stage shows this popup; its single button leaves the run.
    private void WireVictoryButtons()
    {
        if (victoryPanel == null) return;

        SetPanelText(victoryPanel, "Text_Title", "STAGE CLEAR");
        SetPanelText(victoryPanel, "Text_Info", $"All {GameManager.FinalStage} stages cleared!");

        Button exitButton = FindButtonByName(victoryPanel, "Button_OK");
        if (exitButton == null) return;

        // Same Content_Demo convention as the game-over popup: the button container
        // ships inactive and is switched on once its buttons are wired.
        exitButton.transform.parent.gameObject.SetActive(true);
        exitButton.onClick.AddListener(ReturnToTitle);

        var label = exitButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = "나가기";
    }

    private static void SetPanelText(GameObject root, string objectName, string value)
    {
        foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.name == objectName)
            {
                text.text = value;
                return;
            }
        }
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
        HandleShortcuts();

        // The kill count ticks up while the panel is open, so keep it live.
        if (selectedPiece != null)
            RefreshSelectedPieceInfo();

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

    // Keyboard equivalents for the on-screen controls. Piece buying (space) lives in
    // PieceManager next to the pull itself.
    private void HandleShortcuts()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
            ToggleOptionWindow();

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            SetGameSpeed(1f);
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            SetGameSpeed(2f);

        if (keyboard.deleteKey.wasPressedThisFrame)
            SellSelectedPiece();
    }

    public void ToggleOptionWindow()
    {
        if (optionPanel == null) return;

        if (optionPanel.activeSelf)
            CloseOptionWindow();
        else
            OpenOptionWindow();
    }

    private void UpdateGold(int gold) { if (goldText != null) goldText.text = $"Gold: {gold}"; }
    private void UpdateLives(int lives) { if (livesText != null) livesText.text = $"Lives: {lives}"; }
    private void UpdateWave(int wave)
    {
        if (waveText != null) waveText.text = $"Wave: {wave}";
        // Clearing a boss stage raises the pull price, so refresh the label with the wave.
        UpdateBuyButtonText();
    }

    private void OnStateChanged(GameState state)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(state == GameState.GameOver);
        if (victoryPanel != null) victoryPanel.SetActive(state == GameState.Victory);
    }

    private void UpdateBuyButtonText()
    {
        if (buyButtonText == null) return;

        // The price climbs past each boss, so read it rather than printing a constant.
        int cost = pieceManager != null ? pieceManager.CurrentPullCost : 0;
        buyButtonText.text = $"Pull ({cost}G)";
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
            CreateSliderLabel(efxSound, "효과음");
        }

        if (bgSound != null)
        {
            bgSound.SetValueWithoutNotify(backgroundVolume);
            bgSound.onValueChanged.AddListener(SetBackgroundVolume);
            CreateSliderLabel(bgSound, "브금");
        }

        ApplyEffectsVolume(effectsVolume);
        ApplyBackgroundVolume(backgroundVolume);
    }

    public void SetEffectsVolume(float volume)
    {
        ApplyEffectsVolume(volume);
        PlayerPrefs.SetFloat(EffectsVolumeKey, volume);

        // The options window pauses the game, so no gameplay sound would otherwise play
        // while the slider is being dragged and it would feel like it does nothing.
        // SFXManager's per-clip cooldown keeps a fast drag from machine-gunning this.
        SFXManager.Instance?.PlayButtonClick();
    }

    public void SetBackgroundVolume(float volume)
    {
        ApplyBackgroundVolume(volume);
        PlayerPrefs.SetFloat(BackgroundVolumeKey, volume);
    }

    // PlayerPrefs.Save writes to disk, which is far too heavy to run on every slider
    // frame; persist once when the scene goes away instead.
    private void OnApplicationQuit() => PlayerPrefs.Save();

    private void ApplyEffectsVolume(float volume)
    {
        // SFXManager builds its AudioSource at runtime, so it can never appear in the
        // serialized list below and has to be driven through its own API.
        SFXManager.Instance?.SetVolume(volume);

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

    // Sits directly under the options button, mirroring its anchoring so it stays put
    // whatever the screen size.
    private void CreateSpeedToggleButton()
    {
        if (speedButton != null || openButton == null) return;

        RectTransform openRect = openButton.GetComponent<RectTransform>();
        if (openRect == null) return;

        var speedObject = new GameObject("Button_SpeedToggle", typeof(RectTransform), typeof(Image), typeof(Button));
        speedObject.transform.SetParent(openButton.transform.parent, false);

        var speedRect = speedObject.GetComponent<RectTransform>();
        speedRect.anchorMin = openRect.anchorMin;
        speedRect.anchorMax = openRect.anchorMax;
        speedRect.pivot = openRect.pivot;
        speedRect.sizeDelta = openRect.sizeDelta;
        speedRect.anchoredPosition = openRect.anchoredPosition
            - new Vector2(0f, openRect.sizeDelta.y + 12f);

        Image background = speedObject.GetComponent<Image>();
        Image openImage = openButton.GetComponent<Image>();
        if (openImage != null && openImage.sprite != null)
        {
            background.sprite = openImage.sprite;
            background.type = openImage.type;
        }
        background.color = new Color(0.20f, 0.22f, 0.28f, 0.95f);

        speedButton = speedObject.GetComponent<Button>();
        speedButton.targetGraphic = background;
        speedButton.onClick.AddListener(ToggleGameSpeed);
        SFXManager.Instance?.BindButtonClickSound(speedButton);

        if (statusTextTitle != null)
        {
            speedButtonText = Instantiate(statusTextTitle, speedObject.transform);
            speedButtonText.name = "SpeedToggleText";
            speedButtonText.alignment = TextAlignmentOptions.Center;
            speedButtonText.fontSize = statusTextTitle.fontSize;
            speedButtonText.color = Color.white;
            speedButtonText.raycastTarget = false;

            RectTransform textRect = speedButtonText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;
        }

        ApplyGameSpeed();
    }

    // Caption sitting just left of a volume slider, right-aligned so it reads into the
    // track. Positioned off the slider's own rect rather than fixed numbers so it stays
    // put if the slider is ever moved or resized in the scene.
    private void CreateSliderLabel(Slider slider, string caption)
    {
        if (slider == null || statusTextTitle == null) return;

        var sliderRect = slider.transform as RectTransform;
        if (sliderRect == null) return;

        const float gap = 16f;
        float sliderLeftEdge = sliderRect.anchoredPosition.x - sliderRect.sizeDelta.x * sliderRect.pivot.x;

        TextMeshProUGUI label = Instantiate(statusTextTitle, sliderRect.parent);
        label.name = slider.name + "_Label";
        label.alignment = TextAlignmentOptions.Right;
        label.fontSize = statusTextTitle.fontSize * 0.9f;
        label.color = new Color(0.9f, 0.92f, 0.96f, 1f);
        label.raycastTarget = false;
        label.text = caption;

        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = sliderRect.anchorMin;
        labelRect.anchorMax = sliderRect.anchorMax;
        labelRect.pivot = new Vector2(1f, 0.5f);
        labelRect.sizeDelta = new Vector2(190f, 44f);
        labelRect.anchoredPosition = new Vector2(sliderLeftEdge - gap, sliderRect.anchoredPosition.y);
    }

    // Keyboard reference inside the options window, anchored above the bottom so it sits
    // clear of the exit button.
    private void CreateShortcutHelp()
    {
        if (shortcutHelpText != null || optionPanel == null || statusTextTitle == null) return;

        shortcutHelpText = Instantiate(statusTextTitle, optionPanel.transform);
        shortcutHelpText.name = "ShortcutHelpText";
        shortcutHelpText.alignment = TextAlignmentOptions.TopLeft;
        shortcutHelpText.fontSize = statusTextTitle.fontSize * 0.8f;
        shortcutHelpText.lineSpacing = 12f;
        shortcutHelpText.color = new Color(0.85f, 0.87f, 0.92f, 1f);
        shortcutHelpText.raycastTarget = false;
        shortcutHelpText.text =
            "[ 조작키 ]\n" +
            "Space      기물 구매\n" +
            "Delete     선택 기물 판매\n" +
            "1 / 2      1배속 / 2배속\n" +
            "ESC        일시정지";

        // Occupies the band between the volume sliders and the exit button; the options
        // window is sized (750x900) to leave exactly this gap free.
        RectTransform rect = shortcutHelpText.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(560f, 250f);
        rect.anchoredPosition = new Vector2(0f, -110f);
    }

    public void ToggleGameSpeed()
    {
        SetGameSpeed(Mathf.Approximately(gameSpeed, 1f) ? 2f : 1f);
    }

    public void SetGameSpeed(float speed)
    {
        gameSpeed = speed;
        ApplyGameSpeed();
    }

    private void ApplyGameSpeed()
    {
        // While the options window holds the game at 0 the new speed is only recorded;
        // CloseOptionWindow puts it into effect.
        bool paused = optionPanel != null && optionPanel.activeSelf;
        if (!paused)
            Time.timeScale = gameSpeed;

        if (speedButtonText != null)
            speedButtonText.text = $"{gameSpeed:0}x";
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
            $"공격속도: {selectedPiece.GetAttackCooldown():0.0}초\n" +
            $"처치: {selectedPiece.Kills}";

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
            // Resume at whatever speed the player picked, not unconditionally 1x.
            Time.timeScale = gameSpeed;
        }
    }
}
