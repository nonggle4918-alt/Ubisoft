using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScencsChane : MonoBehaviour
{
    private void Start()
    {
        WireButton("Canvas/Button_StartGame", StartGame);
        WireButton("Canvas/Button_QuitGame", QuitGame);
        CreateInfiniteModeButton();
    }

    private static Button WireButton(string path, UnityEngine.Events.UnityAction action)
    {
        GameObject go = GameObject.Find(path);
        Button button = go != null ? go.GetComponent<Button>() : null;
        if (button != null)
            button.onClick.AddListener(action);
        return button;
    }

    // Clones Button_StartGame at runtime instead of requiring a scene edit, matching the
    // dynamic-button convention already used by UIManager (e.g. Button_SpeedToggle).
    private void CreateInfiniteModeButton()
    {
        GameObject startGameObject = GameObject.Find("Canvas/Button_StartGame");
        if (startGameObject == null) return;

        GameObject infiniteObject = Instantiate(startGameObject, startGameObject.transform.parent);
        infiniteObject.name = "Button_InfiniteMode";

        RectTransform startRect = startGameObject.GetComponent<RectTransform>();
        RectTransform infiniteRect = infiniteObject.GetComponent<RectTransform>();
        if (startRect != null && infiniteRect != null)
            infiniteRect.anchoredPosition = startRect.anchoredPosition - new Vector2(0f, startRect.sizeDelta.y + 16f);

        Button infiniteButton = infiniteObject.GetComponent<Button>();
        if (infiniteButton == null) return;

        infiniteButton.onClick.RemoveAllListeners();
        infiniteButton.onClick.AddListener(StartInfiniteGame);

        var label = infiniteObject.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (label != null)
            label.text = "무한 모드";
    }

    public void StartGame()
    {
        GameManager.RequestedMode = GameMode.Normal;
        SceneManager.LoadScene("InGameScens");
    }

    public void StartInfiniteGame()
    {
        GameManager.RequestedMode = GameMode.Infinite;
        SceneManager.LoadScene("InGameScens");
    }

    public void OutGame()
    {
        SceneManager.LoadScene("StandbyScenes");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
