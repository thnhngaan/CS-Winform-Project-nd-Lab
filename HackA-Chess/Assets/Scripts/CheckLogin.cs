using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class CheckLogin : MonoBehaviour
{
    public string NextScene;
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(NextScene);
    }
}
