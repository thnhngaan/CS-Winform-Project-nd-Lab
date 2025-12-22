using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayVsAIEasy()
    {
        PlayerPrefs.SetString("GameMode", "PvAI_Easy");
        SceneManager.LoadScene("GameAI");
    }

    public void PlayVsAINormal()
    {
        PlayerPrefs.SetString("GameMode", "PvAI_Normal");
        SceneManager.LoadScene("GameAI");
    }

    public void PlayVsAIHard()
    {
        PlayerPrefs.SetString("GameMode", "PvAI_Hard");
        SceneManager.LoadScene("GameAI");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
