using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScencsChane : MonoBehaviour
{
    private void Start()
    {
        WireButton("Canvas/Button_StartGame", StartGame);
        WireButton("Canvas/Button_QuitGame", QuitGame);
    }

    private static void WireButton(string path, UnityEngine.Events.UnityAction action)
    {
        GameObject go = GameObject.Find(path);
        Button button = go != null ? go.GetComponent<Button>() : null;
        if (button != null)
            button.onClick.AddListener(action);
    }

    public void StartGame()
    {
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
