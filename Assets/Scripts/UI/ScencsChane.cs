using UnityEngine;
using UnityEngine.SceneManagement;

public class ScencsChane : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void StartGame()
    {
        SceneManager.LoadScene("InGameScens");
    }

    public void OutGame()
    {
        SceneManager.LoadScene("StandbyScenes");
    }
}
