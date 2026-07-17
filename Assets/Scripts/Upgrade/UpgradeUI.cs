using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour
{
    private GameObject popup;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI infoText;
    private Button upgradeButton;
    private TextMeshProUGUI upgradeButtonText;

    private PieceUpgradeType currentType;

    private readonly Dictionary<PieceUpgradeType, string> displayNames = new Dictionary<PieceUpgradeType, string>
    {
        { PieceUpgradeType.Bishop, "비숍" },
        { PieceUpgradeType.Knight, "나이트" },
        { PieceUpgradeType.Rook, "룩" }
    };

    private void Start()
    {
        SetupOpenButton("Canvas/UpgradeBtn_Bishop", PieceUpgradeType.Bishop, new Vector2(117f, 210f));
        SetupOpenButton("Canvas/UpgradeBtn_Knight", PieceUpgradeType.Knight, new Vector2(237f, 210f));
        SetupOpenButton("Canvas/UpgradeBtn_Rook", PieceUpgradeType.Rook, new Vector2(357f, 210f));
        SetupPopup();

        if (popup != null)
            popup.SetActive(false);

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradeChanged += OnUpgradeChanged;
        if (GameManager.Instance != null)
            GameManager.Instance.OnGoldChanged += OnGoldChanged;
    }

    private void OnDestroy()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradeChanged -= OnUpgradeChanged;
        if (GameManager.Instance != null)
            GameManager.Instance.OnGoldChanged -= OnGoldChanged;
    }

    private void SetupOpenButton(string path, PieceUpgradeType type, Vector2 anchoredPosition)
    {
        GameObject go = GameObject.Find(path);
        if (go == null) return;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(118f, 84f);
        rt.anchoredPosition = anchoredPosition;

        Transform label = go.transform.Find("Text");
        if (label != null)
        {
            TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = displayNames[type];
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 10f;
                tmp.fontSizeMax = 26f;
            }
        }

        Button button = go.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OpenPopup(type));
        }
    }

    private void SetupPopup()
    {
        popup = GameObject.Find("Canvas/UpgradePopup");
        if (popup == null) return;

        RectTransform prt = popup.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;

        Transform title = popup.transform.Find("Text_Title");
        if (title != null) titleText = title.GetComponent<TextMeshProUGUI>();

        Transform info = popup.transform.Find("Text_Info");
        if (info != null) infoText = info.GetComponent<TextMeshProUGUI>();

        Transform okTransform = popup.transform.Find("Content_Demo/Button_OK");
        if (okTransform == null) return;

        upgradeButton = okTransform.GetComponent<Button>();
        Transform okLabel = okTransform.Find("Text (TMP)");
        if (okLabel != null)
        {
            upgradeButtonText = okLabel.GetComponent<TextMeshProUGUI>();
            ShrinkButtonLabel(upgradeButtonText);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

        okTransform.parent.gameObject.SetActive(true);

        GameObject closeGo = Instantiate(okTransform.gameObject, okTransform.parent);
        closeGo.name = "Button_Close";

        RectTransform okRt = okTransform.GetComponent<RectTransform>();
        RectTransform closeRt = closeGo.GetComponent<RectTransform>();
        okRt.sizeDelta = new Vector2(150f, 90f);
        closeRt.sizeDelta = new Vector2(150f, 90f);
        okRt.anchoredPosition = new Vector2(85f, 0f);
        closeRt.anchoredPosition = new Vector2(-85f, 0f);

        Button closeButton = closeGo.GetComponent<Button>();
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePopup);
        }

        Transform closeLabel = closeGo.transform.Find("Text (TMP)");
        if (closeLabel != null)
        {
            TextMeshProUGUI tmp = closeLabel.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = "닫기";
                ShrinkButtonLabel(tmp);
            }
        }
    }

    private static void ShrinkButtonLabel(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        tmp.enableAutoSizing = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.fontSize = 22f;
    }

    private void OpenPopup(PieceUpgradeType type)
    {
        currentType = type;
        if (popup != null)
        {
            popup.SetActive(true);
            popup.transform.SetAsLastSibling();
        }
        RefreshPopup();
    }

    private void ClosePopup()
    {
        if (popup != null)
            popup.SetActive(false);
    }

    private void OnUpgradeClicked()
    {
        if (UpgradeManager.Instance == null) return;
        UpgradeManager.Instance.TryUpgrade(currentType);
        RefreshPopup();
    }

    private void OnUpgradeChanged(PieceUpgradeType type)
    {
        if (popup != null && popup.activeSelf && type == currentType)
            RefreshPopup();
    }

    private void OnGoldChanged(int gold)
    {
        if (popup != null && popup.activeSelf)
            RefreshPopup();
    }

    private void RefreshPopup()
    {
        UpgradeManager manager = UpgradeManager.Instance;
        if (manager == null) return;

        int level = manager.GetLevel(currentType);
        float atk = manager.GetAtkPercent(currentType);
        float cool = manager.GetCoolPercent(currentType);
        bool maxed = manager.IsMaxed(currentType);

        if (titleText != null)
            titleText.text = displayNames[currentType] + " 강화";

        if (infoText != null)
        {
            if (maxed)
            {
                infoText.text =
                    $"레벨 {level} / {manager.MaxLevel} (MAX)\n\n" +
                    $"공격력  +{atk:0.#}%\n" +
                    $"공격속도  +{cool:0.#}%";
            }
            else
            {
                float nextAtk = manager.GetAtkPercentAt(currentType, level + 1);
                float nextCool = manager.GetCoolPercentAt(currentType, level + 1);
                infoText.text =
                    $"레벨 {level} / {manager.MaxLevel}\n\n" +
                    $"공격력  +{atk:0.#}%  →  +{nextAtk:0.#}%\n" +
                    $"공격속도  +{cool:0.#}%  →  +{nextCool:0.#}%";
            }
        }

        if (upgradeButton != null)
        {
            int cost = manager.NextCost(currentType);
            bool canAfford = GameManager.Instance != null && GameManager.Instance.Gold >= cost;
            upgradeButton.interactable = !maxed && canAfford;
            if (upgradeButtonText != null)
                upgradeButtonText.text = maxed ? "최대 레벨" : $"강화 ({cost}G)";
        }
    }
}
