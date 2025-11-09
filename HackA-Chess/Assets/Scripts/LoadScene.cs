using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadScene : MonoBehaviour
{
    public string NextScene;
    public void GoToNextScene()
    {
        SceneManager.LoadScene(NextScene);
    }
}
