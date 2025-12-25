using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverAI : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text winnerText;
    public GameObject user1;
    public GameObject user2;
    public GameObject message;
    public TMP_Text chat;
    public TMP_Text UserName;
    public TMP_Text EnemyName;

    // Gọi hàm này khi hết game
    public void ShowGameOver(string winner)
    {
        winnerText.text = winner.ToUpper() + " WINS!";
        panel.SetActive(true);
        user1.SetActive(false);
        user2.SetActive(false);
        message.SetActive(false);
        chat.gameObject.SetActive(false);
        UserName.gameObject.SetActive(false);
        EnemyName.gameObject.SetActive(false);
    }

    // Nút Rematch
    public void OnRematchButton()
    {
        Debug.Log("Rematch clicked");
        // Load lại scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Nút Quit
    public void OnQuitButton()
    {
        SceneManager.LoadScene("MainMenu");
    }
}