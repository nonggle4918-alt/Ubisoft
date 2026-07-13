using UnityEngine;
using UnityEngine.SceneManagement;

public class ScencsChane : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void SceneChange()
    {
        SceneManager.LoadScene("InGameScens");
    }
}
