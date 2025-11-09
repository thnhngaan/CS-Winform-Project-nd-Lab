using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class CheckRegister : MonoBehaviour
{
    public string NextScene;
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(NextScene);
    }
}
